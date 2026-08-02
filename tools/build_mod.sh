#!/usr/bin/env bash
# Build DefinitiveMultiplayer.tmod and inject icon_small.png
# (tML packaging leaves the stream at EOF before loading icon_small.png, which
# triggers a harmless FNA "unknown image type" warning — we skip that path.)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TML="${TML_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/tModLoader}"
MODS="${TML_MODS:-$HOME/Library/Application Support/Terraria/tModLoader/Mods}"
TMOD="$MODS/DefinitiveMultiplayer.tmod"
DLL="$ROOT/bin/Release/net8.0/DefinitiveMultiplayer.dll"

export DYLD_LIBRARY_PATH="$TML/Libraries/Native/OSX${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"

# Compile DLL (packaging step may fail under plain msbuild without native libs).
dotnet build "$ROOT/DefinitiveMultiplayer.csproj" -c Release -v q || true

cd "$TML"
"$TML/dotnet/dotnet" tModLoader.dll -server -build "$ROOT/" -eac "$DLL"

python3 "$ROOT/tools/inject_tmod_files.py" "$TMOD" \
	--remove-glob-suffix .bak \
	--add "icon_small.png=$ROOT/icon_small.png"

echo "Built: $TMOD"
