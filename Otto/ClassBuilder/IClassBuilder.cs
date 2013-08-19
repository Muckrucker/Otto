using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Otto.ClassBuilder
{
    public interface IClassBuilder
    {
        /// <summary>
        /// Generates the header of the class file, including class declaration and shared template property
        /// </summary>
        /// <param name="className">The class name to use</param>
        /// <param name="template">The template class to use</param>
        /// <returns></returns>
        string GenerateHeader(string className, string template);

        /// <summary>
        /// Generates the code for a click action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        string GenerateClick(string jQuery, string name);

        /// <summary>
        /// Generates the code for a type action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        string GenerateType(string jQuery, string name);

        /// <summary>
        /// Generates the code for a select drop-down action
        /// </summary>
        /// <param name="jQuery">The jQuery for the item to interact with</param>
        /// <param name="name">The name to give the action in code</param>
        /// <returns></returns>
        string GenerateSelect(string jQuery, string name);

        /// <summary>
        /// Generates the footer of the class file, typically closing out the class declaration
        /// </summary>
        /// <returns></returns>
        string GenerateFooter();
    }
}
