#!/bin/bash
#
# Installs the console app as a standalone `52wmovie` command in /usr/local/bin.
# Needs sudo for that folder; the GUI has its own script (../52WeekMovieChallengeHelper.Ui/publish-app.sh)
# and needs no privileges at all.
#
set -euo pipefail

# From this script's own location, not a hardcoded home path — the old version
# pointed at ~/code/C#/…, which stopped existing when the folder became csharp/.
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$PROJECT_DIR/52WeekMovieChallengeHelper.csproj"
BIN_DIR="/usr/local/bin"

case "$(uname -m)" in
  arm64) RID="osx-arm64" ;;
  *)     RID="osx-x64" ;;
esac

# Read from the .csproj rather than written down here: the pair drifted apart
# once already, and a path built from the wrong one copies nothing and says
# nothing about it.
TFM="$(sed -n 's:.*<TargetFramework>\(.*\)</TargetFramework>.*:\1:p' "$CSPROJ" | head -1)"
PUBLISH_DIR="$PROJECT_DIR/bin/Release/$TFM/$RID/publish"

if [ ! -f "$PROJECT_DIR/appsettings.json" ]; then
  echo "✗ Missing $PROJECT_DIR/appsettings.json — see the README for the keys it needs." >&2
  exit 1
fi

echo "▶ Publishing self-contained ($RID, $TFM)…"
dotnet publish "$CSPROJ" -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true --nologo -v quiet

if [ ! -f "$PUBLISH_DIR/52WeekMovieChallengeHelper" ]; then
  echo "✗ No binary at $PUBLISH_DIR — nothing installed." >&2
  exit 1
fi

echo "▶ Installing to $BIN_DIR (sudo)…"
sudo cp "$PUBLISH_DIR/52WeekMovieChallengeHelper" "$BIN_DIR/52wmovie"
sudo cp "$PROJECT_DIR/appsettings.json" "$BIN_DIR/appsettings.json"
sudo chmod +x "$BIN_DIR/52wmovie"

echo "✓ Installed: $BIN_DIR/52wmovie  (run it as: 52wmovie)"
