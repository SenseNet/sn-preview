﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Tools;

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

        // ====================================================================================================================

        private static readonly object LoaderSync = new object();
        private static Dictionary<string, IPreviewImageGenerator> _providers;
        private static Dictionary<string, IPreviewImageGenerator> Providers
        {
            get
            {
                if (_providers == null)
                    lock (LoaderSync)
                        if (_providers == null)
                            _providers = CreateProviderPrototypes();
                return _providers;
            }
        }
        private static Dictionary<string, IPreviewImageGenerator> CreateProviderPrototypes()
        {
            var providers = new Dictionary<string, IPreviewImageGenerator>();
            var providerTypesA = TypeResolver.GetTypesByInterface(typeof(IPreviewImageGenerator));
            foreach (var providerType in providerTypesA)
            {
                if (providerType.IsAbstract)
                    continue;

                var provider = (IPreviewImageGenerator)Activator.CreateInstance(providerType);
                foreach (var extension in provider.KnownExtensions)
                {
                    var ext = extension.ToLowerInvariant();
                    if (providers.TryGetValue(ext, out var existing))
                    {
                        if (providerType.IsInstanceOfType(existing))
                            continue;
                    }
                    providers[ext] = provider;
                }
            }
            return providers;
        }

        public static string GetTaskNameByFileNameExtension(string extension)
        {
            IPreviewImageGenerator provider;
            if (!Providers.TryGetValue(extension.ToLowerInvariant(), out provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            return provider.GetTaskNameByExtension(extension);
        }
        public static string GetTaskTitleByFileNameExtension(string extension)
        {
            IPreviewImageGenerator provider;
            if (!Providers.TryGetValue(extension.ToLowerInvariant(), out provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));
            return provider.GetTaskTitleByExtension(extension);
        }

        public static string[] GetSupportedCustomTaskNames()
        {
            // collect all suppotred task names from the different generator implementations
            return Providers.Values.Select(pig => pig.GetSupportedTaskNames())
                .Where(tnames => tnames != null)
                .SelectMany(tnames => tnames)
                .Where(tn => !string.IsNullOrEmpty(tn))
                .Distinct().OrderBy(tn => tn).ToArray();
        }

        public static bool IsSupportedExtension(string extension)
        {
            return Providers.ContainsKey(extension.ToLowerInvariant());
        }

        public static Task GeneratePreviewAsync(string extension, Stream docStream, IPreviewGenerationContext context, 
            CancellationToken cancellationToken)
        {
            if (!Providers.TryGetValue(extension.ToLowerInvariant(), out var provider))
                throw new ApplicationException(SR.F(SR.UnknownProvider_1, extension));

            return provider.GeneratePreviewAsync(docStream, context, cancellationToken);
        }
    }
}
