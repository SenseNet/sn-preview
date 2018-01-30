using System.Configuration;

namespace SenseNet.Preview.AsposePreviewGenerator
{
    public static class Configuration
    {
        private const string CHUNKSIZEKEY = "ChunkSize";
        private const int DEFAULCHUNKSIZE = 10485760;
        private static int? _chunkSizeInBytes;
        public static int ChunkSizeInBytes
        {
            get
            {
                if (!_chunkSizeInBytes.HasValue)
                {
                    int value;
                    if (!int.TryParse(ConfigurationManager.AppSettings[CHUNKSIZEKEY], out value))
                        value = DEFAULCHUNKSIZE;
                    _chunkSizeInBytes = value;
                }
                return _chunkSizeInBytes.Value;
            }
        }

        private const string PREVIEWRESOLUTIONKEY = "PreviewResolution";
        private const int DEFAULTPREVIEWRESOLUTION = 300;
        private static int? _previewResolution;
        public static int PreviewResolution
        {
            get
            {
                if (_previewResolution.HasValue) 
                    return _previewResolution.Value;

                int value;
                if (!int.TryParse(ConfigurationManager.AppSettings[PREVIEWRESOLUTIONKEY], out value))
                    value = DEFAULTPREVIEWRESOLUTION;
                
                _previewResolution = value;

                return _previewResolution.Value;
            }
        }

        public static string ODataServiceToken { get; internal set; } =
            ConfigurationManager.AppSettings["ODataServiceToken"] ?? "odata.svc";
    }
}
