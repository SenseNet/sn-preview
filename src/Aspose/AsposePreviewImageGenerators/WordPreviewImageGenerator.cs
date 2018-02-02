using System;
using System.IO;

namespace SenseNet.Preview
{
    public class WordPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".doc", ".docx", ".odt", ".rtf", ".txt", ".xml", ".csv" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var document = new Aspose.Words.Document(docStream);
            var pc = document.PageCount;

            // save the document only if this is the first round
            if (context.StartIndex == 0 || pc < 1)
                context.SetPageCount(pc);

            if (pc <= 0)
                return;

            int firstIndex;
            int lastIndex;
            var loggedPageError = false;

            context.SetIndexes(pc, out firstIndex, out lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                try
                {
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new Aspose.Words.Saving.ImageSaveOptions(Aspose.Words.SaveFormat.Png)
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
