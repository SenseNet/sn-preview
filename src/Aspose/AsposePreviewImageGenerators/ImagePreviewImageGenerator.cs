using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Imaging;
using Aspose.Imaging.ImageOptions;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class ImagePreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".gif", ".jpg", ".jpeg", ".bmp", ".png", ".svg", ".exif", ".icon" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = Image.Load(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(1, cancellationToken).ConfigureAwait(false);

            using (var imgStream = new MemoryStream())
            {
                var options = new PngOptions();
                document.Save(imgStream, options);

                try
                {
                    await context.SavePreviewAndThumbnailAsync(imgStream, 1, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (Tools.IsTerminatorError(ex as WebException))
                    {
                        context.LogWarning(1, SR.Exceptions.NotFound);
                        return;
                    }

                    // the preview generator tool will handle the error
                    throw;
                }
            }
        }
    }
}
