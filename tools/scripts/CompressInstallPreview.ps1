$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installFolderPreview = "$srcPath\Preview\nuget\content\Admin\tools"
$installPackagePathPreview = "$installFolderPreview\install-preview.zip"

New-Item $installFolderPreview -Force -ItemType Directory

# Create the install package.
Compress-Archive -Path "$srcPath\Preview\nuget\snadmin\install-preview\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePathPreview

# Copy the install package to the project directory
Move-Item $installPackagePathPreview -Destination "$srcPath\Preview\Preview.Install" -Force
