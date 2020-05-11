using System;
using System.Drawing;
using System.IO;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    public class PreviewGenerationContext : IPreviewGenerationContext
    {
        public int ContentId { get; }
        public int PreviewsFolderId { get; }
        public int StartIndex { get; }
        public int MaxPreviewCount { get; }
        public int PreviewResolution { get; }
        public string Version { get; }
        
        internal PreviewGenerationContext(int contentId, int previewsFolderId, int startIndex, int maxPreviewCount, int previewResolution, string version)
        {
            ContentId = contentId;
            PreviewsFolderId = previewsFolderId;
            StartIndex = startIndex;
            MaxPreviewCount = maxPreviewCount;
            PreviewResolution = previewResolution;
            Version = version;
        }

        public void SetPageCount(int pageCount)
        {
            Program.SetPageCountAsync(pageCount).GetAwaiter().GetResult();
        }
        public void SetIndexes(int pageCount, out int firstIndex, out int lastIndex)
        {
            Program.SetIndexes(StartIndex, pageCount, out firstIndex, out lastIndex, MaxPreviewCount);
        }
        public void SavePreviewAndThumbnail(Stream imgStream, int page)
        {
            Program.SavePreviewAndThumbnailAsync(imgStream, page, PreviewsFolderId).GetAwaiter().GetResult();
        }
        public void SaveEmptyPreview(int page)
        {
            Program.SaveEmptyPreviewAsync(page, PreviewsFolderId).GetAwaiter().GetResult();
        }
        public void SaveImage(Bitmap image, int page)
        {
            Program.SaveImageAsync(image, page, PreviewsFolderId).GetAwaiter().GetResult();
        }

        public void LogInfo(int page, string message)
        {
            Logger.WriteInfo(ContentId, page, message);
        }
        public void LogWarning(int page, string message)
        {
            Logger.WriteWarning(ContentId, page, message);
        }
        public void LogError(int page, string message = null, Exception ex = null)
        {
            Logger.WriteError(ContentId, page, message, ex, StartIndex, Version);
        }
    }
}
