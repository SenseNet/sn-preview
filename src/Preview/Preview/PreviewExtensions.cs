using Microsoft.Extensions.DependencyInjection;

namespace SenseNet.Preview
{
    public static class PreviewExtensions
    {
        /// <summary>
        /// Adds the provided preview provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPreviewGenerator<T>(this IServiceCollection services) where T : class, IPreviewImageGenerator
        {
            return services.AddSingleton<IPreviewImageGenerator, T>();
        }

        /// <summary>
        /// Adds the core preview feature to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPreview(this IServiceCollection services)
        {
            return services.AddSingleton<IPreviewGeneratorManager, PreviewGeneratorManager>();
        }
    }
}
