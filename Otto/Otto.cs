using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Otto
{
    public class Otto
    {
        private IWebDriver _driver;
        private XDocument _xDoc;

        enum ElementTypes
        {
            Click,
            Type,
            Select
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Otto()
        {
        }

        /// <summary>
        /// Creates a new chromedriver browser instance and navigates to the supplied url
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            _driver = new ChromeDriver();
            _driver.Navigate().GoToUrl(url);
        }

        /// <summary>
        /// Does the heavy lifting and goes through the entire keyword class generation process 
        /// from web-site to file creation
        /// </summary>
        public void Generate()
        {
            //ensure the driver has been created
            if (_driver == null)
            {
                throw new NullReferenceException("The Driver instance is null.  Please use Initialize first");
            }

            //inject the htmlasxml script and fetch the returned xml-compatible string
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("load htmltoxml from github");
            //give the script a second to load
            System.Threading.Thread.Sleep(1000);
            _xDoc = XDocument.Parse((string)js.ExecuteScript("return HtmlAsXml.toXmlString();"));

            //return a filtered list of elements to process with their respective type
            Dictionary<IEnumerable<XElement>, ElementTypes> filteredElements = FilterUsableElements();
        }

        /// <summary>
        /// Rips through the xmldoc and returns elements based on our selection criteria
        /// </summary>
        /// <returns>A filtered collection of XElements that we want to parse</returns>
        private Dictionary<IEnumerable<XElement>, ElementTypes> FilterUsableElements()
        {
            Dictionary<IEnumerable<XElement>, ElementTypes> filteredElements = new Dictionary<IEnumerable<XElement>, ElementTypes>();
            IEnumerable<XElement> currentElements;

            //type-based elements
            //grab input boxes
            currentElements = (from node in _xDoc.Descendants("input")
                               where (node.Attribute("type") != null &&
                                    (node.Attribute("type").Value.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                                    node.Attribute("type").Value.Equals("password", StringComparison.OrdinalIgnoreCase)))
                               select node);
            //grab textareas
            currentElements = currentElements.Union(from node in _xDoc.Descendants("textarea") select node);
            filteredElements.Add(currentElements, ElementTypes.Type);

            //click-based elements
            //grab anchor elements
            currentElements = (from node in _xDoc.Descendants("a") select node);
            //grab button elements
            currentElements = currentElements.Union(from node in _xDoc.Descendants("button")
                                                    where (node.Attribute("type") != null &&
                                                         (node.Attribute("type").Value.Equals("submit", StringComparison.OrdinalIgnoreCase)))
                                                    select node);
            //grab checkbox elements
            currentElements = currentElements.Union(from node in _xDoc.Descendants("input")
                                                    where (node.Attribute("type") != null &&
                                                    (node.Attribute("type").Value.Equals("checkbox", StringComparison.OrdinalIgnoreCase) ||
                                                    node.Attribute("type").Value.Equals("submit", StringComparison.OrdinalIgnoreCase) ||
                                                    node.Attribute("type").Value.Equals("radio", StringComparison.OrdinalIgnoreCase) ||
                                                    node.Attribute("type").Value.Equals("button", StringComparison.OrdinalIgnoreCase)))
                                                    select node);
            filteredElements.Add(currentElements, ElementTypes.Click);

            //dropdown-based elements
            //grab select elements
            currentElements = (from node in _xDoc.Descendants("select") select node);
            //grab unordered list elements
            currentElements = currentElements.Union(from node in _xDoc.Descendants("select") select node);
            filteredElements.Add(currentElements, ElementTypes.Select);
            return filteredElements;
        }

        /// <summary>
        /// Rips through the collection of elements and discerns their jquery selector
        /// </summary>
        /// <param name="elements">Collection of elements to parse</param>
        /// <param name="type">The ElementsTypes of the elements to parse.</param>
        /// <returns>A list of XElements that will be used to generate the class mappings.</returns>
        private List<XElement> ParseElements(IEnumerable<XElement> elements, ElementTypes type)
        {
            List<XElement> updatedElements = new List<XElement>();
            List<MatchCount> matchList = new List<MatchCount>();
            foreach (XElement element in elements)
            {
                XElement newElement = ParseJQuery(element);
                if (newElement != null)
                {
                    newElement.SetAttributeValue("type", type);
                    //check to see if we've encountered this element before so we can increment the name
                    foreach (XElement updatedElement in updatedElements)
                    {
                        if (updatedElement.Name.LocalName.Equals(newElement.Name.LocalName, StringComparison.OrdinalIgnoreCase))
                        {
                            //go through the 'matched' list to update the counter accordingly
                            bool isFound = false;
                            foreach (MatchCount match in matchList)
                            {
                                if (match.Name.Equals(newElement.Name.LocalName, StringComparison.OrdinalIgnoreCase))
                                {
                                    match.Count++;
                                    newElement.Name = newElement.Name.LocalName + "_gen" + match.Count;
                                    isFound = true;
                                    break;
                                }
                                if (!isFound)
                                {
                                    matchList.Add(new MatchCount(newElement.Name.LocalName, 2));
                                    newElement.Name = newElement.Name.LocalName + "_gen2";
                                }
                            }
                        }
                    }
                    updatedElements.Add(newElement);
                }
                else
                {
                    //something may have gone wrong?
                }
            }
            return updatedElements;
        }

        /// <summary>
        /// Parses the element into its usable jQuery selector components
        /// </summary>
        /// <param name="element">The element to parse</param>
        /// <returns>A new XElement with an educated guess for 'name' used as the node and
        /// containing two attributes; the jQuery selector and the type of element</returns>
        private XElement ParseJQuery(XElement element)
        {
            XElement updatedElement = null;
            string tag = element.Name.LocalName;
            string text = string.Empty;
            string value = TryGetElementAttribute(element, "value");
            string name = TryGetElementAttribute(element, "name");
            string id = TryGetElementAttribute(element, "id");
            string title = TryGetElementAttribute(element, "title");
            string classValue = TryGetElementAttribute(element, "class");
            string style = TryGetElementAttribute(element, "style");
            string type = TryGetElementAttribute(element, "type");
            string jQuery = string.Empty;
            string field = string.Empty;

            //take a guess at naming the field
            if (element.Element("innerText") != null)
            {
                text = element.Element("innerText").Value;
                field = text;
            }
            else
            {
                //apply regex to id, name, and other such fields that will vary site by site
                // to discern the actual usable value for these fields

                //we have no text to go on to name the field
                // try the value, name, id, class, and finally leave it generic
                if (!string.IsNullOrEmpty(value))
                    field = value;
                else if (!string.IsNullOrEmpty(name))
                    field = name;
                else if (!string.IsNullOrEmpty(id))
                    field = id;
                else if (!string.IsNullOrEmpty(classValue))
                    field = classValue;
                else
                    field = tag;
            }
            field = field.Replace(" ", "");




            return updatedElement;
        }

        /// <summary>
        /// Creates a jQuery selector based on the supplied input
        /// </summary>
        /// <param name="tag">The html tagname of the element</param>
        /// <param name="id">The id attribute of the element</param>
        /// <param name="type">The type attribute of the element</param>
        /// <param name="textValue">The text present on/inside the element</param>
        /// <param name="title">The title attribute of the element</param>
        /// <param name="classValue">The class attribute of the element</param>
        /// <param name="style">The style attribute of the element</param>
        /// <returns></returns>
        private string BuildGenericJQuerySelector(string tag, 
            string id = "",
            string type = "",
            string textValue = "",
            string title = "",
            string classValue = "",
            string style = "")
        {
            StringBuilder selector = new StringBuilder(tag);
            if (!string.IsNullOrEmpty(id))
                selector.Append(string.Format("[id*='{0}']", id));
            if (!string.IsNullOrEmpty(type))
                selector.Append(string.Format("[type='{0}']", type));
            if (!string.IsNullOrEmpty(title))
                selector.Append(string.Format("[title='{0}']", title));
            if (!string.IsNullOrEmpty(classValue))
                selector.Append(string.Format("[class*='{0}']", classValue));
            if (!string.IsNullOrEmpty(style))
                selector.Append(string.Format("[style*='{0}']", style));
            if (!string.IsNullOrEmpty(textValue))
                selector.Append(string.Format(":contains('{0}')", textValue));

            return selector.ToString();
        }

        /// <summary>
        /// Attempts to parse the element for the specified attribute in a safe way
        /// </summary>
        /// <param name="element">The element to parse</param>
        /// <param name="attribute">The attribute to retrieve</param>
        /// <returns>The value of the attribute or an empty string if it wasn't found</returns>
        private string TryGetElementAttribute(XElement element, string attribute)
        {
            if (element.Attribute(attribute) != null)
            {
                return element.Attribute(attribute).Value.Trim();
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public class MatchCount
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public MatchCount(string name, int count)
        {
            this.Name = name;
            this.Count = count;
        }
    }
}
