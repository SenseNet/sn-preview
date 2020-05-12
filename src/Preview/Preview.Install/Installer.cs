using SenseNet.ContentRepository;

namespace SenseNet.Preview.Install
{
    public static class Installer
    {
        private const string InstallPackageName = "install-preview.zip";

        /// <summary>
        /// Installs the sensenet Preview component.
        /// </summary>
        /// <param name="installer">Installer instance that contains the <see cref="RepositoryBuilder"/>.</param>
        public static SenseNet.Packaging.Installer InstallPreview(this SenseNet.Packaging.Installer installer)
        {
            installer.InstallPackage(typeof(Installer).Assembly, InstallPackageName);

            return installer;
        }
    }
}
