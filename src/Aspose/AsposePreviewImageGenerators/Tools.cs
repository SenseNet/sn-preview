using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SenseNet.Client;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class Tools
    {
        public static bool ContentNotFound(Exception ex)
        {
            // If the error contains a 404 response, terminate the process silently as the content or its version 
            // is missing - which is not really an error (e.g. if the content has been approved in the meantime).
            return ex is ClientException cex && cex.StatusCode == HttpStatusCode.NotFound;
        }

        public static bool IsTerminatorError(Exception ex)
        {
            // If the error contains a 404 or 500 response, terminate the process silently as the content or its version 
            // is missing - which is not really an error (e.g. if the content has been approved in the meantime).
            if (ex == null || !(ex is ClientException cex))
                return false;

            switch (cex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.BadGateway:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Logs exceptions thrown during iterating through document pages. It also 
        /// saves an empty image in place of the one that failed.
        /// </summary>
        /// <returns>True if the process should be terminated.</returns>
        public static bool HandlePageError(Exception ex, int page, IPreviewGenerationContext context, bool logEvent)
        {
            if (ContentNotFound(ex))
            {
                context.LogWarning(page, SR.Exceptions.NotFound + ex);
                return true;
            }
            if (IsTerminatorError(ex))
            {
                // We log this because this may be a network problem that 
                // prevents us from accessing the portal.
                context.LogWarning(page, SerializeException(ex));
                return true;
            }

            // We must not log an error here, because that would set the status of
            // the whole document to 'Error', and we would loose all previously
            // generated pages and the page count information.
            // We write only the first warning message instead to avoid overloading the event log.
            if (logEvent)
                context.LogWarning(page, SerializeException(ex));

            // Substitute the wrong image with an empty one. This will prevent the
            // viewer plugin from registering preview tasks for missing images over and
            // over again.
            context.SaveEmptyPreview(page);

            return false;
        }

        public static string SerializeException(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            try
            {
                var writer = new StringWriter();
                JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                    .Serialize(writer, ex);
                return writer.GetStringBuilder().ToString();
            }
            catch
            {
                // Most likely the exception is not serializable.
                return ex.ToString();
            }
        }
    }
}
