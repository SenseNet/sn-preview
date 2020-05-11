using System.IO;
using System.Threading;
using Aspose.Tasks;
using Aspose.Tasks.Saving;

namespace SenseNet.Preview.Aspose.PreviewImageGenerators
{
    public class ProjectPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".mpp" };

        public override async System.Threading.Tasks.Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancellationToken)
        {
            var document = new Project(docStream);
            
            // This is the simplest way to create a reasonably readable 
            // preview from a project file: convert it to a PDF first.
            using (var pdfStream = new MemoryStream())
            {
                // save project file in memory as a pdf document
                document.Save(pdfStream, SaveFileFormat.PDF);

                // generate previews from the pdf document
                await new PdfPreviewImageGenerator().GeneratePreviewAsync(pdfStream, context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
