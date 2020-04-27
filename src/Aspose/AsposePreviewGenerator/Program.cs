using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Drawing;
using System.Collections.Specialized;
using System.Threading;
using SenseNet.TaskManagement.Core;

namespace SenseNet.Preview.AsposePreviewGenerator
{
    internal class Program
    {
        public static int ContentId { get; set; }
        public static string Version { get; set; }
        public static int StartIndex { get; set; }
        public static int MaxPreviewCount { get; set; }
        public static string SiteUrl { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }

        private static int REQUEST_RETRY_COUNT = 3;
        private static string EmptyImage = "empty.png";

        private static SnSubtask _generatingPreviewSubtask;

        private static void Main(string[] args)
        {
            if (!ParseParameters(args))
            {
                Logger.WriteWarning(ContentId, 0, "Aspose preview generator process arguments are not correct.");
                return;
            }

            ServicePointManager.DefaultConnectionLimit = 10;

            try
            {
                GenerateImages();
            }
            catch (Exception ex)
            {
                if (Tools.ContentNotFound(ex as WebException))
                    return;

                Logger.WriteError(ContentId, 0, ex: ex, startIndex: StartIndex, version: Version);

                SetPreviewStatus(-3); // PreviewStatus.Error
            }
        }

        // ================================================================================================== Preview generation

        protected static void GenerateImages()
        {
            int previewsFolderId;
            string contentPath;
            var downloadingSubtask = new SnSubtask("Downloading", "Downloading file and other information");
            downloadingSubtask.Start();

            try
            {
                previewsFolderId = GetPreviewsFolderId();

                if (previewsFolderId < 1)
                {
                    Logger.WriteWarning(ContentId, 0, "Previews folder not found, maybe the content is missing.");
                    downloadingSubtask.Finish();
                    return;
                }

                var fileInfo = GetFileInfo();

                if (fileInfo == null)
                {
                    Logger.WriteWarning(ContentId, 0, "Content not found.");
                    downloadingSubtask.Finish();
                    return;
                }

                downloadingSubtask.Progress(10, 100, 2, 110, "File info downloaded.");

                contentPath = fileInfo["Path"].Value<string>();

                //UNDONE: uncomment license check
                //CheckLicense(contentPath.Substring(contentPath.LastIndexOf('/') + 1));
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during initialization. The process will exit without generating images.", ex: ex, startIndex: StartIndex, version: Version);
                return;
            }

//if (contentPath.EndsWith("freeze.txt", StringComparison.OrdinalIgnoreCase))
//    Freeze();
//if (contentPath.EndsWith("overflow.txt", StringComparison.OrdinalIgnoreCase))
//    Overflow();

            using (var docStream = GetBinary())
            {
                if (docStream == null)
                {
                    Logger.WriteWarning(ContentId, 0, string.Format("Document not found; maybe the content or its version {0} is missing.", Version));
                    downloadingSubtask.Finish();
                    return;
                }

                downloadingSubtask.Progress(100, 100, 10, 110, "File downloaded.");
                
                if (docStream.Length == 0)
                {
                    SetPreviewStatus(0); // PreviewStatus.EmptyDocument
                    downloadingSubtask.Finish();
                    return;
                }
                downloadingSubtask.Finish();

                _generatingPreviewSubtask = new SnSubtask("Generating images");
                _generatingPreviewSubtask.Start();

                var extension = contentPath.Substring(contentPath.LastIndexOf('.'));
                PreviewImageGenerator.GeneratePreview(extension, docStream, new PreviewGenerationContext(
                    ContentId, previewsFolderId, StartIndex, MaxPreviewCount, Configuration.PreviewResolution, Version));

                _generatingPreviewSubtask.Finish();
            }
        }

        // ================================================================================================== Communication with the portal

        private static int GetPreviewsFolderId()
        {
            var url = GetUrl(SiteUrl, "GetPreviewsFolder", ContentId, new Dictionary<string, string> { { "version", Version } });
            var json = GetResponseJson(url, "POST", string.Format("{{ empty: {0} }}", (StartIndex == 0).ToString().ToLower()));

            return json != null && json["Id"] != null
                ? json["Id"].Value<int>()
                : 0;
        }

        private static Stream GetBinary()
        {
            var fileUrl = string.Format("{0}/binaryhandler.ashx?nodeid={1}&propertyname=Binary&version={2}", SiteUrl, ContentId, Version);

            var uri = new Uri(fileUrl);
            var myReq = WebRequest.Create(uri);
            var documentStream = new MemoryStream();

            SetAuthenticationForRequest(myReq);

            try
            {
                using (var wr = myReq.GetResponse())
                {
                    using (var rs = wr.GetResponseStream())
                    {
                        if (rs != null)
                            rs.CopyTo(documentStream);
                        else
                            Logger.WriteWarning(ContentId, 0, "The downloaded binary stream is null.");
                    }
                }
            }
            catch (WebException ex)
            {
                // 404 means the document has been deleted or the version (e.g. a draft version) does not
                // exist anymore. That is not an error, we should silently finish executing this task.
                if (Tools.ContentNotFound(ex))
                    return null;

                Logger.WriteError(ContentId, 0, "Error during remote file access.", ex, StartIndex, Version);

                // We need to throw the error further to let the main catch block
                // log the error and set the preview status to 'Error'.
                throw;
            }

            return documentStream;
        }

        private static JToken GetFileInfo()
        {
            var url = GetUrl(SiteUrl, null, ContentId, new Dictionary<string, string> 
            { 
                { "version", Version }, 
                { "$select", "Name,DisplayName,Path,CreatedById" },
                { "metadata", "no" }
            });

            var json = GetResponseJson(url);

            return json != null ? json["d"] : null;
        }

        private static void SetPreviewStatus(int status)
        {
            // REVIEWSTATUS ENUM in document provider

            // NoProvider = -5,
            // Postponed = -4,
            // Error = -3,
            // NotSupported = -2,
            // InProgress = -1,
            // EmptyDocument = 0,
            // Ready = 1

            var url = GetUrl(SiteUrl, "SetPreviewStatus", ContentId);

            GetResponseContent(url, "POST", string.Format("{{ status: {0} }}", status));
        }

        public static void SetPageCount(int pageCount)
        {
            var url = GetUrl(SiteUrl, "SetPageCount", ContentId);

            GetResponseContent(url, "POST", string.Format("{{ pageCount: {0} }}", pageCount));
        }

        public static void SavePreviewAndThumbnail(Stream imageStream, int page, int previewsFolderId)
        {
            // save main preview image
            SaveImageStream(imageStream, GetPreviewNameFromPageNumber(page), page, Common.PREVIEW_WIDTH, Common.PREVIEW_HEIGHT, previewsFolderId);
            var progress = ((page - StartIndex) * 2 - 1) * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);

            // save smaller image for thumbnail
            SaveImageStream(imageStream, GetThumbnailNameFromPageNumber(page), page, Common.THUMBNAIL_WIDTH, Common.THUMBNAIL_HEIGHT, previewsFolderId);
            progress = ((page - StartIndex) * 2) * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);
        }

        private static void SaveImageStream(Stream imageStream, string name, int page, int width, int height, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            using (var original = Image.FromStream(imageStream))
            {
                width = Math.Min(width, original.Width);
                height = Math.Min(height, original.Height);

                using (var resized = ResizeImage(original, width, height))
                {
                    if (resized == null)
                        return;

                    using (var memStream = new MemoryStream())
                    {
                        resized.Save(memStream, Common.PREVIEWIMAGEFORMAT);

                        SaveImageStream(memStream, name, page, previewsFolderId);
                    }
                }
            }
        }

        private static void SaveImageStream(Stream imageStream, string previewName, int page, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);

            try
            {
                var imageId = UploadImage(imageStream, previewsFolderId, previewName);

                // set initial preview image properties (CreatedBy, Index, etc.)
                var url = GetUrl(SiteUrl, "SetInitialPreviewProperties", imageId);

                GetResponseContent(url, "POST");
            }
            catch (WebException ex)
            {
                var logged = false;

                if (ex.Response != null)
                {
                    // a 404 must be handled by the caller
                    if (Tools.ContentNotFound(ex))
                        throw;

                    string responseContent = null;

                    using (var stream = ex.Response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                responseContent = reader.ReadToEnd();
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        // node is out of date is not an error
                        if (responseContent.IndexOf("NodeIsOutOfDateException", StringComparison.InvariantCulture) > 0)
                            return;
                        
                        Logger.WriteError(ContentId, page, responseContent, ex, StartIndex, Version);
                        logged = true;
                    }

                    // in case of status 500, we still have to terminate the process after logging the error
                    if (Tools.IsTerminatorError(ex))
                        throw;
                }

                if (!logged)
                    Logger.WriteError(ContentId, page, "Error during uploading a preview image.", ex, StartIndex, Version);
            }
        }

        public static void SaveEmptyPreview(int page, int previewsFolderId)
        {
            if (File.Exists(EmptyImage))
            {
                using (var emptyImage = Image.FromFile(EmptyImage))
                {
                    SaveImage(emptyImage, page, previewsFolderId);
                }
            }
            else
            {
                using (var emptyImage = new Bitmap(16, 16))
                {
                    SaveImage(emptyImage, page, previewsFolderId);
                }
            }
        }

        public static void SaveImage(Image image, int page, int previewsFolderId)
        {
            using (var imgStream = new MemoryStream())
            {
                image.Save(imgStream, Common.PREVIEWIMAGEFORMAT);
                SavePreviewAndThumbnail(imgStream, page, previewsFolderId);
            }
        }

        private static int UploadImage(Stream imageStream, int previewsFolderId, string imageName)
        {
            var imageStreamLength = imageStream.Length;
            var useChunk = imageStreamLength > Configuration.ChunkSizeInBytes;
            var url = GetUrl(SiteUrl, "Upload", previewsFolderId, new Dictionary<string, string> { { "create", "1" }, { "Overwrite", "true" } });
            var uploadedImageId = 0;
            var retryCount = 0;
            var token = string.Empty;

            // send initial request
            while (retryCount < REQUEST_RETRY_COUNT)
            {
                try
                {
                    var myReq = GetInitWebRequest(url, imageStreamLength, imageName);

                    using (var wr = myReq.GetResponse())
                    {
                        using (var stream = wr.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                token = reader.ReadToEnd();
                            }
                        }
                    }

                    // succesful request: skip out from retry loop
                    break;
                }
                catch (WebException)
                {
                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                        throw;
                    
                    Thread.Sleep(50);
                }

                retryCount++;
            }

            var boundary = "---------------------------" + DateTime.UtcNow.Ticks.ToString("x");
            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // send subsequent requests
            var buffer = new byte[Configuration.ChunkSizeInBytes];
            int bytesRead;
            var start = 0;

            while ((bytesRead = imageStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                url = GetUrl(SiteUrl, "Upload", previewsFolderId, new Dictionary<string, string> { { "Overwrite", "true" } });

                retryCount = 0;

                // get the request object for the actual chunk
                while (retryCount < REQUEST_RETRY_COUNT)
                {
                    var chunkRequest = GetChunkWebRequest(url, imageStreamLength, imageName, token, boundary);

                    if (useChunk)
                        chunkRequest.Headers.Set("Content-Range", string.Format("bytes {0}-{1}/{2}", start, start + bytesRead - 1, imageStreamLength));

                    // write the chunk into the request stream
                    using (var reqStream = chunkRequest.GetRequestStream())
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                        reqStream.Write(trailer, 0, trailer.Length);
                    }

                    // send the request
                    try
                    {
                        using (var wr = chunkRequest.GetResponse())
                        {
                            using (var stream = wr.GetResponseStream())
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    var imgContentJObject = JsonConvert.DeserializeObject(reader.ReadToEnd()) as JObject;

                                    uploadedImageId = imgContentJObject["Id"].Value<int>();
                                }
                            }
                        }

                        // successful request: skip out from the retry loop
                        break;
                    }
                    catch (WebException)
                    {
                        if (retryCount >= REQUEST_RETRY_COUNT - 1)
                            throw;
                        
                        Thread.Sleep(50);
                    }

                    retryCount++;
                }

                start += bytesRead;
            }

            return uploadedImageId;
        }

        // ================================================================================================== Helper methods

        public static void SetIndexes(int originalStartIndex, int pageCount, out int startIndex, out int lastIndex, int maxPreviewCount)
        {
            startIndex = Math.Min(originalStartIndex, pageCount - 1);
            lastIndex = Math.Min(startIndex + maxPreviewCount - 1, pageCount - 1);
        }

        private static string GetPreviewNameFromPageNumber(int page)
        {
            return string.Format(Common.PREVIEW_IMAGENAME, page);
        }

        private static string GetThumbnailNameFromPageNumber(int page)
        {
            return string.Format(Common.THUMBNAIL_IMAGENAME, page);
        }

        private static Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            if (image == null)
                return null;

            // do not scale up the image
            if (image.Width <= maxWidth && image.Height <= maxHeight)
                return image;

            int newWidth;
            int newHeight;

            ComputeResizedDimensions(image.Width, image.Height, maxWidth, maxHeight, out newWidth, out newHeight);

            try
            {
                var newImage = new Bitmap(newWidth, newHeight);
                newImage.SetResolution(Configuration.PreviewResolution, Configuration.PreviewResolution);

                using (var graphicsHandle = Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (OutOfMemoryException omex)
            {
                Logger.WriteError(ContentId, message: "Out of memory error during image resizing.", ex: omex, startIndex: StartIndex, version: Version);
                return null;
            }
        }

        private static void ComputeResizedDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
        {
            // do not scale up the image
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                newWidth = originalWidth;
                newHeight = originalHeight;
                return;
            }

            var percentWidth = (float)maxWidth / (float)originalWidth;
            var percentHeight = (float)maxHeight / (float)originalHeight;

            // determine which dimension scale should we use (the smaller)
            var percent = percentHeight < percentWidth ? percentHeight : percentWidth;

            // Compute new width and height, based on the final scale. Do not
            // allow values smaller than 1 because that would be an invalid
            // value for bitmap dimensions.
            newWidth = Math.Max(1, (int)Math.Round(originalWidth * percent));
            newHeight = Math.Max(1, (int)Math.Round(originalHeight * percent));
        }

        private static JToken GetResponseJson(string url, string verb = null, string body = null)
        {
            var responseText = GetResponseContent(url, verb, body);

            try
            {
                return string.IsNullOrEmpty(responseText) ? null : (JsonConvert.DeserializeObject(responseText) as JObject);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error during deserializing JSON response: " + responseText, ex);
            }
        }

        private static string GetResponseContent(string url, string verb = null, string body = null)
        {
            var retryCount = 0;

            while (retryCount < REQUEST_RETRY_COUNT)
            {
                var uri = new Uri(url);
                var myRequest = WebRequest.Create(uri);

                SetAuthenticationForRequest(myRequest);

                if (!string.IsNullOrEmpty(verb))
                {
                    myRequest.Method = verb;
                }

                if (!string.IsNullOrEmpty(body))
                {
                    myRequest.ContentLength = body.Length;

                    using (var requestWriter = new StreamWriter(myRequest.GetRequestStream()))
                    {
                        requestWriter.Write(body);
                    }
                }
                else
                {
                    myRequest.ContentLength = 0;
                }

                try
                {
                    using (var wr = myRequest.GetResponse())
                    {
                        using (var stream = wr.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    if (retryCount >= REQUEST_RETRY_COUNT - 1)
                        throw;
                    
                    Thread.Sleep(50);
                }

                retryCount++;
            }

            return string.Empty;
        }

        private static string GetUrl(string siteUrl, string odataFunctionName = null, int contentId = 0, IDictionary<string, string> parameters = null)
        {
            var url = string.Format("{0}/" + Configuration.ODataServiceToken + "/Content({1})", siteUrl, contentId);

            if (!string.IsNullOrEmpty(odataFunctionName))
                url += "/" + odataFunctionName;

            if (parameters != null && parameters.Keys.Count > 0)
                url += "?" + string.Join("&", parameters.Select(dk => string.Format("{0}={1}", dk.Key, dk.Value)));

            return url;
        }

        private static void SetAuthenticationForRequest(WebRequest myReq)
        {
            if (string.IsNullOrEmpty(Username))
            {
                // use NTLM authentication
                myReq.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                // use basic authentication
                var usernamePassword = Username + ":" + Password;
                myReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(usernamePassword)));
            }
        }

        private static WebRequest GetInitWebRequest(string url, long fileLength, string fileName)
        {
            var myReq = WebRequest.Create(new Uri(url));
            myReq.Method = "POST";

            SetAuthenticationForRequest(myReq);

            myReq.ContentType = "application/x-www-form-urlencoded";

            var useChunk = fileLength > Configuration.ChunkSizeInBytes;
            var postData = string.Format("ContentType=PreviewImage&FileName={0}&Overwrite=true&UseChunk={1}", fileName, useChunk);
            var postDataBytes = Encoding.ASCII.GetBytes(postData);

            myReq.ContentLength = postDataBytes.Length;

            using (var reqStream = myReq.GetRequestStream())
            {
                reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            }

            return myReq;
        }

        private static WebRequest GetChunkWebRequest(string url, long fileLength, string fileName, string token, string boundary)
        {
            var myReq = (HttpWebRequest)WebRequest.Create(new Uri(url));

            myReq.Method = "POST";
            myReq.ContentType = "multipart/form-data; boundary=" + boundary;
            myReq.KeepAlive = true;

            SetAuthenticationForRequest(myReq);

            myReq.Headers.Add("Content-Disposition", "attachment; filename=\"" + fileName + "\"");

            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

            var useChunk = fileLength > Configuration.ChunkSizeInBytes;
            var postValues = new NameValueCollection
                                 {
                                     {"ContentType", "PreviewImage"},
                                     {"FileName", fileName},
                                     {"Overwrite", "true"},
                                     {"UseChunk", useChunk.ToString()},
                                     {"ChunkToken", token}
                                 };

            // we must not close the stream after this as we need to write 
            // the chunk into it in the caller method
            var reqStream = myReq.GetRequestStream();

            // write form data values
            foreach (string key in postValues.Keys)
            {
                reqStream.Write(boundarybytes, 0, boundarybytes.Length);

                var formitem = string.Format(formdataTemplate, key, postValues[key]);
                var formitembytes = Encoding.UTF8.GetBytes(formitem);

                reqStream.Write(formitembytes, 0, formitembytes.Length);
            }

            // write a boundary
            reqStream.Write(boundarybytes, 0, boundarybytes.Length);

            // write file name and content type
            var header = string.Format(headerTemplate, "files[]", fileName);
            var headerbytes = Encoding.UTF8.GetBytes(header);

            reqStream.Write(headerbytes, 0, headerbytes.Length);

            return myReq;
        }

        private static bool ParseParameters(string[] args)
        {
            /* *********************************************************** */
            
            //UNDONE: remove hardcoded parameters
            ContentId = 1354;
            Version = "V1.0.A";
            StartIndex = 0;
            MaxPreviewCount = 3;
            SiteUrl = "https://localhost:44362";
            return true;

            /* *********************************************************** */

            foreach (var arg in args)
            {
                if (arg.StartsWith("USERNAME:", StringComparison.OrdinalIgnoreCase))
                {
                    Username = GetParameterValue(arg);
                }
                else if (arg.StartsWith("PASSWORD:", StringComparison.OrdinalIgnoreCase))
                {
                    Password = GetParameterValue(arg);
                }
                else if (arg.StartsWith("DATA:", StringComparison.OrdinalIgnoreCase))
                {
                    var data = GetParameterValue(arg).Replace("\"\"", "\"");

                    var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                    var serializer = JsonSerializer.Create(settings);
                    var jreader = new JsonTextReader(new StringReader(data));
                    dynamic previewData = serializer.Deserialize(jreader) as JObject;

                    ContentId = previewData.Id;
                    Version = previewData.Version;
                    StartIndex = previewData.StartIndex;
                    MaxPreviewCount = previewData.MaxPreviewCount;
                    SiteUrl = previewData.CommunicationUrl;
                }
            }

            return ContentId > 0 && !string.IsNullOrEmpty(Version) && StartIndex >= 0 && MaxPreviewCount > 0 && !string.IsNullOrEmpty(SiteUrl);
        }

        private static string GetParameterValue(string arg)
        {
            return arg.Substring(arg.IndexOf(":") + 1).TrimStart(new char[] { '\'', '"' }).TrimEnd(new char[] { '\'', '"' });
        }

        private static void CheckLicense(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.')).ToLower();
            var licensePath = Common.LICENSEPATH;
            try
            {
                if (Common.WORD_EXTENSIONS.Contains(extension))
                    new Aspose.Words.License().SetLicense(licensePath);
                else if (Common.IMAGE_EXTENSIONS.Contains(extension) || Common.TIFF_EXTENSIONS.Contains(extension))
                    new Aspose.Imaging.License().SetLicense(licensePath);
                else if (Common.DIAGRAM_EXTENSIONS.Contains(extension))
                    new Aspose.Diagram.License().SetLicense(licensePath);
                else if (Common.WORKBOOK_EXTENSIONS.Contains(extension))
                    new Aspose.Cells.License().SetLicense(licensePath);
                else if (Common.PDF_EXTENSIONS.Contains(extension))
                    new Aspose.Pdf.License().SetLicense(licensePath);
                else if (Common.PRESENTATION_EXTENSIONS.Contains(extension) || Common.PRESENTATIONEX_EXTENSIONS.Contains(extension))
                    new Aspose.Slides.License().SetLicense(licensePath);
                else if (Common.EMAIL_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Word for generating preview images from msg files
                    new Aspose.Email.License().SetLicense(licensePath);
                    new Aspose.Words.License().SetLicense(licensePath);
                }
                else if (Common.PROJECT_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Pdf for generating preview images from mpp files
                    new Aspose.Tasks.License().SetLicense(licensePath);
                    new Aspose.Pdf.License().SetLicense(licensePath);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during license check. ", ex: ex, startIndex: StartIndex, version: Version);
            }
        }
    }
}
