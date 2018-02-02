using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace SenseNet.Preview
{
    public class Tools
    {
        public static bool ContentNotFound(WebException ex)
        {
            // If the error contains a 404 response, terminate the process silently as the content or its version 
            // is missing - which is not really an error (e.g. if the content has been approved in the meantime).

            var rp = ex?.Response as HttpWebResponse;

            return rp?.StatusCode == HttpStatusCode.NotFound;
        }

        public static bool IsTerminatorError(WebException ex)
        {
            // If the error contains a 404 or 500 response, terminate the process silently as the content or its version 
            // is missing - which is not really an error (e.g. if the content has been approved in the meantime).

            var rp = ex?.Response as HttpWebResponse;
            if (rp == null)
                return false;

            switch (rp.StatusCode)
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
            if (ContentNotFound(ex as WebException))
            {
                context.LogWarning(page, SR.Exceptions.NotFound + ex);
                return true;
            }
            if (IsTerminatorError(ex as WebException))
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
