using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Pdf;
using Aspose.Pdf.Devices;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class PdfPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".pdf" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            var document = new Document(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(document.Pages.Count, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(document.Pages.Count, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var imgStream = new MemoryStream())
                {
                    try
                    {
                        var pngDevice = new PngDevice(new Resolution(context.PreviewResolution, context.PreviewResolution));
                        pngDevice.Process(document.Pages[i + 1], imgStream);
                        if (imgStream.Length == 0)
                            continue;

                        await context.SavePreviewAndThumbnailAsync(imgStream, i + 1, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (await Tools.HandlePageErrorAsync(ex, i + 1, context, !loggedPageError,
                            cancellationToken).ConfigureAwait(false))
                            return;

                        loggedPageError = true;
                    }
                }
            }
        }
    }
}
