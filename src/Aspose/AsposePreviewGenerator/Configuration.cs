using System;
using Microsoft.Extensions.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    #region Helper classes
    public class UploadConfig
    {
        /// <summary>
        /// Upload chunk size in bytes.
        /// </summary>
        public int ChunkSize { get; set; } = 10485760;
    }
    public class ImageGenerationConfig
    {
        public int PreviewResolution { get; set; } = 300;
        public bool CheckLicense { get; set; } = true;
    }
    public class AuthenticationConfig
    {
        public string ClientSecret { get; set; } = "secret";
    }

    public class EnvironmentConfig
    {
        public bool IsDevelopment { get; set; }
    }

    #endregion

    public class Configuration
    {
        public AuthenticationConfig Authentication { get; } = new AuthenticationConfig();
        public UploadConfig Upload { get; } = new UploadConfig();
        public ImageGenerationConfig ImageGeneration { get; } = new ImageGenerationConfig();
        public EnvironmentConfig Environment { get; } = new EnvironmentConfig();
        
        internal static Configuration Instance { get; private set; }

        public static void Initialize()
        {
            var configuration = new Configuration();

            try
            {
                var configurationSource = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();

                configurationSource.Bind(configuration);
                
                var environmentName = configurationSource.GetValue("NETCORE_ENVIRONMENT", "Production");

                configuration.Environment.IsDevelopment = string.Equals(environmentName, "Development",
                    StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                SnTrace.System.WriteError($"Error loading configuration: {ex.Message}. Working with defaults.");
            }

            SnTrace.System.Write($"Configuration: chunk size: {configuration.Upload.ChunkSize}, " +
                                 $"preview resolution: {configuration.ImageGeneration.PreviewResolution}");
            
            Instance = configuration;
        }
    }
}
