using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsposeCells = Aspose.Cells;
using AsposeDiagram = Aspose.Diagram;
using AsposeImaging = Aspose.Imaging;
using AsposePdf = Aspose.Pdf;
using AsposeSlides = Aspose.Slides;
using AsposeEmail = Aspose.Email;
using AsposeTasks = Aspose.Tasks;
using AsposeWords = Aspose.Words;
using Aspose.Words.Drawing;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SNCR = SenseNet.ContentRepository;
using STORAGE = SenseNet.ContentRepository.Storage;

namespace SenseNet.Preview.Aspose
{
    public class AsposePreviewProvider : DocumentPreviewProvider
    {
        public static readonly string DefaultPreviewGeneratorTaskName = "AsposePreviewGenerator";
        public static readonly string DefaultPreviewGeneratorTaskTitle = "Generating preview";

        internal enum LicenseProvider { Words, Diagram, Cells, Pdf, Slides, Tasks, Imaging, Email }

        // ===================================================================================================== Properties

        protected static bool LicenseChecked { get; set; }

        // ===================================================================================================== Overrides

        public override string GetPreviewGeneratorTaskName(string contentPath)
        {
            var ext = Path.GetExtension(STORAGE.RepositoryPath.GetFileName(contentPath));
            return PreviewImageGenerator.GetTaskNameByFileNameExtension(ext) ?? DefaultPreviewGeneratorTaskName;
        }
        public override string GetPreviewGeneratorTaskTitle(string contentPath)
        {
            var ext = Path.GetExtension(STORAGE.RepositoryPath.GetFileName(contentPath));
            return PreviewImageGenerator.GetTaskTitleByFileNameExtension(ext) ?? DefaultPreviewGeneratorTaskTitle;
        }

        private static string[] _supportedTaskNames;
        public override string[] GetSupportedTaskNames()
        {
            if (_supportedTaskNames == null)
            {
                // Collect custom task names from the generator implementations (e.g. name of
                // a dedicated task for generating preview images for 3ds files)
                var supportedTaskNames = PreviewImageGenerator.GetSupportedCustomTaskNames().ToList();

                // extend the list with the default task name
                supportedTaskNames.Add(DefaultPreviewGeneratorTaskName);
                supportedTaskNames.Sort();

                _supportedTaskNames = supportedTaskNames.Distinct().ToArray();
            }

            return _supportedTaskNames;
        }
        
        public override bool IsContentSupported(STORAGE.Node content)
        {
            return PreviewImageGenerator.IsSupportedExtension(ContentNamingProvider.GetFileExtension(content.Name));
        }

        protected override Stream GetPreviewImagesDocumentStream(Content content, IEnumerable<SNCR.Image> previewImages, DocumentFormat documentFormat, RestrictionType? restrictionType = null)
        {
            if (documentFormat == DocumentFormat.NonDefined)
                documentFormat = GetFormatByName(content.Name);

            // Unfortunately we need to create a new memory stream here
            // instead of writing into the output stream directly, because
            // Aspose needs to Seek the stream during document creation, 
            // which is not supported by the Http Response output stream.

            switch (documentFormat)
            {
                case DocumentFormat.Doc:
                case DocumentFormat.Docx: return GetPreviewImagesWordStream(content, previewImages, restrictionType);
                case DocumentFormat.NonDefined:
                case DocumentFormat.Pdf: return GetPreviewImagesPdfStream(content, previewImages, restrictionType);
                case DocumentFormat.Ppt:
                case DocumentFormat.Pptx: return GetPreviewImagesPowerPointStream(content, previewImages, restrictionType);
                case DocumentFormat.Xls:
                case DocumentFormat.Xlsx: return GetPreviewImagesExcelStream(content, previewImages, restrictionType);
            }

            return null;
        }

        // ===================================================================================================== Generate documents

        protected Stream GetPreviewImagesPdfStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Pdf);

            try
            {
                var ms = new MemoryStream();
                using var document = new AsposePdf.Document();
                var index = 1;
                var pageAttributes = GetPageAttributes(content);            
                var imageOptions = new PreviewImageOptions() { RestrictionType = restrictionType };

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    // use the stored rotation value for this page if exists
                    int? rotation = pageAttributes.ContainsKey(previewImage.Index)
                        ? pageAttributes[previewImage.Index].degree
                        : null;

                    imageOptions.Rotation = rotation;

                    using (var imgStream = GetRestrictedImage(previewImage, imageOptions))
                    {
                        int newWidth;
                        int newHeight;

                        // Compute dimensions using a SQUARE (max width and height are equal).
                        ComputeResizedDimensionsWithRotation(previewImage, PREVIEW_PDF_HEIGHT, rotation, out newWidth, out newHeight);

                        var imageStamp = new AsposePdf.ImageStamp(imgStream)
                                             {
                                                 TopMargin = 10,
                                                 HorizontalAlignment = AsposePdf.HorizontalAlignment.Center,
                                                 VerticalAlignment = AsposePdf.VerticalAlignment.Top,
                                                 Width = newWidth,
                                                 Height = newHeight
                                             };

                        try
                        {
                            var page = index == 1 ? document.Pages[1] : document.Pages.Add();

                            // If the final image is landscape, we need to rotate the page 
                            // to fit the image. It does not matter if the original image 
                            // was landscape or the rotation made it that way.
                            if (newWidth > newHeight)
                            {
                                page.Rotate = AsposePdf.Rotation.on90;
                            }

                            page.AddStamp(imageStamp);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            SnLog.WriteException(ex, "Error during pdf generation. Path: " + previewImage.Path);
                            break;
                        }
                    }

                    index++;
                }

                document.Save(ms);

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesWordStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Words);

            try
            {
                var ms = new MemoryStream();
                var document = new AsposeWords.Document();
                var builder = new AsposeWords.DocumentBuilder(document);
                var index = 1;
                var saveFormat = content.Name.ToLower().EndsWith(".docx") ? AsposeWords.SaveFormat.Docx : AsposeWords.SaveFormat.Doc;
                var pageAttributes = GetPageAttributes(content);            
                var imageOptions = new PreviewImageOptions() { RestrictionType = restrictionType };

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    // use the stored rotation value for this page if exists
                    int? rotation = pageAttributes.ContainsKey(previewImage.Index)
                        ? pageAttributes[previewImage.Index].degree
                        : null;

                    imageOptions.Rotation = rotation;                                       

                    using (var imgStream = GetRestrictedImage(previewImage, imageOptions))
                    {
                        int newWidth;
                        int newHeight;

                        // Compute dimensions using a SQUARE (max width and height are equal).
                        ComputeResizedDimensionsWithRotation(previewImage, PREVIEW_WORD_HEIGHT, rotation, out newWidth, out newHeight);

                        try
                        {
                            // skip to the next page
                            if (index > 1)
                                builder.Writeln("");

                            // If the final image is landscape, we need to rotate the page 
                            // to fit the image. It does not matter if the original image 
                            // was landscape or the rotation made it that way.
                            // Switch orientation only if needed.
                            if (newWidth > newHeight)
                            {
                                if (builder.PageSetup.Orientation != AsposeWords.Orientation.Landscape)
                                {
                                    if (index > 1)
                                        builder.InsertBreak(AsposeWords.BreakType.SectionBreakContinuous);

                                    builder.PageSetup.Orientation = AsposeWords.Orientation.Landscape;
                                }
                            }
                            else
                            {
                                if (builder.PageSetup.Orientation != AsposeWords.Orientation.Portrait)
                                {
                                    if (index > 1)
                                        builder.InsertBreak(AsposeWords.BreakType.SectionBreakContinuous);

                                    builder.PageSetup.Orientation = AsposeWords.Orientation.Portrait;
                                }
                            }
                            
                            builder.InsertImage(imgStream,
                                                RelativeHorizontalPosition.LeftMargin,
                                                -5,
                                                RelativeVerticalPosition.TopMargin,
                                                -50,
                                                newWidth,
                                                newHeight,
                                                WrapType.Square);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            SnLog.WriteException(ex, "Error during document generation. Path: " + previewImage.Path);
                            break;
                        }
                    }

                    index++;
                }

                document.Save(ms, saveFormat);

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesExcelStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Cells);

            try
            {
                var ms = new MemoryStream();
                var oldExcel = content.Name.ToLower().EndsWith(".xls");
                var fileFormat = oldExcel ? AsposeCells.FileFormatType.Excel97To2003 : AsposeCells.FileFormatType.Xlsx;
                var saveFormat = oldExcel ? AsposeCells.SaveFormat.Excel97To2003 : AsposeCells.SaveFormat.Xlsx;
                var document = new AsposeCells.Workbook(fileFormat);
                var index = 1;
                var imageOptions = new PreviewImageOptions() { RestrictionType = restrictionType };

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    using (var imgStream = GetRestrictedImage(previewImage, imageOptions))
                    {
                        var image = System.Drawing.Image.FromStream(imgStream);
                        var imageForDocument = ResizeImage(image, Math.Min(image.Width, PREVIEW_EXCEL_WIDTH), Math.Min(image.Height, PREVIEW_EXCEL_HEIGHT));

                        if (imageForDocument != null)
                        {
                            using (var imageStream = new MemoryStream())
                            {
                                imageForDocument.Save(imageStream, Common.PREVIEWIMAGEFORMAT);

                                try
                                {
                                    var ws = index == 1 ? document.Worksheets[0] : document.Worksheets.Add("Sheet" + index);
                                    ws.Pictures.Add(0, 0, imageStream);
                                }
                                catch (IndexOutOfRangeException ex)
                                {
                                    SnLog.WriteException(ex, "Error during document generation. Path: " + previewImage.Path);
                                    break;
                                }
                            }
                        }
                    }

                    index++;
                }

                document.Save(ms, saveFormat);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return null;
        }

        protected Stream GetPreviewImagesPowerPointStream(Content content, IEnumerable<SNCR.Image> previewImages, RestrictionType? restrictionType = null)
        {
            CheckLicense(LicenseProvider.Slides);

            try
            {
                var ms = new MemoryStream();
                var extension = ContentNamingProvider.GetFileExtension(content.Name).ToLower();
                var oldPpt = Common.PRESENTATION_EXTENSIONS.Contains(extension);
                var saveFormat = oldPpt ? AsposeSlides.Export.SaveFormat.Ppt : AsposeSlides.Export.SaveFormat.Pptx;
                var docPresentation = new AsposeSlides.Presentation();
                var index = 1;
                var imageOptions = new PreviewImageOptions() { RestrictionType = restrictionType };

                foreach (var previewImage in previewImages.Where(previewImage => previewImage != null))
                {
                    using (var imgStream = GetRestrictedImage(previewImage, imageOptions))
                    {
                        var image = System.Drawing.Image.FromStream(imgStream);
                        var imageForDocument = ResizeImage(image, Math.Min(image.Width, Common.PREVIEW_POWERPOINT_WIDTH), Math.Min(image.Height, Common.PREVIEW_POWERPOINT_HEIGHT));

                        if (imageForDocument != null)
                        {
                            try
                            {
                                var img = docPresentation.Images.AddImage(imageForDocument);
                                var slide = docPresentation.Slides[0];
                                if (index > 1)
                                {
                                    docPresentation.Slides.AddClone(slide);
                                    slide = docPresentation.Slides[index - 1];
                                }

                                slide.Shapes.AddPictureFrame(AsposeSlides.ShapeType.Rectangle, 10, 10,
                                                             imageForDocument.Width, imageForDocument.Height,
                                                             img);
                            }
                            catch (IndexOutOfRangeException ex)
                            {
                                SnLog.WriteException(ex, "Error during document generation. Path: " + previewImage.Path);
                                break;
                            }
                        }
                    }

                    index++;
                }
                
                docPresentation.Save(ms, saveFormat);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return null;
        }

        // ===================================================================================================== Helper methods

        protected static void SetIndexes(string path, int originalStartIndex, int pageCount, out int startIndex, out int lastIndex)
        {
            var imageCount = Settings.GetValue(DOCUMENTPREVIEW_SETTINGS, MAXPREVIEWCOUNT, path, 10);

            SetIndexes(path, originalStartIndex, pageCount, out startIndex, out lastIndex, imageCount);
        }

        protected static void SetIndexes(string path, int originalStartIndex, int pageCount, out int startIndex, out int lastIndex, int maxPreviewCount)
        {
            startIndex = Math.Min(originalStartIndex, pageCount - 1);
            lastIndex = Math.Min(startIndex + maxPreviewCount - 1, pageCount - 1);
        }

        internal static void CheckLicense(LicenseProvider provider)
        {
            try
            {
                var licensePath = Common.LICENSEPATH;
                switch (provider)
                {
                    case LicenseProvider.Cells:
                        new AsposeCells.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Diagram:
                        new AsposeDiagram.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Pdf:
                        new AsposePdf.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Slides:
                        new AsposeSlides.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Words:
                        new AsposeWords.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Tasks:
                        new AsposeTasks.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Imaging:
                        new AsposeImaging.License().SetLicense(licensePath);
                        break;
                    case LicenseProvider.Email:
                        new AsposeEmail.License().SetLicense(licensePath);
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLicenseException(ex);
            }
        }

        protected static void WriteLicenseException(Exception ex)
        {
            var lex = new Exception("There was an error using Apose License (" + Common.LICENSEPATH + ")", ex);
            SnLog.WriteException(lex);
        }

        protected static DocumentFormat GetFormatByName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return DocumentFormat.Pdf;

            var extension = ContentNamingProvider.GetFileExtension(fileName).ToLower();

            if (Common.PDF_EXTENSIONS.Contains(extension))
                return DocumentFormat.Pdf;

            if (Common.PRESENTATION_EXTENSIONS.Contains(extension))
                return DocumentFormat.Ppt;

            if (Common.PRESENTATIONEX_EXTENSIONS.Contains(extension))
                return DocumentFormat.Pptx;

            if (Common.WORKBOOK_EXTENSIONS.Contains(extension))
                return extension.EndsWith("x") ? DocumentFormat.Xlsx : DocumentFormat.Xls;

            if (Common.WORD_EXTENSIONS.Contains(extension))
                return extension.EndsWith("x") ? DocumentFormat.Docx : DocumentFormat.Doc;

            if (Common.EMAIL_EXTENSIONS.Contains(extension))
                return DocumentFormat.Pdf;

            return DocumentFormat.Pdf;
        }
    }
}
