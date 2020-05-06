using SenseNet.Tools;

namespace SenseNet.Preview.Aspose
{
    public static class AsposeExtensions
    {
        public static IRepositoryBuilder UseAsposeDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder)
        {
            repositoryBuilder.UseDocumentPreviewProvider(new AsposePreviewProvider());

            return repositoryBuilder;
        }
    }
}
