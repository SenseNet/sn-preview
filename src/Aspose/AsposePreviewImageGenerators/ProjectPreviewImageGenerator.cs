using System.IO;
using Aspose.Tasks;
using Aspose.Tasks.Saving;

namespace SenseNet.Preview
{
    public class ProjectPreviewImageGenerator : PreviewImageGenerator
    {
        public override string[] KnownExtensions { get; } = { ".mpp" };

        public override void GeneratePreview(Stream docStream, IPreviewGenerationContext context)
        {
            var document = new Project(docStream);
            
            // This is the simplest way to create a reasonably readable 
            // preview from a project file: convert it to a PDF first.
            using (var pdfStream = new MemoryStream())
            {
                // save project file in memory as a pdf document
                document.Save(pdfStream, SaveFileFormat.PDF);

                // generate previews from the pdf document
                new PdfPreviewImageGenerator().GeneratePreview(pdfStream, context);
            }
        }
    }
}
