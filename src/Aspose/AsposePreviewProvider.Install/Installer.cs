using SenseNet.ContentRepository;

namespace SenseNet.Preview.Aspose.Install
{
    public static class Installer
    {
        private const string InstallPackageName = "install-preview-aspose.zip";

        /// <summary>
        /// Installs the sensenet Preview Aspose component.
        /// </summary>
        /// <param name="installer">Installer instance that contains the <see cref="RepositoryBuilder"/>.</param>
        public static Packaging.Installer InstallPreviewAspose(this Packaging.Installer installer)
        {
            installer.InstallPackage(typeof(Installer).Assembly, InstallPackageName);

            return installer;
        }
    }
}
