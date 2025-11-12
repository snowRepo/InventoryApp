# PowerShell script to build for Windows

# Set variables
$ProjectName = "InventoryApp"
$Version = "1.0.0"
$TargetFramework = "net8.0"
$Runtime = "win-x64"
$OutputDir = "publish-win"
$ZipName = "${ProjectName}-${Version}-Windows.zip"

# Clean previous builds
Write-Host "Cleaning previous builds..."
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
if (Test-Path $ZipName) {
    Remove-Item -Path $ZipName -Force
}

# Publish the application
Write-Host "Publishing the application for Windows..."

# Ensure the Assets directory exists for the icon
if (-not (Test-Path -Path "Assets")) {
    New-Item -ItemType Directory -Path "Assets" -Force | Out-Null
}

# Publish with explicit icon and single file settings
dotnet publish -c Release -r $Runtime \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:ApplicationIcon="Assets/avalonia-logo.ico" \
    -p:WindowsAppSDKSelfContained=true \
    -o $OutputDir

# Create ZIP file
Write-Host "Creating ZIP archive..."
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipName -Force

Write-Host "Build complete! ZIP file created: $ZipName"
