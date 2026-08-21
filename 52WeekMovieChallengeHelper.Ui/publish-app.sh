#!/bin/bash
#
# Builds "52 Week Movie Challenge Helper.app" for macOS.
#
#   ./publish-app.sh              build into dist/
#   ./publish-app.sh --install    …and install it into /Applications
#
# Building and installing are separate steps so the daily sweep can do the
# installing itself (see ~/code/.claude/install-app.sh): it has to quit a
# running copy first, or the app keeps running from the deleted bundle and the
# install silently does nothing.
#
# The bundle carries its own copy of the .NET runtime, so it runs without .NET
# installed — and its own appsettings.json, so it doesn't need the console app
# to have been installed system-wide.
#
set -euo pipefail

# Resolved from this script's own location. The previous version hardcoded
# ~/code/C#/…, which stopped existing when that folder was renamed to csharp/ —
# the build quietly stopped working and the installed copy sat at July's build.
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$PROJECT_DIR")"
CSPROJ="$PROJECT_DIR/52WeekMovieChallengeHelper.Ui.csproj"
SETTINGS="$REPO_DIR/52WeekMovieChallengeHelper/appsettings.json"

APP_NAME="52 Week Movie Challenge Helper"
DIST_DIR="$REPO_DIR/dist"
APP="$DIST_DIR/$APP_NAME.app"
EXE="52WeekMovieChallengeHelper.Ui"

INSTALL=0
for arg in "$@"; do
  case "$arg" in
    --install) INSTALL=1 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

case "$(uname -m)" in
  arm64) RID="osx-arm64" ;;
  *)     RID="osx-x64" ;;
esac

# Both API keys live here and the file is git-ignored, so a fresh clone builds an
# app that throws on its first lookup. Better to say so now than at runtime.
if [ ! -f "$SETTINGS" ]; then
  echo "✗ Missing $SETTINGS — see the README for the OMDb and TMDB keys it needs." >&2
  exit 1
fi

PUBLISH_DIR="$PROJECT_DIR/bin/Release/net10.0/$RID/publish"

echo "▶ Publishing self-contained ($RID)…"
# Deliberately not PublishSingleFile: macOS can't dlopen libSkiaSharp from inside
# a bundled executable, so Avalonia's native dylibs have to sit on disk beside it.
rm -rf "$PUBLISH_DIR"
dotnet publish "$CSPROJ" -c Release -r "$RID" --self-contained true --nologo -v quiet

echo "▶ Assembling $APP_NAME.app…"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH_DIR/" "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/$EXE"

if [ -f "$PROJECT_DIR/AppIcon.icns" ]; then
  cp "$PROJECT_DIR/AppIcon.icns" "$APP/Contents/Resources/AppIcon.icns"
fi

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>              <string>$APP_NAME</string>
  <key>CFBundleDisplayName</key>       <string>$APP_NAME</string>
  <key>CFBundleIdentifier</key>        <string>com.alarkins.52weekmoviechallengehelper</string>
  <key>CFBundleVersion</key>           <string>1.0</string>
  <key>CFBundleShortVersionString</key><string>1.0</string>
  <key>CFBundleExecutable</key>        <string>$EXE</string>
  <key>CFBundleIconFile</key>          <string>AppIcon</string>
  <key>CFBundlePackageType</key>       <string>APPL</string>
  <key>LSMinimumSystemVersion</key>    <string>11.0</string>
  <key>NSHighResolutionCapable</key>   <true/>
</dict>
</plist>
PLIST

# Ad-hoc signature, so Gatekeeper lets a locally built arm64 bundle run at all.
codesign --force --deep --sign - "$APP" 2>/dev/null

echo "✓ Built: $APP"

if [ "$INSTALL" -eq 1 ]; then
  DEST="/Applications/$APP_NAME.app"
  echo "▶ Installing to ${DEST}…"
  # Replaced rather than merged: leftovers from an older publish would otherwise
  # linger inside the bundle.
  rm -rf "$DEST"
  ditto "$APP" "$DEST"
  xattr -dr com.apple.quarantine "$DEST" 2>/dev/null || true
  echo "✓ Installed: $DEST"
else
  echo "  Install it with:  $0 --install"
fi
