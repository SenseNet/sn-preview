using System;
using System.Drawing;
using System.IO;
using Aspose.Slides;

namespace SenseNet.Preview
{
    public class PresentationPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".pot", ".pps", ".ppt", ".potx", ".ppsx", ".pptx", ".odp" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var pres = new Presentation(docStream);

            if (context.StartIndex == 0)
                context.SetPageCount(pres.Slides.Count);

            int firstIndex;
            int lastIndex;
            var loggedPageError = false;

            context.SetIndexes(pres.Slides.Count, out firstIndex, out lastIndex);

            // calculate size based on the original aspect ratio and the expected image size.
            var sizeF = pres.SlideSize.Size;
            var ratio = Math.Min((float)Common.PREVIEW_WIDTH / sizeF.Width, 
                                 (float)Common.PREVIEW_HEIGHT / sizeF.Height);
            var size = new Size((int)Math.Round(sizeF.Width * ratio),
                                (int)Math.Round(sizeF.Height * ratio));

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                try
                {
                    var slide = pres.Slides[i];

                    // generate image
                    using (var image = slide.GetThumbnail(size))
                    {
                        context.SaveImage(image, i + 1);
                    }
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
