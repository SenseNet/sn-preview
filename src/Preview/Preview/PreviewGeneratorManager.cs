using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Preview
{
    public interface IPreviewGeneratorManager
    {
        string GetTaskNameByFileNameExtension(string extension);
        string GetTaskTitleByFileNameExtension(string extension);
        string[] GetSupportedCustomTaskNames();
        bool IsSupportedExtension(string extension);

        Task GeneratePreviewAsync(string extension, Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancel);
    }

    public class PreviewGeneratorManager : IPreviewGeneratorManager
    {
        private readonly Dictionary<string, IPreviewImageGenerator> _generators;

        public PreviewGeneratorManager(IEnumerable<IPreviewImageGenerator> generators)
        {
            _generators = BuildGeneratorList(generators);
        }

        private static Dictionary<string, IPreviewImageGenerator> BuildGeneratorList(IEnumerable<IPreviewImageGenerator> generators)
        {
            var providers = new Dictionary<string, IPreviewImageGenerator>();
            if (generators == null)
                return providers;

            // Iterate through all generators and decide which generator will be called
            // for which extension.
            foreach (var generator in generators)
            {
                foreach (var extension in generator.KnownExtensions)
                {
                    var ext = extension.ToLowerInvariant();
                    if (providers.TryGetValue(ext, out var existing))
                    {
                        // if we already have a more specific generator for this extension, DO NOT overwrite it
                        if (generator.GetType().IsInstanceOfType(existing))
                            continue;
                    }
                    providers[ext] = generator;
                }
            }
            
            return providers;
        }

        public string GetTaskNameByFileNameExtension(string extension)
        {
            if (!_generators.TryGetValue(extension.ToLowerInvariant(), out var provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            return provider.GetTaskNameByExtension(extension);
        }
        public string GetTaskTitleByFileNameExtension(string extension)
        {
            if (!_generators.TryGetValue(extension.ToLowerInvariant(), out var provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            return provider.GetTaskTitleByExtension(extension);
        }
        public string[] GetSupportedCustomTaskNames()
        {
            // collect all supported task names from the different generator implementations
            return _generators.Values.Select(pig => pig.GetSupportedTaskNames())
                .Where(names => names != null)
                .SelectMany(names => names)
                .Where(tn => !string.IsNullOrEmpty(tn))
                .Distinct().OrderBy(tn => tn).ToArray();
        }
        public bool IsSupportedExtension(string extension)
        {
            return _generators.ContainsKey(extension.ToLowerInvariant());
        }

        public Task GeneratePreviewAsync(string extension, Stream docStream, IPreviewGenerationContext context,
            CancellationToken cancel)
        {
            if (!_generators.TryGetValue(extension.ToLowerInvariant(), out var provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));

            return provider.GeneratePreviewAsync(docStream, context, cancel);
        }
    }
}
