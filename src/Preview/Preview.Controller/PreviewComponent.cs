using System;
using SenseNet.ContentRepository;

namespace SenseNet.Preview.Controller
{
    internal class PreviewComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.Preview";
        public override Version SupportedVersion { get; } = new Version(7, 1, 1, 1);
    }
}
