using System;
using System.IO;
using Aspose.Pdf;
using Aspose.Pdf.Devices;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class PdfPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".pdf" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var document = new Document(docStream);

            if (context.StartIndex == 0)
                context.SetPageCount(document.Pages.Count);

            var loggedPageError = false;

            context.SetIndexes(document.Pages.Count, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                using (var imgStream = new MemoryStream())
                {
                    try
                    {
                        var pngDevice = new PngDevice(new Resolution(context.PreviewResolution, context.PreviewResolution));
                        pngDevice.Process(document.Pages[i + 1], imgStream);
                        if (imgStream.Length == 0)
                            continue;

                        context.SavePreviewAndThumbnail(imgStream, i + 1);
                    }
                    catch (Exception ex)
                    {
                        if (Tools.HandlePageError(ex, i + 1, context, !loggedPageError))
                            return;

                        loggedPageError = true;
                    }
                }
            }
        }
    }
}
