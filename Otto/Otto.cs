using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

using Otto;
using Otto.ClassBuilder;

namespace Otto
{
    public class Otto
    {
        private IWebDriver _driver;
        private XDocument _xDoc;

        public enum ElementTypes
        {
            Click,
            Type,
            Select
        }

        public enum ClassLanguage
        {
            CSharp,
            VB
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
        /// <param name="url">The initial url to navigate to</param>
        public void Initialize(string url)
        {
            _driver = new ChromeDriver();
            _driver.Navigate().GoToUrl(url);
        }

        /// <summary>
        /// Does the heavy lifting and goes through the entire keyword class generation process 
        /// from web-site to file creation
        /// </summary>
        /// <param name="className">The className to use for the generated file</param>
        /// <param name="language">The language type to generate the class as</param>
        public void Generate(string className, ClassLanguage language)
        {
            //ensure the driver has been created
            if (_driver == null)
            {
                throw new NullReferenceException("The Driver instance is null.  Please use Initialize first");
            }

            //inject the htmlasxml script and fetch the returned xml-compatible string
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript(Properties.Resources.HtmlToXml);
            //give the script a second to load
            System.Threading.Thread.Sleep(1000);
            _xDoc = XDocument.Parse((string)js.ExecuteScript("return HtmlAsXml.toXmlString();"));

            //return a filtered list of elements to process with their respective type
            Dictionary<IEnumerable<XElement>, ElementTypes> filteredElements = FilterUsableElements();

            //parse the elements into usable info
            XDocument finalXDoc = new XDocument();
            finalXDoc.AddFirst(new XElement("root"));
            finalXDoc.Root.SetAttributeValue("class", className);
            //finalXDoc.Root.SetAttributeValue("template", "NOT YET IMPLEMENTED");
            finalXDoc.Root.SetAttributeValue("template", "");
            foreach (KeyValuePair<IEnumerable<XElement>, ElementTypes> entry in filteredElements)
            {
                finalXDoc.Root.Add(ParseElements(entry.Key, entry.Value).ToArray());
            }
            
            //create the class file
            CreateClass(finalXDoc, language);
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
                else if (!string.IsNullOrEmpty(title))
                    field = title;
                else if (!string.IsNullOrEmpty(id))
                    field = id;
                else if (!string.IsNullOrEmpty(name))
                    field = name;
                else if (!string.IsNullOrEmpty(classValue))
                    field = classValue;
                else
                    field = tag;
            }
            field = field.Replace(" ", "");

            //create the selector
            string selector = BuildGenericJQuerySelector(tag: tag, 
                id: id, 
                type: type, 
                textValue: text, 
                title: title, 
                classValue: classValue, 
                style: style);
            jQuery = "$(\"" + selector + "\")";

            int recordCount = JquerySize(jQuery);
            if (recordCount == 1)
            {
                try
                {
                    updatedElement = new XElement(field.Replace("-", "_").Replace(".", "_"));
                }
                catch (Exception e)
                {
                    updatedElement = new XElement(tag);
                }
                updatedElement.SetAttributeValue("jQuery", jQuery);
            }
            else if (recordCount > 1)
            {
                //supplied jquery was not unique enough
                //recurse up the parent chain from the element in question and make a more complex lookup statement
            }
            else
            {
                //supplied jquery was either too specific or not unique enough
                //try to recurse in both directions to find a lookup that works
            }

            return updatedElement;
        }

        /// <summary>
        /// Parses the final XDocument down into a usable class for automation
        /// </summary>
        /// <param name="xDoc">The XDocument containing all of the prepped data for generation</param>
        /// <param name="lang">The .NET class to output the class as</param>
        private void CreateClass(XDocument xDoc, ClassLanguage lang)
        {
            //determine the filename for the new class file
            string filename = xDoc.Root.Attribute("class").Value;
            //determine where to save the class after creation
            string folderName = CreateFolder(filename);
            //determine which helper class to use with generation
            IClassBuilder classHelper;
            string extension = string.Empty;
            switch (lang)
            {
                case ClassLanguage.VB:
                    classHelper = new VBClassBuilder();
                    extension = ".vb";
                    break;
                case ClassLanguage.CSharp:
                    classHelper = new VBClassBuilder();
                    extension = ".cs";
                    break;
                default:
                    classHelper = null;
                    extension = null;
                    break;
            }
            //generate the header
            StringBuilder classBuilder = new StringBuilder(classHelper.GenerateHeader(filename, xDoc.Root.Attribute("template").Value));

            //iterate through all the elements and generate their class accessors
            foreach (XElement element in xDoc.Root.Descendants())
            {
                ElementTypes type = (ElementTypes)Enum.Parse(typeof(ElementTypes), (TryGetElementAttribute(element, "type")));
                switch (type)
                {
                    case ElementTypes.Click:
                        classBuilder.AppendLine(classHelper.GenerateClick(TryGetElementAttribute(element, "jQuery"), element.Name.LocalName));
                        break;
                    case ElementTypes.Select:
                        classBuilder.AppendLine(classHelper.GenerateSelect(TryGetElementAttribute(element, "jQuery"), element.Name.LocalName));
                        break;
                    case ElementTypes.Type:
                        classBuilder.AppendLine(classHelper.GenerateType(TryGetElementAttribute(element, "jQuery"), element.Name.LocalName));
                        break;
                }
            }

            //generate the footer
            classBuilder.AppendLine(classHelper.GenerateFooter());

            //output the class file
            File.WriteAllText(String.Concat(folderName, filename, extension), classBuilder.ToString());
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

        /// <summary>
        /// Returns how many unique elements were found from the jquery statement
        /// </summary>
        /// <param name="jquery">The jQuery to execute</param>
        /// <returns></returns>
        private int JquerySize(string jquery)
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                Object size = js.ExecuteScript("return " + jquery + ".size();");
                return int.Parse(size.ToString());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Used to create a new folder based off the filename with a timestamp.
        /// Note: Timestamp is appended to retain a historical record of previously created classes
        /// </summary>
        /// <param name="filename">The filename to create the folder name off of</param>
        /// <returns></returns>
        private string CreateFolder(string filename)
        {
            DateTime timestamp = DateTime.Now;
            string folderName = String.Format("..\\..\\classes\\{0}_{1}\\", filename, timestamp.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folderName);
            return folderName;
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
