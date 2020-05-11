using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Email;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class EmailPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".msg" };

        public override async Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            var email = MailMessage.Load(docStream);

            using (var emailStream = new MemoryStream())
            {
                email.Save(emailStream, SaveOptions.DefaultMhtml);
                emailStream.Position = 0;

                await new WordPreviewImageGenerator().GeneratePreviewAsync(emailStream, context, 
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
