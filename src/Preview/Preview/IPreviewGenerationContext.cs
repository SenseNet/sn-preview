using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    public interface IPreviewGenerationContext
    {
        int ContentId { get; }
        int PreviewsFolderId { get; }
        int StartIndex { get; }
        int MaxPreviewCount { get; }
        int PreviewResolution { get; }
        string Version { get; }

        Task SetPageCountAsync(int pageCount, CancellationToken cancellationToken);
        void SetIndexes(int pageCount, out int firstIndex, out int lastIndex);

        Task SavePreviewAndThumbnailAsync(Stream imgStream, int page, CancellationToken cancellationToken);
        Task SaveEmptyPreviewAsync(int page, CancellationToken cancellationToken);
        Task SaveImageAsync(Bitmap image, int page, CancellationToken cancellationToken);

        void LogInfo(int page, string message);
        void LogWarning(int page, string message);
        void LogError(int page, string message = null, Exception ex = null);
    }
}
