using SenseNet.Configuration;
using SenseNet.Preview;
using SenseNet.Tools;

namespace Preview.Controller
{
    public static class PreviewExtensions
    {
        public static IRepositoryBuilder UseDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder, 
            DocumentPreviewProvider previewProvider)
        {
            Providers.Instance.PreviewProvider = previewProvider;

            return repositoryBuilder;
        }
    }
}
