using System;
using System.IO;
using Aspose.Diagram;
using Aspose.Diagram.Saving;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class DiagramPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".vdw", ".vdx", ".vsd", ".vss", ".vst", ".vsx", ".vtx" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var document = new Diagram(docStream);

            if (context.StartIndex == 0)
                context.SetPageCount(document.Pages.Count);

            int firstIndex;
            int lastIndex;
            var loggedPageError = false;

            context.SetIndexes(document.Pages.Count, out firstIndex, out lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                try
                {
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new ImageSaveOptions(SaveFileFormat.PNG)
                        {
                            PageIndex = i,
                            Resolution = context.PreviewResolution
                        };

                        document.Save(imgStream, options);
                        if (imgStream.Length == 0)
                            continue;

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
