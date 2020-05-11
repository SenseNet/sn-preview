using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Saving;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class WordPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".doc", ".docx", ".odt", ".rtf", ".txt", ".xml", ".csv" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            var document = new Document(docStream);
            var pc = document.PageCount;

            // save the document only if this is the first round
            if (context.StartIndex == 0 || pc < 1)
                await context.SetPageCountAsync(pc, cancellationToken).ConfigureAwait(false);

            if (pc <= 0)
                return;

            var loggedPageError = false;

            context.SetIndexes(pc, out var firstIndex, out var lastIndex);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var imgStream = new MemoryStream())
                    {
                        var options = new ImageSaveOptions(SaveFormat.Png)
                        {
                            PageIndex = i,
                            Resolution = context.PreviewResolution
                        };

                        document.Save(imgStream, options);
                        if (imgStream.Length == 0)
                            continue;

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
