using System;
using System.Diagnostics;
using SenseNet.TaskManagement.Core;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    internal class Logger
    {
        private static readonly string LOG_PREFIX = "#AsposePreviewGenerator> ";

        internal static void WriteInfo(int contentId, int page, string message)
        {
            Trace.WriteLine(string.Format("{0} {1} Content id: {2}, page number: {3}", LOG_PREFIX, message, contentId, page));
        }

        internal static void WriteWarning(int contentId, int page, string message)
        {
            WriteInfo(contentId, page, message);

            // this will be recognized and logged by the agent (because of the WARNING prefix)
            Console.WriteLine("WARNING: Content id: {0}, page: {1}. {2}", contentId, page, message);
        }

        internal static void WriteError(int contentId, int page = 0, string message = null, Exception ex = null, int startIndex = 0, string version = "")
        {
            Trace.WriteLine(string.Format("{0} ERROR {1} Content id: {2}, version: {3}, page number: {4}, start index: {5}, Exception: {6}",
                LOG_PREFIX,
                message == null ? "Error during preview generation." : message.Replace(Environment.NewLine, " * "), 
                contentId, 
                version,
                page, 
                startIndex,
                ex == null ? string.Empty : ex.ToString().Replace(Environment.NewLine, " * ")));

            if (ex != null)
                Console.WriteLine("ERROR:" + SnTaskError.Create(ex, new
                {
                    ContentId = contentId, 
                    Page = page, 
                    StartIndex = startIndex, 
                    Version = version,
                    Message = message
                }));
        }
    }
}
