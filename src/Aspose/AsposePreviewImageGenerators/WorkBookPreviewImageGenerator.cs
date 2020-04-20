using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Aspose.Cells;
using Aspose.Cells.Drawing;
using Aspose.Cells.Rendering;

namespace SenseNet.Preview
{
    public class WorkBookPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".ods", ".xls", ".xlsm", ".xlsx", ".xltm", ".xltx" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var document = new Workbook(docStream);
            var printOptions = new ImageOrPrintOptions
            {
                ImageType = GetImageType(),
                OnePagePerSheet = false,
                HorizontalResolution = context.PreviewResolution,
                VerticalResolution = context.PreviewResolution
            };
            
            // every worksheet may contain multiple pages (as set by Excel 
            // automatically, or by the user using the print layout)
            var estimatedPageCount = document.Worksheets.Select(w => new SheetRender(w, printOptions).PageCount).Sum();

            if (context.StartIndex == 0)
                context.SetPageCount(estimatedPageCount);

            int firstIndex;
            int lastIndex;

            context.SetIndexes(estimatedPageCount, out firstIndex, out lastIndex);

            var workbookPageIndex = 0;
            var worksheetIndex = 0;
            var loggedPageError = false;

            // iterate through worksheets
            while (worksheetIndex < document.Worksheets.Count)
            {
                try
                {
                    var worksheet = document.Worksheets[worksheetIndex];
                    var sheetRender = new SheetRender(worksheet, printOptions);

                    // if we need to start preview generation on a subsequent worksheet, skip the previous ones
                    if (workbookPageIndex + sheetRender.PageCount < context.StartIndex)
                    {
                        workbookPageIndex += sheetRender.PageCount;
                        worksheetIndex++;
                        continue;
                    }

                    // iterate through pages inside a worksheet
                    for (var worksheetPageIndex = 0; worksheetPageIndex < sheetRender.PageCount; worksheetPageIndex++)
                    {
                        // if the desired page interval contains this page, generate the image
                        if (workbookPageIndex >= firstIndex && workbookPageIndex <= lastIndex)
                        {
                            using (var imgStream = new MemoryStream())
                            {
                                sheetRender.ToImage(worksheetPageIndex, imgStream);

                                // handle empty sheets
                                if (imgStream.Length == 0)
                                    context.SaveEmptyPreview(workbookPageIndex + 1);
                                else
                                    context.SavePreviewAndThumbnail(imgStream, workbookPageIndex + 1);
                            }
                        }

                        workbookPageIndex++;
                    }
                }
                catch (Exception ex)
                {
                    if (Tools.HandlePageError(ex, workbookPageIndex + 1, context, !loggedPageError))
                        return;

                    loggedPageError = true;
                    workbookPageIndex++;
                }

                worksheetIndex++;
            }

            // set the real count if some of the sheets turned out to be empty
            if (workbookPageIndex < estimatedPageCount)
                context.SetPageCount(workbookPageIndex);
        }

        private static ImageType GetImageType()
        {
            if (Common.PREVIEWIMAGEFORMAT.Equals(ImageFormat.Png))
                return ImageType.Png;
            if (Common.PREVIEWIMAGEFORMAT.Equals(ImageFormat.Jpeg))
                return ImageType.Jpeg;
            if (Common.PREVIEWIMAGEFORMAT.Equals(ImageFormat.Bmp))
                return ImageType.Bmp;
            if (Common.PREVIEWIMAGEFORMAT.Equals(ImageFormat.Gif))
                return ImageType.Gif;

            return ImageType.Png;
        }
    }
}
