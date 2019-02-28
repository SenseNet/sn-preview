$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installFolderPreview = "$srcPath\Preview\nuget\content\Admin\tools"
$installFolderAspose = "$srcPath\Aspose\nuget\content\Admin\tools"
$installPackagePathPreview = "$installFolderPreview\install-preview.zip"
$installPackagePathAspose = "$installFolderAspose\install-preview-aspose.zip"

# delete existing packages
Remove-Item $PSScriptRoot\*.nupkg

Compress-Archive -Path "$srcPath\Preview\nuget\snadmin\install-preview\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePathPreview

dotnet pack $srcPath\Preview\Preview.Controller\Preview.Controller.csproj -c Release -o $PSScriptRoot

# check if Aspose components are present
if (Test-Path "$srcPath\Aspose\AsposePreviewProvider\bin\Release\SenseNet.Preview.AsposePreviewProvider.dll")
{
	New-Item $installFolderAspose -Force -ItemType Directory
	Compress-Archive -Path "$srcPath\Aspose\nuget\snadmin\install-aspose\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePathAspose
	nuget pack $srcPath\Aspose\AsposePreviewProvider\AsposePreviewProvider.nuspec -properties Configuration=Release -OutputDirectory $PSScriptRoot
}
else
{
	Write-Host "Aspose preview provider not found, Aspose package is not built."
}
