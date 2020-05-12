using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    public interface IPreviewImageGenerator
    {
        string[] KnownExtensions { get; }
        string GetTaskNameByExtension(string extension);
        string GetTaskTitleByExtension(string extension);
        string[] GetSupportedTaskNames();
        Task GeneratePreviewAsync(Stream docStream, IPreviewGenerationContext context, CancellationToken cancellationToken);
    }
}
