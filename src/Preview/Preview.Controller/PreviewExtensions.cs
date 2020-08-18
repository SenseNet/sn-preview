using System;
using SenseNet.Configuration;
using SenseNet.Preview;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class PreviewExtensions
    {
        [Obsolete("In a .Net Core environment please use the AddSenseNetDocumentPreviewProvider method instead.")]
        public static IRepositoryBuilder UseDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder, 
            DocumentPreviewProvider previewProvider)
        {
            Providers.Instance.PreviewProvider = previewProvider;

            return repositoryBuilder;
        }
    }
}
