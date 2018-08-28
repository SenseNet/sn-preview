using System;

namespace SenseNet.Preview.Controller
{
    internal static class ContentExtensions
    {
        public static string GetParentPath(this string path)
        {
            return path.Substring(0, path.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
