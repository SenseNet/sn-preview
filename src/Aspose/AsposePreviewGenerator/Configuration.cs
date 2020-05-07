using System.Configuration;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    //UNDONE: refactor configuration to use the modern API with a fallback
    public static class Configuration
    {
        private const string CHUNKSIZEKEY = "ChunkSize";
        private const int DefaultChunkSize = 10485760;
        private static int? _chunkSizeInBytes;
        public static int ChunkSizeInBytes
        {
            get
            {
                if (!_chunkSizeInBytes.HasValue)
                {
                    if (!int.TryParse(ConfigurationManager.AppSettings[CHUNKSIZEKEY], out var value))
                        value = DefaultChunkSize;
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

                if (!int.TryParse(ConfigurationManager.AppSettings[PREVIEWRESOLUTIONKEY], out var value))
                    value = DEFAULTPREVIEWRESOLUTION;
                
                _previewResolution = value;

                return _previewResolution.Value;
            }
        }

        //UNDONE: get client secret from configuration
        public static string ClientSecret => "secret";

        public static string ODataServiceToken { get; internal set; } =
            ConfigurationManager.AppSettings["ODataServiceToken"] ?? "odata.svc";
    }
}
