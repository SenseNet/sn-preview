using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Slides;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class PresentationPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".pot", ".pps", ".ppt", ".potx", ".ppsx", ".pptx", ".odp" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var pres = new Presentation(docStream);

            if (context.StartIndex == 0)
                await context.SetPageCountAsync(pres.Slides.Count, cancellationToken).ConfigureAwait(false);

            var loggedPageError = false;

            context.SetIndexes(pres.Slides.Count, out var firstIndex, out var lastIndex);

            // calculate size based on the original aspect ratio and the expected image size.
            var sizeF = pres.SlideSize.Size;
            var ratio = Math.Min((float)Common.PREVIEW_WIDTH / sizeF.Width, 
                                 (float)Common.PREVIEW_HEIGHT / sizeF.Height);
            var size = new Size((int)Math.Round(sizeF.Width * ratio),
                                (int)Math.Round(sizeF.Height * ratio));

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var slide = pres.Slides[i];

                    // generate image
                    using (var image = slide.GetThumbnail(size))
                    {
                        await context.SaveImageAsync(image, i + 1, cancellationToken).ConfigureAwait(false);
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
