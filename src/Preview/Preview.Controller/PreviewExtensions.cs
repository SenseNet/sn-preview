using System;
using SenseNet.Preview;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class PreviewExtensions
    {
        [Obsolete("In a .Net Core environment please use the AddSenseNetDocumentPreviewProvider method instead.", true)]
        public static IRepositoryBuilder UseDocumentPreviewProvider(this IRepositoryBuilder repositoryBuilder, 
            DocumentPreviewProvider previewProvider)
        {
            return repositoryBuilder;
        }
    }
}
