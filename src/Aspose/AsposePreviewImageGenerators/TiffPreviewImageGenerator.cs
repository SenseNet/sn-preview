using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Tiff;
using Aspose.Imaging.ImageOptions;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class TiffPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".tif", ".tiff" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = (TiffImage)Image.Load(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(document.Frames.Length, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(document.Frames.Length, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    document.ActiveFrame = document.Frames[i];
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new PngOptions();
                        document.Save(imgStream, options);

                        await context.SavePreviewAndThumbnailAsync(imgStream, i + 1, cancellationToken)
                            .ConfigureAwait(false);
                    }
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
