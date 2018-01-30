using System;
using System.IO;
using System.Net;
using Aspose.Imaging;
using Aspose.Imaging.ImageOptions;
using SenseNet.Preview;
using SeekOrigin = System.IO.SeekOrigin;

namespace SenseNet.Preview
{
    public class ImagePreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".gif", ".jpg", ".jpeg", ".bmp", ".png", ".svg", ".exif", ".icon" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            docStream.Seek(0, SeekOrigin.Begin);

            var document = Image.Load(docStream);

            if (context.StartIndex == 0)
                context.SetPageCount(1);

            using (var imgStream = new MemoryStream())
            {
                var options = new PngOptions();
                document.Save(imgStream, options);

                try
                {
                    context.SavePreviewAndThumbnail(imgStream, 1);
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
