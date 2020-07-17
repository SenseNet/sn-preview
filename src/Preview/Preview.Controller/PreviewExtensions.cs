using SenseNet.Configuration;
using SenseNet.Preview;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
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
