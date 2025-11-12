#!/bin/bash

# Set variables
PROJECT_NAME="InventoryApp"
VERSION="1.0.0"
TARGET_FRAMEWORK="net8.0"
RUNTIME="osx-x64"
OUTPUT_DIR="publish"
DMG_NAME="${PROJECT_NAME}-${VERSION}-macOS.dmg"
TEMP_DMG="temp.dmg"
APP_NAME="${PROJECT_NAME}.app"

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf "$OUTPUT_DIR" "$TEMP_DMG" "$DMG_NAME" "$APP_NAME"

# Publish the application
echo "Publishing the application for macOS..."
dotnet publish -c Release -r $RUNTIME --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUTPUT_DIR"

# Create .app bundle
echo "Creating application bundle..."
mkdir -p "$APP_NAME/Contents/MacOS"
mkdir -p "$APP_NAME/Contents/Resources"

# Copy the published files to the .app bundle
cp -R "$OUTPUT_DIR/"* "$APP_NAME/Contents/MacOS/"

# Copy the app icon if it exists
if [ -f "AppIcon.icns" ]; then
    cp "AppIcon.icns" "$APP_NAME/Contents/Resources/"
elif [ -f "NewAppIcon.icns" ]; then
    cp "NewAppIcon.icns" "$APP_NAME/Contents/Resources/AppIcon.icns"
else
    echo "Warning: No app icon found. Using default icon."
fi

# Create Info.plist
cat > "$APP_NAME/Contents/Info.plist" <<EOL
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>InventoryApp</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.inventory.app</string>
    <key>CFBundleName</key>
    <string>${PROJECT_NAME}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOL

# Create a simple DMG using hdiutil with a different approach
echo "Creating DMG..."

# Create a temporary directory for the DMG contents
DMG_TEMP="$TMPDIR/${PROJECT_NAME}_dmg"
mkdir -p "$DMG_TEMP"

# Create a symbolic link to Applications
ln -s "/Applications" "$DMG_TEMP/Applications"

# Copy the app to the DMG
cp -R "$APP_NAME" "$DMG_TEMP/"

# Calculate the size needed for the DMG
SIZE=$(du -sh "$DMG_TEMP" | cut -f1 | tr -d ' ' | tr -d 'M')
SIZE=$((SIZE + 20)) # Add 20MB buffer

# Create the DMG
hdiutil create -volname "$PROJECT_NAME" -srcfolder "$DMG_TEMP" -ov -format UDZO -fs HFS+ "$DMG_NAME"

# Clean up
echo "Cleaning up..."
rm -rf "$OUTPUT_DIR" "$APP_NAME"

echo "Build complete! DMG created: $DMG_NAME"
