using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        Dictionary<Tuple<string, string>, XElement> _itemDict = new Dictionary<Tuple<string, string>, XElement>();

        public enum ElementTypes
        {
            Click,
            Type,
            Select
        }

        public enum ClassLanguage
        {
            CSharp = 0,
            VB = 1
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
                ParseElements(entry.Key, entry.Value);
            }
            //iterate on our final collection of items and wrap the jQuery selector for printing
            foreach (KeyValuePair<Tuple<string, string>, XElement> kvp in _itemDict)
            {
                XElement element = kvp.Value;
                element.SetAttributeValue("jQuery", WrapJquery(element.Attribute("jQuery").Value));
                finalXDoc.Root.Add(element);
            }

            //create the class file
            CreateClass(finalXDoc, language);
        }

        /// <summary>
        /// Disposes of the browser object and shuts down the chromedriver process
        /// </summary>
        public void Cleanup()
        {
            _driver.Quit();
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
            foreach (XElement element in elements.Where(e => e != null))
            {
                try
                {
                    XElement newElement = ParseJQuery(element);
                    newElement.SetAttributeValue("type", type);
                    //check to see if we've encountered this element before so we can modify the name and lookup
                    newElement = GetUniqueElement(newElement);
                    //and then add it to the final list of elements
                    updatedElements.Add(newElement);
                }
                catch
                {
                    //trying to figure out why this works correctly just by running it a second time

                    XElement newElement = ParseJQuery(element);
                    newElement.SetAttributeValue("type", type);
                    //check to see if we've encountered this element before so we can modify the name and lookup
                    newElement = GetUniqueElement(newElement);
                    //and then add it to the final list of elements
                    updatedElements.Add(newElement);
                }
            }
            return updatedElements;
        }

        /// <summary>
        /// A function that will help ensure we generate unique elements for our keywords
        /// </summary>
        /// <param name="element">The element to clean up</param>
        /// <returns></returns>
        private XElement GetUniqueElement(XElement element)
        {
            //go do the heavy lifting to ensure element uniqueness
            element = RecurseUniqueElement(element);

            //we finally have a unique element so add it to our items dictionary
            _itemDict.Add(new Tuple<string, string>(element.Name.LocalName, element.Attribute("jQuery").Value), element);

            return element;
        }

        /// <summary>
        /// A function that will keep iterating on the original element until both its final jQuery and name are unique
        /// </summary>
        /// <param name="element">The element to recurse with</param>
        /// <returns></returns>
        private XElement RecurseUniqueElement(XElement element)
        {
            int parentsCount = 0;
            //check to see if we've encountered this element before so we can modify the name and lookup
            string newElementjQuery = element.Attribute("jQuery").Value;

            //check for a duplicate jQuery lookup so we can make it unique
            while (_itemDict.ContainsKey(new Tuple<string, string>(element.Name.LocalName, newElementjQuery)))
            {
                XElement parent = null;
                parentsCount++;

                //find the total count of items that match the jquery we're interested in.  
                int elementCount = _itemDict.Where(i => i.Key.Item2.Equals(newElementjQuery)).Count();

                //lookup the parent of the other element(s) that matches the new element's jQuery
                if (elementCount == 1) //simplest and should be the only case...
                {
                    //***fix the existing element first
                    //create the key associated with the matching jQuery
                    Tuple<string,string> key = new Tuple<string,string>(element.Name.LocalName, newElementjQuery);

                    // lookup the existing element's parent
                    parent = GetJqueryParent(newElementjQuery, 0, parentsCount);
                    // parse the parent into it's usable jQuery lookup
                    parent = ParseJQuery(parent);
                    // remove the existing element as it will now have a new key for its element
                    XElement oldElement = _itemDict[key];
                    _itemDict.Remove(key);
                    // combine the parent and element jquery lookups together
                    key = new Tuple<string, string>(key.Item1, String.Format("{0} > {1}", parent.Attribute("jQuery").Value, newElementjQuery));
                    oldElement.Attribute("jQuery").Value = key.Item2;
                    _itemDict.Add(key, oldElement);

                    //***fix the new element second
                    // lookup the new element's parent
                    parent = GetJqueryParent(newElementjQuery, 1, parentsCount);
                    // parse the parent into it's usable jQuery lookup
                    parent = ParseJQuery(parent);
                    // combine the parent and element jquery lookups together
                    newElementjQuery = String.Format("{0} > {1}", parent.Attribute("jQuery").Value, newElementjQuery);
                    // combine the parent and element names
                    element.Name = parent.Name.LocalName + "_" + element.Name.LocalName;
                }
                else if (elementCount > 1) //more complicated and should hopefully not become an issue
                {
                    string foo = string.Empty;
                    //for (int i = 0; i <= _itemDict.Where(i => i.Key.Item2.Equals(newElementjQuery)).Count(); i++)
                    //{
                    //    //XElement fixElement = _knownItem
                    //}
                }
            }
            //set the jQuery attribute
            element.SetAttributeValue("jQuery", newElementjQuery);

            // check for a unique name only once we have a unique jQuery selector
            if (_itemDict.Where(i => i.Key.Item1.Equals(element.Name.LocalName)).Count() > 0)
            {
                // append whatever count the element is a copy of to the end of the name
                element.Name = element.Name.LocalName + "_" + _itemDict.Where(i => i.Key.Item1.StartsWith(element.Name.LocalName)).Count();
            }

            return element;
        }

        /// <summary>
        /// Takes an XElement and parses any applicable attribute values from it
        /// </summary>
        /// <param name="element">The XElement to parse</param>
        /// <returns></returns>
        private Dictionary<string, string> ParseXElementAttributes(XElement element)
        {
            Dictionary<string, string> parsed = new Dictionary<string, string>();
            foreach (XAttribute attr in element.Attributes())
            {
                //remove all troublesome attributes until we can find a safe way to wrap them
                if (!attr.Name.LocalName.Contains("mouse") &&
                    !attr.Name.LocalName.Contains("trigger") &&
                    !attr.Name.LocalName.Contains("click") &&
                    !attr.Name.LocalName.Contains("src") &&
                    !attr.Name.LocalName.Contains("style") &&
                    !attr.Name.LocalName.Contains("data-"))
                {
                    parsed.Add(attr.Name.LocalName, TryGetElementAttribute(element, attr.Name.LocalName));
                }
            }
            return parsed;
        }

        /// <summary>
        /// Parses the element into its usable jQuery selector components and return the updated XElement
        /// </summary>
        /// <param name="element">The element to parse</param>
        /// <returns>A new XElement with an educated guess for 'name' used as the node and
        /// containing two attributes; the jQuery selector and the type of element</returns>
        private XElement ParseJQuery(XElement element)
        {
            XElement updatedElement = null;
            string tag = element.Name.LocalName;
            string text = string.Empty;
            Dictionary<string, string> elementAttributes = ParseXElementAttributes(element);
            string jQuery = string.Empty;
            string field = string.Empty;

            //take a guess at naming the field
            //TODO: apply regex to id, name, and other such fields that will vary site by site
            // to discern the actual usable value for these fields

            if (element.Element("innerText") != null)
            {
                text = (string)element.Element("innerText").Value;
                field = text;
            }
            else if (element.Descendants("innerText").Count() > 0)
            {
                //there should be a rare case when something has multiple innerText elements present
                // so we'll grab the first one to avoid issues
                text = (string)element.Descendants("innerText").First().Value;
                field = text;
            } // try the value, name, id, class, and finally leave it generic, should be a rarity
            else if (elementAttributes.ContainsKey("value"))
                field = (string)elementAttributes["value"];
            else if (elementAttributes.ContainsKey("title"))
                field = (string)elementAttributes["title"];
            else if (elementAttributes.ContainsKey("id"))
                field = (string)elementAttributes["id"];
            else if (elementAttributes.ContainsKey("class"))
                field = (string)elementAttributes["class"];
            else if (elementAttributes.ContainsKey("name"))
                field = (string)elementAttributes["name"];
            else if (elementAttributes.ContainsKey("rel"))
                field = (string)elementAttributes["rel"];
            else
                field = tag;

            //create the selector
            string selector = BuildGenericJQuerySelector(tag: tag,
                attributes: elementAttributes,
                textValue: text);

            //create the updated element proper
            try
            {
                //currently we roll with the selector as-is if it matched an element on the page
                // further logic will be needed to recursively separate similar elements
                // notes: since the elements are checked in document order and added to the dictionary of elements
                // we can use that index when refering to the elements in javascript/jquery lookup to deduce the correct
                // parents/siblings to use to uniquely identify the element
                if (GetJquerySize(WrapJquery(selector)) >= 1)
                {
                    updatedElement = new XElement(ScrubField(field, tag));
                }
                else //we've discovered a broken jquery selector
                {
                    updatedElement = new XElement(ScrubField(string.Concat("0_JQUERY_MATCHES_", field), tag));
                }
            }
            catch (Exception e)
            {
                //these cases should become exceedingly rare as the project evolves
                updatedElement = new XElement(tag + "_UNABLE_TO_PARSE_SIZE_OF_JQUERY");
            }

            // leave the jQuery selector raw for the time being, we'll wrap it above after doing a uniqueness check
            updatedElement.SetAttributeValue("jQuery", selector);

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
                    classHelper = new CSClassBuilder();
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
            Dictionary<string, string> attributes,
            string textValue = "")
        {
            StringBuilder selector = new StringBuilder(tag);
            for (int i = 0; i < attributes.Keys.Count; i++)
            {
                string key = attributes.Keys.ElementAt(i);
                selector.Append(string.Format("[{0}='{1}']", key, attributes[key]));
            }
            if (!string.IsNullOrEmpty(textValue))
                selector.Append(string.Format(":contains('{0}')", HttpUtility.HtmlDecode(textValue).Trim()));

            return selector.ToString();
        }

        /// <summary>
        /// Replaces all illegal characters from the field value with _ as field is used to help create the xelement name and later on the keyword name 
        ///      and can contain no illegal characters
        /// </summary>
        /// <param name="field">The field value to scrub</param>
        /// <param name="tag">The tag value of the element being scrubbed</param>
        /// <returns></returns>
        private string ScrubField(string field, string tag)
        {
            //watch for special cases
            //remove any random html encoding we might have encountered, ie &nbsp
            field = HttpUtility.HtmlDecode(field);

            //such as field containing a leading number
            if (Regex.Match(field, @"^(\d+)").Success)
            {
                field = String.Concat(tag, "_", field);
            }

            //remove other invalid characters
            //great non-regex answer here
            //http://stackoverflow.com/questions/13343885/how-to-replace-a-character-in-string-using-linq
            field = new string(field.Select(c => (!char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-') ? char.Parse("_") : c).ToArray());
            return field;
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
        private int GetJquerySize(string jquery)
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
        /// Returns the parent of the jQuery selector cleaned of childnodes
        /// </summary>
        /// <param name="jquery">The jQuery selector to look up the parent for</param>
        /// <param name="index">The index of the item to return the parent for</param>
        /// <param name="depth">The number of .parent() elements to look-up to</param>
        /// <returns></returns>
        private XElement GetJqueryParent(string jquery, int index, int depth)
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                StringBuilder script = new StringBuilder(String.Format("return HtmlAsXml.elementToXmlString($({0}[{1}])", WrapJquery(jquery), index.ToString()));
                for (int i = 0; i < depth; i++)
                {
                    script.Append(".parent()");
                }
                script.Append(".clone().empty());");
                Object elem = js.ExecuteScript(script.ToString());
                return XElement.Parse((string)(elem.ToString()));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the first child of the jQuery selector cleaned of childnodes
        /// </summary>
        /// <param name="jquery">The jQuery selector to look up the first child for</param>
        /// <returns></returns>
        private XElement GetJqueryChild(string jquery)
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                //$(jquery).children()[0]).clone().empty()
                Object elem = js.ExecuteScript("return $(" + WrapJquery(jquery) + ".children()[0]).clone().empty();");
                return XElement.Parse((string)(elem.ToString()));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Takes that jquery and wraps it up real nice to make it selector-compatible
        /// </summary>
        /// <param name="jquery">The jquery selector to wrap</param>
        /// <returns></returns>
        private string WrapJquery(string jquery)
        {
            if (jquery.StartsWith("$"))
            {
                return jquery;
            }
            return "$(\"" + jquery + "\")";
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
}
