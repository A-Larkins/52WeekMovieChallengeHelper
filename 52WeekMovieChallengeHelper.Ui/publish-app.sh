#!/bin/bash
# Builds the GUI as a macOS .app bundle and installs it into /Applications.
set -e

APP_NAME="52 Week Movie Challenge Helper"
APP_DIR="/Applications/${APP_NAME}.app"
PROJECT_DIR=~/code/C#/52WeekMovieChallengeHelper/52WeekMovieChallengeHelper.Ui
PUBLISH_DIR="${PROJECT_DIR}/bin/Release/net10.0/osx-arm64/publish"

cd "${PROJECT_DIR}"
# Not PublishSingleFile: macOS cannot dlopen libSkiaSharp from inside a bundled
# exe, so the native dylibs need to sit on disk next to the executable.
rm -rf "${PUBLISH_DIR}"
dotnet publish -c Release -r osx-arm64 --self-contained true

rm -rf "${APP_DIR}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${APP_DIR}/Contents/Resources"

cat > "${APP_DIR}/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>52 Week Movie Challenge Helper</string>
  <key>CFBundleDisplayName</key>
  <string>52 Week Movie Challenge Helper</string>
  <key>CFBundleIdentifier</key>
  <string>com.alarkins.52weekmoviechallengehelper</string>
  <key>CFBundleVersion</key>
  <string>1.0</string>
  <key>CFBundleShortVersionString</key>
  <string>1.0</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleExecutable</key>
  <string>52WeekMovieChallengeHelper.Ui</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
</dict>
</plist>
PLIST

cp -R "${PUBLISH_DIR}/" "${APP_DIR}/Contents/MacOS/"
chmod +x "${APP_DIR}/Contents/MacOS/52WeekMovieChallengeHelper.Ui"

if [ -f "${PROJECT_DIR}/AppIcon.icns" ]; then
  cp "${PROJECT_DIR}/AppIcon.icns" "${APP_DIR}/Contents/Resources/"
fi

# Ad-hoc sign so Gatekeeper lets a locally built arm64 bundle run.
codesign --force --deep --sign - "${APP_DIR}"

echo "Installed ${APP_DIR}"
