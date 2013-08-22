﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Otto.ClassBuilder
{
    class CSClassBuilder : IClassBuilder
    {
        /// <summary>
        /// Generates the header of the class file, including class declaration and shared template property
        /// </summary>
        /// <param name="className">The class name to use</param>
        /// <param name="template">The template class to use</param>
        /// <returns></returns>
        public string GenerateHeader(string className, string template)
        {
            StringBuilder generatedString = new StringBuilder();
            generatedString.AppendLine("namespace Automation");
            generatedString.AppendLine("{");
            generatedString.AppendLine(String.Format("public class {0} : {1}", className, "AutoBase"));
            generatedString.AppendLine("{");
            if (!string.IsNullOrEmpty(template))
            {
                generatedString.AppendLine(String.Format("public readonly {0} Template", template));
                generatedString.AppendLine("{");
                generatedString.AppendLine(String.Format("     get { return new {0}(); }", template));
                generatedString.AppendLine("}");
            }
            return generatedString.ToString();
        }

        /// <summary>
        /// Generates the code for a click action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        public string GenerateClick(string jQuery, string name)
        {
            StringBuilder generatedString = new StringBuilder();
            generatedString.AppendLine("/// <summary>");
            generatedString.AppendLine(String.Format("/// Clicks on the '{0}' button", name));
            generatedString.AppendLine("/// </summary>");
            generatedString.AppendLine(String.Format("public void Click_{0}()", name));
            generatedString.AppendLine("{");
            generatedString.AppendLine("     //human-readable jquery");
            generatedString.AppendLine(String.Format("     //AutoBase.Click(\"{0}\");", HttpUtility.HtmlDecode(jQuery)));
            generatedString.AppendLine("     //machine-readable jquery");
            generatedString.AppendLine(String.Format("     AutoBase.Click(\"{0}\");", HttpUtility.HtmlEncode(jQuery)));
            generatedString.AppendLine("}");
            return generatedString.ToString();
        }

        /// <summary>
        /// Generates the code for a type action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        public string GenerateType(string jQuery, string name)
        {
            StringBuilder generatedString = new StringBuilder();
            generatedString.AppendLine("/// <summary>");
            generatedString.AppendLine(String.Format("/// Sets the text on the '{0}' field", name));
            generatedString.AppendLine("/// <param name=\"value\">The value to set in the field</param>)");
            generatedString.AppendLine("/// </summary>");
            generatedString.AppendLine(String.Format("public void Set_{0}(string value)", name));
            generatedString.AppendLine("{");
            generatedString.AppendLine("     //human-readable jquery");
            generatedString.AppendLine(String.Format("     //AutoBase.SetField(\"{0}\", value, FieldType.Text);", HttpUtility.HtmlDecode(jQuery)));
            generatedString.AppendLine("     //machine-readable jquery");
            generatedString.AppendLine(String.Format("     AutoBase.SetField(\"{0}\", value, FieldType.Text);", HttpUtility.HtmlEncode(jQuery)));
            generatedString.AppendLine("}");
            generatedString.AppendLine();
            generatedString.AppendLine("/// <summary>");
            generatedString.AppendLine(String.Format("/// Verifies the text on the '{0}' field", name));
            generatedString.AppendLine("/// <param name=\"value\">The value to verify on the field</param>)");
            generatedString.AppendLine("/// </summary>");
            generatedString.AppendLine(String.Format("public void Verify_{0}(string value)", name));
            generatedString.AppendLine("{");
            generatedString.AppendLine("     //human-readable jquery");
            generatedString.AppendLine(String.Format("     //AutoBase.VerifyField(\"{0}\", value, FieldType.Text);", HttpUtility.HtmlDecode(jQuery)));
            generatedString.AppendLine("     //machine-readable jquery");
            generatedString.AppendLine(String.Format("     AutoBase.VerifyField(\"{0}\", value, FieldType.Text);", HttpUtility.HtmlEncode(jQuery)));
            generatedString.AppendLine("}");
            return generatedString.ToString();
        }

        /// <summary>
        /// Generates the code for a select drop-down action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        public string GenerateSelect(string jQuery, string name)
        {
            StringBuilder generatedString = new StringBuilder();
            generatedString.AppendLine("/// <summary>");
            generatedString.AppendLine(String.Format("/// Sets the text on the '{0}' field", name));
            generatedString.AppendLine("/// <param name=\"value\">The value to set in the field</param>)");
            generatedString.AppendLine("/// </summary>");
            generatedString.AppendLine(String.Format("public void Set_{0}(string value)", name));
            generatedString.AppendLine("{");
            generatedString.AppendLine("     //human-readable jquery");
            generatedString.AppendLine(String.Format("     //AutoBase.SetField(\"{0}\", value, FieldType.Select);", HttpUtility.HtmlDecode(jQuery)));
            generatedString.AppendLine("     //machine-readable jquery");
            generatedString.AppendLine(String.Format("     AutoBase.SetField(\"{0}\", value, FieldType.Select);", HttpUtility.HtmlEncode(jQuery)));
            generatedString.AppendLine("}");
            generatedString.AppendLine();
            generatedString.AppendLine("/// <summary>");
            generatedString.AppendLine(String.Format("/// Verifies the text on the '{0}' field", name));
            generatedString.AppendLine("/// <param name=\"value\">The value to verify on the field</param>)");
            generatedString.AppendLine("/// </summary>");
            generatedString.AppendLine(String.Format("public void Verify_{0}(string value)", name));
            generatedString.AppendLine("{");
            generatedString.AppendLine("     //human-readable jquery");
            generatedString.AppendLine(String.Format("     //AutoBase.VerifyField(\"{0}\", value, FieldType.Select);", HttpUtility.HtmlDecode(jQuery)));
            generatedString.AppendLine("     //machine-readable jquery");
            generatedString.AppendLine(String.Format("     AutoBase.VerifyField(\"{0}\", value, FieldType.Select);", HttpUtility.HtmlEncode(jQuery)));
            generatedString.AppendLine("}");
            return generatedString.ToString();
        }

        /// <summary>
        /// Generates the footer of the class file, typically closing out the class declaration
        /// </summary>
        /// <returns></returns>
        public string GenerateFooter()
        {
            StringBuilder generatedString = new StringBuilder();
            generatedString.AppendLine("}");
            generatedString.AppendLine("}");
            return generatedString.ToString();
        }
    }
}
