using System.IO;
using Aspose.Email;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class EmailPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".msg" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var email = MailMessage.Load(docStream);

            using (var emailStream = new MemoryStream())
            {
                email.Save(emailStream, SaveOptions.DefaultMhtml);
                emailStream.Position = 0;

                new WordPreviewImageGenerator().GeneratePreview(emailStream, context);
            }
        }
    }
}
