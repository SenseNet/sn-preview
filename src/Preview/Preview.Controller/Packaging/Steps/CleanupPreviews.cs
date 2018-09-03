using System.Threading;
using SenseNet.Packaging.Steps;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;

namespace SenseNet.Preview.Packaging.Steps
{
    public enum CleanupMode
    {
        AllVersions,
        KeepLastVersions,
        EmptyFoldersOnly
    }
    
    public class CleanupPreviews : Step
    {
        public string Path { get; set; }
        public int MaxIndex { get; set; }
        public CleanupMode Mode { get; set; }

        public int MaxDegreeOfParallelism { get; set; } = 10;
        public int BlockSize { get; set; } = 500;

        private ExecutionContext _context;
        private int _folderCount;
        private int _imageCount;

        public override void Execute(ExecutionContext context)
        {
            _context = context;

            using (new Timer(state => WriteProgress(), null, 1000, 2000))
            {
                var pc = new PreviewCleaner(Path, Mode, MaxIndex, MaxDegreeOfParallelism, BlockSize);
                pc.OnFolderDeleted += (s, e) => { Interlocked.Increment(ref _folderCount);};
                pc.OnImageDeleted += (s, e) => { Interlocked.Increment(ref _imageCount); };

                pc.Execute();
            }

            WriteProgress();
            _context?.Console?.WriteLine();
        }

        private void WriteProgress()
        {
            _context?.Console?.Write($"  Deleted folders: {_folderCount}, images: {_imageCount}                        \r");
        }
    }
}