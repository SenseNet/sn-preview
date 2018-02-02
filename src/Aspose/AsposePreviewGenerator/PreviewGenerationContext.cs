using System;
using System.Drawing;
using System.IO;

namespace SenseNet.Preview.AsposePreviewGenerator
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
            this.ContentId = contentId;
            this.PreviewsFolderId = previewsFolderId;
            this.StartIndex = startIndex;
            this.MaxPreviewCount = maxPreviewCount;
            this.PreviewResolution = previewResolution;
            this.Version = version;
        }

        public void SetPageCount(int pageCount)
        {
            Program.SetPageCount(pageCount);
        }
        public void SetIndexes(int pageCount, out int firstIndex, out int lastIndex)
        {
            Program.SetIndexes(StartIndex, pageCount, out firstIndex, out lastIndex, MaxPreviewCount);
        }
        public void SavePreviewAndThumbnail(Stream imgStream, int page)
        {
            Program.SavePreviewAndThumbnail(imgStream, page, PreviewsFolderId);
        }
        public void SaveEmptyPreview(int page)
        {
            Program.SaveEmptyPreview(page, PreviewsFolderId);
        }
        public void SaveImage(Bitmap image, int page)
        {
            Program.SaveImage(image, page, PreviewsFolderId);
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
