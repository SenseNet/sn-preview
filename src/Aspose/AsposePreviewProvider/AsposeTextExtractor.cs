using Aspose.Pdf.Text;
using SenseNet.Search;
using System.IO;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    public class AsposePdfTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            Task.Run(() =>
            {
                AsposePreviewProvider.CheckLicense(AsposePreviewProvider.LicenseProvider.Pdf);
                var document = new Aspose.Pdf.Document(stream);
                var textAbsorber = new TextAbsorber();
                document.Pages.Accept(textAbsorber);
                ContentRepository.Search.Indexing.IndexingTools.AddTextExtract(context.VersionId, textAbsorber.Text);
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

                var document = new Aspose.Words.Document(stream);

                ContentRepository.Search.Indexing.IndexingTools.AddTextExtract(context.VersionId, document.GetText());
            });

            return string.Empty;
        }
    }
}
