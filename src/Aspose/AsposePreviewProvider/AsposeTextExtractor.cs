using Aspose.Pdf.Text;
using System.IO;
using System.Threading.Tasks;
using AsposePdf = Aspose.Pdf;
using AsposeWords = Aspose.Words;
using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.Preview.Aspose
{
    public class AsposePdfTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            Task.Run(() =>
            {
                AsposePreviewProvider.CheckLicense(AsposePreviewProvider.LicenseProvider.Pdf);
                var document = new AsposePdf.Document(stream);
                var textAbsorber = new TextAbsorber();
                document.Pages.Accept(textAbsorber);
                IndexingTools.AddTextExtract(context.VersionId, textAbsorber.Text);
            });

            return string.Empty;
        }

    }

    public class AsposeRtfTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            Task.Run(() =>
            {
                AsposePreviewProvider.CheckLicense(AsposePreviewProvider.LicenseProvider.Words);

                var document = new AsposeWords.Document(stream);

                IndexingTools.AddTextExtract(context.VersionId, document.GetText());
            });

            return string.Empty;
        }
    }
}
