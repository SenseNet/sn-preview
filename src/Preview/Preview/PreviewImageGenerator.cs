using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SenseNet.Preview
{
    public abstract class PreviewImageGenerator : IPreviewImageGenerator
    {
        private readonly ILogger<PreviewImageGenerator> _logger;

        protected PreviewImageGenerator(ILogger<PreviewImageGenerator> logger)
        {
            _logger = logger;
        }

        public abstract string[] KnownExtensions { get; }
        public abstract Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context, 
            CancellationToken cancellationToken);

        public virtual string GetTaskNameByExtension(string extension)
        {
            // fallback to default
            return null;
        }
        public virtual string GetTaskTitleByExtension(string extension)
        {
            // fallback to default
            return null;
        }
        public virtual string[] GetSupportedTaskNames()
        {
            // fallback to default task name, defined by the preview provider
            return null;
        }
    }
}
