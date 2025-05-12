using System.IO;
using System.Reflection;

namespace Tetca.Helpers
{
    /// <summary>
    /// Helper to access embedded resources in the assembly.
    /// </summary>
    public static class EmbeddedResources
    {
        /// <summary>
        /// Helper method to access embedded resources in the assembly.
        /// </summary>
        /// <param name="resourceName">Name of the resource</param>
        /// <returns>Stream with the resource contents</returns>
        public static Stream ResourceStream(string resourceName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(App.Name + "." + resourceName);
        }
    }
}
