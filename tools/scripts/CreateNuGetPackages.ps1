$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))

# delete existing packages
# Remove-Item $PSScriptRoot\*.nupkg

# All build functionality has been moved to csproj files and pre-build events.
