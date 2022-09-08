using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    public abstract class PreviewImageGenerator : IPreviewImageGenerator
    {
        public abstract string[] KnownExtensions { get; }
        public abstract Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context, 
            CancellationToken cancellationToken);
        public virtual string GetTaskNameByExtension(string extension)
        {
            // means default
            return null;
        }
        public virtual string GetTaskTitleByExtension(string extension)
        {
            // means default
            return null;
        }
        public virtual string[] GetSupportedTaskNames()
        {
            // fallback to default task name, defined by the preview provider
            return null;
        }
    }
}
