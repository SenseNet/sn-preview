using System;
using System.IO;
using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Tiff;
using Aspose.Imaging.ImageOptions;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class TiffPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".tif", ".tiff" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = (TiffImage)Image.Load(docStream);

            if (context.StartIndex == 0)
                context.SetPageCount(document.Frames.Length);

            int firstIndex;
            int lastIndex;
            var loggedPageError = false;

            context.SetIndexes(document.Frames.Length, out firstIndex, out lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                try
                {
                    document.ActiveFrame = document.Frames[i];
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new PngOptions();
                        document.Save(imgStream, options);

                        context.SavePreviewAndThumbnail(imgStream, i + 1);
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
