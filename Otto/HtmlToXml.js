/*! jQuery v1.10.2 jquery.com | jquery.org/license */
//load jQuery 1.10.2.min
jQueryScript = window.document.getElementsByTagName("body")[0].appendChild(document.createElement("script"));
jQueryScript.setAttribute("language", "javascript");
jQueryScript.setAttribute("type", "text/javascript");
jQueryScript.setAttribute("src", "http://code.jquery.com/jquery-1.10.2.min.js");

/* 
 * DOMParser HTML extension 
 * 2012-02-02 
 * 
 * By Eli Grey, http://eligrey.com 
 * Public domain. 
 * NO WARRANTY EXPRESSED OR IMPLIED. USE AT YOUR OWN RISK. 
 */

/*! @source https://gist.github.com/1129031 */
/*global document, DOMParser*/

(function (DOMParser) {
    "use strict";
    var DOMParser_proto = DOMParser.prototype
      , real_parseFromString = DOMParser_proto.parseFromString;

    // Firefox/Opera/IE throw errors on unsupported types  
    try {
        // WebKit returns null on unsupported types  
        if ((new DOMParser).parseFromString("", "text/html")) {
            // text/html parsing is natively supported  
            return;
        }
    } catch (ex) { }

    DOMParser_proto.parseFromString = function (markup, type) {
        if (/^\s*text\/html\s*(?:;|$)/i.test(type)) {
            var doc = document.implementation.createHTMLDocument("");

            if (markup.toLowerCase().indexOf('<!doctype') > -1) {
                doc.documentElement.innerHTML = markup;
            }
            else {
                doc.body.innerHTML = markup;
            }
            return doc;
        } else {
            return real_parseFromString.apply(this, arguments);
        }
    };
}(DOMParser));

/* 
 * HtmlAsXml javascript
 * 2013-08-19 
 * 
 * By Stephen McCusker, https://github.com/Muckrucker/
 * Public domain. 
 * NO WARRANTY EXPRESSED OR IMPLIED. USE AT YOUR OWN RISK. 
 */

/*! @source https://github.com/Muckrucker/HtmlAsXml */
HtmlAsXml = function () {
    //private members
    var _myHtml = null;

    //returns the html doc of the current page as a string
    var _getHtml = function () {
        //we don't need the entire page coming back at this time, so let's clean it up before we ever work with it
        //remove the script blocks
        window.$('script').remove();
        window.$('noscript').remove();
        return document.getElementsByTagName("body")[0].outerHTML;
    };

    //used to iterate through all properties of an element and returns them back
    //see: http://stackoverflow.com/questions/828311/how-to-iterate-through-all-attributes-in-an-html-element
    var _getElementAttributes = function (elem) {
        var returnString = " ";
        //grab attributes
        for (var i = 0; i < elem.attributes.length; i++) {
            var attrib = elem.attributes[i];
            //.specified indicates it actually has a value present, otherwise it can loop all possible attributes
            if (attrib.specified) {
                //we only want attributes that have a value specified, we're uninteresting in empty attributes
                if (attrib.value) {
                    //we want format 'name="value" '
                    //returnString += attrib.name + "=\"" + (_stringContains(attrib.name, "data-") ? _parseDataTagAttribute(attrib) : _htmlEncode(attrib.value)) + "\" ";
                    returnString += attrib.name + "=\"" + _parseElementAttributeValue(attrib) + "\" ";
                }
            }
        }
        return returnString;
    };

    //used to parse an element's attribute value into an xml-friendly format
    var _parseElementAttributeValue = function (attrib) {
        return _escapeJavascriptNuances(_htmlEncode(attrib.value));
    };

    //used to escape the common js nuances of single quotes, double quotes, and the backslash
    var _escapeJavascriptNuances = function (value) {
        //some simple regex to strip out problem characters with safer equivalents
        return value.replace(/'/g, "&apos;").replace(/\\/g, "\\").replace(/"/g, "&quot;");
    };

    //html encode the values being referenced to avoid issues with converting them into xml later
    //see: http://stackoverflow.com/questions/1219860/javascript-jquery-html-encoding
    var _htmlEncode = function (value) {
        //create a in-memory div, set it's inner text(which jQuery automatically encodes)
        //then grab the encoded contents back out.  The div never exists on the page.
        value = $('<div/>').text(value).html();
        //encode any illegal js characters to avoid parsing issues later
        //only checking for the backslash as the rest should already be handled by the encoding above
        if (_stringContains(value, "\\")) {
            value = _escapeJavascriptNuances(value);
        }
        return value;
    };

    //html decode the values from their escaped/safe syntax back to plain text
    //see: http://stackoverflow.com/questions/1219860/javascript-jquery-html-encoding
    var _htmlDecode = function (value) {
        return $('<div/>').html(value).text();
    };

    //performs the required lookup and processing for an iframe element
    var _recurseIFrameElement = function (elem) {
        //our element will come in stripped of its children, perform a lookup to get the DOM version
        var jQueryLookup = "window.$(\"" + elem.tagName.toLowerCase();
        //grab attributes
        for (var i = 0; i < elem.attributes.length; i++) {
            var attrib = elem.attributes[i];
            //.specified indicates it actually has a value present, otherwise it can loop all possible attributes
            if (attrib.specified) {
                //we want format "[name='value']"
                //id and class should be enough to find various iframes strewn about the DOM
                if (attrib.name === 'id' || attrib.name === 'class') {
                    jQueryLookup += "[" + attrib.name + "='" + _htmlEncode(attrib.value) + "']";
                }
            }
        }
        jQueryLookup += "\")";
        var domElem = eval(jQueryLookup);
        //existence check
        if (domElem) {
            //figure out which way we can access the iFrame's internal document and then
            if (domElem.contents) {
                if (domElem.contents().length === 0) {
                    //we have located an iFrame element with a valid contents() element but it is cross-domain hosted, unable to parse at this time
                    //note: in order to potentially handle this, we would require the use of an external caller to follow the form below
                    // this allows us to open a new window that is the entire iframe, removing the cross-domain concerns.  using a tool like Selenium
                    // should allow querying into this fake page as if it were a real page.  not currently implemented
                    // var newWindow = window.open(iframe.src, "title");
                    // newWindow.focus();
                    // newWindow.document;
                    return "";
                } else {
                    //find the first element inside the iFrame's document and pass that whole structure back into the recursion
                    return _recurseElementFix(domElem.contents().find('*')[0]);
                }
            } else if (domElem.contentDocument) {
                return "<body>" + _recurseElementFix(domElem.contentDocument.documentElement.getElementsByTagName("body")[0]) + "</body>";
            } else {
                //we couldn't find a valid way to access the iFrame's document, do not attempt to parse at this time
                console.log("New unhandled iframe type located.  Lookup info: " + jQueryLookup);
                return "";
            }
        } else {
            console.log("Invalid jQuery lookup used.  Lookup info: " + jQueryLookup);
            return "";
        }
    };

    //used to recurse through the list of elements and fix their bracketing to xml-standards
    //returns the string equivalent of corrected xml
    var _recurseElementFix = function (elem) {
        try {
            var tagName = elem.tagName.toLowerCase();
            var returnString = "<" + tagName + _getElementAttributes(elem) + ">";

            //grab the top level elems and we'll iterate through them to find the broken html to xml conversion
            if (tagName !== "iframe") {
                //don't forget any plain text - ie <a>foo</a>
                //it will be returned in a separate <innerText> node if there is any present
                returnString += _tryGetInnerText(elem);

                var topLevelElems = elem.children;
                for (var i = 0; i < topLevelElems.length; i++) {
                    returnString += _recurseElementFix(topLevelElems[i]);
                }
            } else {
                //we're dealing with an iFrame - the bane of the internet's existence... even IE6 looks on in horror
                //currently only handles iFrames that do not have cross-domain javascript execution issues
                returnString += _recurseIFrameElement(elem);
            }
        }
        catch (exp) {
            //eating this atm
            console.log("Failure caught while trying to parse an element: " + elem);
        }
        finally {
            //return the finished string
            returnString += "</" + tagName + ">";

            return returnString;
        }
    };

    //iterates through the parent level nodes and fixes their tags
    var _fixFormatting = function (type) {
        //boil down the html a bit
        _myHtml = _getHtml();

        //attempt to parse as xml, this is pretty much a pipedream but it couldn't hurt to try!
        var parser = new DOMParser();
        var xmlDoc = parser.parseFromString(_myHtml, "text/xml");
        var htmlDoc = null;

        //look for potential errors
        var parserError = xmlDoc.getElementsByTagName('parsererror')[0];
        if (parserError) {
            //grab the current html string and parse it as an html document to preserve querying mechanisms
            body = new DOMParser().parseFromString(_myHtml, "text/html");
            //attempt to recurse the html string into a valid xml-format string
            htmlDoc = _recurseElementFix(body.childNodes[1]);
        }

        if (type === 'doc') {
            //return the finished html doc as an xml doc
            return new DOMParser().parseFromString(htmlDoc, "text/xml");
        } else if (type === 'string') {
            //return the finished html doc as an xml doc compatible string
            return htmlDoc;
        }
    };

    //simple string contains method, avoiding adding this to the String.prototype in case of fires
    var _stringContains = function (str, contains) {
        return str.indexOf(contains) !== -1;
    };

    //tries to retrieve the inner text of an element
    //returns an <innerText> node with content if successful, an empty string otherwise
    //notes: this has an issue with items that have a split of text and html - ie <a>foo <p>bie</p> doo</a>
    //and will return 'foo  doo' currently
    var _tryGetInnerText = function (elem) {
        var text = null;
        try {
            //check for any internal text on a given element, ie 'foo' in <a>foo</a> 
            //see: http://stackoverflow.com/questions/6520192/get-text-node-of-an-element
            text = _htmlEncode(window.$(elem).contents().filter(function () { return this.nodeType === 3; }).text());
        } catch (exp) {
            //if something went awry trying to parse/process the text content, just set it to an empty string
            console.log("Failed to retrieve the innertext content of an element: " + elem);
            text = "";
        } finally {
            //if our text is undefined, return an empty string
            if (!text) { text = ""; }
            text = text.trim();

            //strip out all newline, returns, and/or tabs and encode the value in case of illegal chars
            //return "<innerText>" + _htmlEncode(text.trim().replace(/[\n\r\t]/g, '')) + "</innerText>";
            return (text.length > 0 ? "<innerText>" + _htmlEncode(text.replace(/[\n\r\t]/g, '')) + "</innerText>" : "");
        }
    };

    //public members
    return {
        //converts the html of the current page into an xml document
        toXml: function () {
            return _fixFormatting("doc");
        },
        //converts the html of the current page into an xml document compatible string
        toXmlString: function () {
            return _fixFormatting("string");
        }
    };
}();