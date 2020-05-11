using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using SenseNet.Client;
using SenseNet.Diagnostics;
using AsposeTools = SenseNet.Preview.Aspose.PreviewImageGenerators.Tools;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;
using ServerContext = SenseNet.Client.ServerContext;
using AsposeWords = Aspose.Words;
using AsposeImaging = Aspose.Imaging;
using AsposeDiagram = Aspose.Diagram;
using AsposeCells = Aspose.Cells;
using AsposePdf = Aspose.Pdf;
using AsposeSlides = Aspose.Slides;
using AsposeEmail = Aspose.Email;
using AsposeTasks = Aspose.Tasks;
    

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
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

        // shortcut
        public static Configuration Config => Configuration.Instance;

        private static int REQUEST_RETRY_COUNT = 3;
        private static string EmptyImage = "empty.png";

        private static SnSubtask _generatingPreviewSubtask;

        private static async Task Main(string[] args)
        {
            if (!ParseParameters(args))
            {
                Logger.WriteWarning(ContentId, 0, "Aspose preview generator process arguments are not correct.");
                return;
            }

            if (!await InitializeAsync())
                return;

            try
            {
                await GenerateImagesAsync();
            }
            catch (Exception ex)
            {
                if (AsposeTools.ContentNotFound(ex))
                    return;

                Logger.WriteError(ContentId, 0, ex: ex, startIndex: StartIndex, version: Version);

                await SetPreviewStatusAsync(-3); // PreviewStatus.Error
            }
        }

        private static async Task<bool> InitializeAsync()
        {
            SnTrace.EnableAll();
            Configuration.Initialize();

            ServicePointManager.DefaultConnectionLimit = 10;

            ClientContext.Current.ChunkSizeInBytes = Config.Upload.ChunkSize;
            ClientContext.Current.AddServer(new ServerContext
            {
                Url = SiteUrl,
                Username = Username,
                Password = Password,

                IsTrusted = Config.Environment.IsDevelopment
            });

            try
            {
                var token = await GetAccessTokenAsync();
                ClientContext.Current.Server.Authentication.AccessToken = token;

                if (string.IsNullOrEmpty(token))
                    SnTrace.System.Write("Access token is empty, fallback to user name and password.");
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, 0, ex: ex, startIndex: StartIndex, version: Version, message:
                    $"Authentication failed for site {SiteUrl}: {ex.Message}");
                return false;
            }

            return true;
        }

        // ================================================================================================== Preview generation

        private static async Task GenerateImagesAsync()
        {
            int previewsFolderId;
            string contentPath;
            var downloadingSubtask = new SnSubtask("Downloading", "Downloading file and other information");
            downloadingSubtask.Start();

            try
            {
                var fileInfo = await GetFileInfoAsync().ConfigureAwait(false);
                if (fileInfo == null)
                {
                    Logger.WriteWarning(ContentId, 0, "Content not found.");
                    downloadingSubtask.Finish();
                    return;
                }

                previewsFolderId = await GetPreviewsFolderIdAsync();
                if (previewsFolderId < 1)
                {
                    Logger.WriteWarning(ContentId, 0, "Previews folder not found, maybe the content is missing.");
                    downloadingSubtask.Finish();
                    return;
                }

                downloadingSubtask.Progress(10, 100, 2, 110, "File info downloaded.");

                contentPath = fileInfo.Path;

                if (Config.ImageGeneration.CheckLicense)
                    CheckLicense(contentPath.Substring(contentPath.LastIndexOf('/') + 1));
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during initialization. The process will exit without generating images.", ex: ex, startIndex: StartIndex, version: Version);
                return;
            }

            #region For tests
            //if (contentPath.EndsWith("freeze.txt", StringComparison.OrdinalIgnoreCase))
            //    Freeze();
            //if (contentPath.EndsWith("overflow.txt", StringComparison.OrdinalIgnoreCase))
            //    Overflow();
            #endregion

            await using var docStream = await GetBinaryAsync();
            if (docStream == null)
            {
                Logger.WriteWarning(ContentId, 0, $"Document not found; maybe the content or its version {Version} is missing.");
                downloadingSubtask.Finish();
                return;
            }

            downloadingSubtask.Progress(100, 100, 10, 110, "File downloaded.");
                
            if (docStream.Length == 0)
            {
                await SetPreviewStatusAsync(0); // PreviewStatus.EmptyDocument
                downloadingSubtask.Finish();
                return;
            }
            downloadingSubtask.Finish();

            _generatingPreviewSubtask = new SnSubtask("Generating images");
            _generatingPreviewSubtask.Start();

            var extension = contentPath.Substring(contentPath.LastIndexOf('.'));

            //UNDONE: make preview generation async
            PreviewImageGenerator.GeneratePreview(extension, docStream, new PreviewGenerationContext(
                ContentId, previewsFolderId, StartIndex, MaxPreviewCount, 
                Config.ImageGeneration.PreviewResolution, Version));

            _generatingPreviewSubtask.Finish();
        }

        // ================================================================================================== Communication with the portal

        private static async Task<int> GetPreviewsFolderIdAsync()
        {
            try
            {
                var previewsFolder = await GetResponseJsonAsync(new ODataRequest
                    {
                        ContentId = ContentId,
                        ActionName = "GetPreviewsFolder",
                        Version = Version
                    },
                    HttpMethod.Post,
                    new {empty = StartIndex == 0}).ConfigureAwait(false);

                return previewsFolder.Id;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, 0,$"GetPreviewsFolderId error: {ex.Message}", ex, version: Version);
            }

            return 0;
        }
        private static async Task<Stream> GetBinaryAsync()
        {
            var documentStream = new MemoryStream();

            try
            {
                // download the whole file from the server
                await RESTCaller.GetStreamResponseAsync(ContentId, Version, async response =>
                    {
                        if (response == null)
                            throw new ClientException($"Content {ContentId} {Version} not found.", 
                                HttpStatusCode.NotFound);
                        
                        await response.Content.CopyToAsync(documentStream).ConfigureAwait(false);
                        documentStream.Seek(0, SeekOrigin.Begin);
                    }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ClientException ex)
            {
                if (AsposeTools.ContentNotFound(ex))
                    return null;

                Logger.WriteError(ContentId, 0, "Error during remote file access.", ex, StartIndex, Version);

                // We need to throw the error further to let the main catch block
                // log the error and set the preview status to 'Error'.
                throw;
            }

            return documentStream;
        }
        private static Task<Content> GetFileInfoAsync()
        {
            return Content.LoadAsync(new ODataRequest
            {
                ContentId = ContentId,
                Select = new[] { "Name", "DisplayName", "Path", "CreatedById" },
                Version = Version,
                Metadata = MetadataFormat.None
            });
        }

        private static Task SetPreviewStatusAsync(int status)
        {
            // PREVIEWSTATUS ENUM in document provider

            // NoProvider = -5,
            // Postponed = -4,
            // Error = -3,
            // NotSupported = -2,
            // InProgress = -1,
            // EmptyDocument = 0,
            // Ready = 1

            return PostAsync("SetPreviewStatus", new {status});
        }
        public static Task SetPageCountAsync(int pageCount)
        {
            return PostAsync("SetPageCount", new { pageCount });
        }

        public static async Task SavePreviewAndThumbnailAsync(Stream imageStream, int page, int previewsFolderId)
        {
            // save main preview image
            await SaveImageStreamAsync(imageStream, GetPreviewNameFromPageNumber(page), page, 
                Common.PREVIEW_WIDTH, Common.PREVIEW_HEIGHT, previewsFolderId);

            var progress = ((page - StartIndex) * 2 - 1) * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);

            // save smaller image for thumbnail
            await SaveImageStreamAsync(imageStream, GetThumbnailNameFromPageNumber(page), page, 
                Common.THUMBNAIL_WIDTH, Common.THUMBNAIL_HEIGHT, previewsFolderId);

            progress = (page - StartIndex) * 2 * 100 / MaxPreviewCount / 2;
            _generatingPreviewSubtask.Progress(progress, 100, progress + 10, 110);
        }

        private static async Task SaveImageStreamAsync(Stream imageStream, string name, int page, int width, int height, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            using var original = Image.FromStream(imageStream);

            width = Math.Min(width, original.Width);
            height = Math.Min(height, original.Height);

            using var resized = ResizeImage(original, width, height);
            if (resized == null)
                return;

            await using var memStream = new MemoryStream();
            resized.Save(memStream, Common.PREVIEWIMAGEFORMAT);

            await SaveImageStreamAsync(memStream, name, page, previewsFolderId);
        }
        private static async Task SaveImageStreamAsync(Stream imageStream, string previewName, int page, int previewsFolderId)
        {
            imageStream.Seek(0, SeekOrigin.Begin);

            try
            {
                var imageId = await UploadImageAsync(imageStream, previewsFolderId, previewName);

                // set initial preview image properties (CreatedBy, Index, etc.)
                await PostAsync(imageId, "SetInitialPreviewProperties");
            }
            catch (ClientException ex)
            {
                // a 404 must be handled by the caller
                if (AsposeTools.ContentNotFound(ex))
                    throw;

                // node is out of date is not an error
                if (ex.ErrorData?.ExceptionType?.Contains("NodeIsOutOfDateException") ?? false)
                    return;

                Logger.WriteError(ContentId, page, ex.Message, ex, StartIndex, Version);
                
                // in case of status 500, we still have to terminate the process after logging the error
                if (AsposeTools.IsTerminatorError(ex))
                    throw;
            }
        }
        public static async Task SaveEmptyPreviewAsync(int page, int previewsFolderId)
        {
            if (File.Exists(EmptyImage))
            {
                using var emptyImage = Image.FromFile(EmptyImage);
                await SaveImageAsync(emptyImage, page, previewsFolderId);
            }
            else
            {
                using var emptyImage = new Bitmap(16, 16);
                await SaveImageAsync(emptyImage, page, previewsFolderId);
            }
        }
        public static Task SaveImageAsync(Image image, int page, int previewsFolderId)
        {
            using var imgStream = new MemoryStream();
            image.Save(imgStream, Common.PREVIEWIMAGEFORMAT);
            return SavePreviewAndThumbnailAsync(imgStream, page, previewsFolderId);
        }

        private static Task<int> UploadImageAsync(Stream imageStream, int previewsFolderId, string imageName)
        {
            return Retrier.RetryAsync(REQUEST_RETRY_COUNT, 50, async () =>
            {
                imageStream.Seek(0, SeekOrigin.Begin);

                var image = await Content.UploadAsync(previewsFolderId, imageName,
                    imageStream, "PreviewImage").ConfigureAwait(false);

                return image.Id;
            }, (result, count, ex) =>
            {
                if (ex == null)
                    return true;

                if (count == 1 || AsposeTools.ContentNotFound(ex))
                    throw ex;

                return false;
            });
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

            ComputeResizedDimensions(image.Width, image.Height, maxWidth, maxHeight, out var newWidth, out var newHeight);

            try
            {
                var newImage = new Bitmap(newWidth, newHeight);
                newImage.SetResolution(Config.ImageGeneration.PreviewResolution, 
                    Config.ImageGeneration.PreviewResolution);

                using (var graphicsHandle = Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (OutOfMemoryException ex)
            {
                Logger.WriteError(ContentId, message: "Out of memory error during image resizing.", ex: ex, startIndex: StartIndex, version: Version);
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

        private static Task<string> PostAsync(string actionName, object body = null)
        {
            return PostAsync(ContentId, actionName, body);
        }
        private static Task<string> PostAsync(int contentId, string actionName, object body = null)
        {
            var bodyText = body == null
                ? null
                : JsonHelper.Serialize(body);

            return GetResponseStringAsync(new ODataRequest
            {
                ContentId = contentId,
                ActionName = actionName
            }, HttpMethod.Post, bodyText);
        }

        private static Task<string> GetResponseStringAsync(ODataRequest request, HttpMethod method = null, string body = null)
        {
            return Retrier.RetryAsync(REQUEST_RETRY_COUNT, 50,
                async () => await RESTCaller.GetResponseStringAsync(request, method ?? HttpMethod.Get, body),
                (result, count, ex) =>
                {
                    if (ex == null)
                        return true;

                    // last try: throw the exception
                    if (count == 1 || AsposeTools.ContentNotFound(ex))
                        throw ex;

                    // failed, but give them a new chance
                    return false;
                });
        }
        private static Task<dynamic> GetResponseJsonAsync(ODataRequest request, HttpMethod method = null, object body = null)
        {
            return Retrier.RetryAsync(REQUEST_RETRY_COUNT, 50,
                async () => await RESTCaller.GetResponseJsonAsync(request, method: method ?? HttpMethod.Get, postData: body),
                (result, count, ex) =>
                {
                    if (ex == null)
                        return true;

                    // last try: throw the exception
                    if (count == 1 || AsposeTools.ContentNotFound(ex))
                        throw ex;

                    // failed, but give them a new chance
                    return false;
                });
        }
        
        private static async Task<string> GetAccessTokenAsync()
        {
            // get the configured authority from the sensenet service
            dynamic authInfo;
            try
            {
                authInfo = await RESTCaller.GetResponseJsonAsync(new ODataRequest
                {
                    Path = "/Root",
                    ActionName = "GetClientRequestParameters",
                    Parameters = { { "clientType", "client" } }
                }).ConfigureAwait(false);
            }
            catch
            {
                // Could not retrieve authentication info. This is OK in case of
                // an old content repository.
                SnTrace.System.Write($"Authority information could not be retrieved from the service: {SiteUrl}");
                return string.Empty;
            }

            string authority = authInfo.authority;

            SnTrace.System.Write($"Authority {authority} got from the service {authority}");

            // discover endpoints from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(authority);
            if (disco.IsError)
            {
                var message = $"Authority {authority} responded with an error: {disco.Error}, " +
                               "discovery document could not be retrieved.";

                throw new InvalidOperationException(message, disco.Exception);
            }

            SnTrace.System.Write("Discovery document received successfully.");

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = authInfo.client_id,
                ClientSecret = Config.Authentication.ClientSecret,
                Scope = "sensenet"
            }).ConfigureAwait(false);

            if (tokenResponse.IsError)
            {
                var message = $"Authority {authority} responded with an error: {tokenResponse.Error}, " +
                              "access token could not be retrieved.";

                throw new InvalidOperationException(message, tokenResponse.Exception);
            }

            SnTrace.System.Write("Client credentials access token received successfully.");

            return tokenResponse.AccessToken;
        }
        
        private static bool ParseParameters(string[] args)
        {
            /* *********************************************************** */
            
            //UNDONE: remove hardcoded parameters
            ContentId = 1140; //1368;
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
            return arg.Substring(arg.IndexOf(":", StringComparison.Ordinal) + 1).TrimStart('\'', '"').TrimEnd('\'', '"');
        }

        private static void CheckLicense(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.')).ToLower();
            var licensePath = Common.LICENSEPATH;
            try
            {
                if (Common.WORD_EXTENSIONS.Contains(extension))
                    new AsposeWords.License().SetLicense(licensePath);
                else if (Common.IMAGE_EXTENSIONS.Contains(extension) || Common.TIFF_EXTENSIONS.Contains(extension))
                    new AsposeImaging.License().SetLicense(licensePath);
                else if (Common.DIAGRAM_EXTENSIONS.Contains(extension))
                    new AsposeDiagram.License().SetLicense(licensePath);
                else if (Common.WORKBOOK_EXTENSIONS.Contains(extension))
                    new AsposeCells.License().SetLicense(licensePath);
                else if (Common.PDF_EXTENSIONS.Contains(extension))
                    new AsposePdf.License().SetLicense(licensePath);
                else if (Common.PRESENTATION_EXTENSIONS.Contains(extension) || Common.PRESENTATIONEX_EXTENSIONS.Contains(extension))
                    new AsposeSlides.License().SetLicense(licensePath);
                else if (Common.EMAIL_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Word for generating preview images from msg files
                    new AsposeEmail.License().SetLicense(licensePath);
                    new AsposeWords.License().SetLicense(licensePath);
                }
                else if (Common.PROJECT_EXTENSIONS.Contains(extension))
                {
                    // we use Aspose.Pdf for generating preview images from mpp files
                    new AsposeTasks.License().SetLicense(licensePath);
                    new AsposePdf.License().SetLicense(licensePath);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ContentId, message: "Error during license check. ", ex: ex, startIndex: StartIndex, version: Version);
            }
        }
    }
}
