using SenseNet.ContentRepository;
using SenseNet.Packaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Preview.Aspose
{
    public static class InstallerExtensions
    {
        private const string InstallPackageName = "install-preview-aspose.zip";

        /// <summary>
        /// Installs the sensenet Preview Aspose component.
        /// </summary>
        /// <param name="installer">Installer instance that contains the <see cref="RepositoryBuilder"/>.</param>
        public static Installer InstallPreviewAspose(this Installer installer)
        {
            installer.InstallPackage(typeof(InstallerExtensions).Assembly, InstallPackageName);

            return installer;
        }
    }
}
