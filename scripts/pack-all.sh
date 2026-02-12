#!/bin/bash
set -euo pipefail

ARTIFACTS_DIR="${1:-./artifacts}"
CONFIGURATION="${2:-Release}"

mkdir -p "$ARTIFACTS_DIR"

echo "Building solution..."
dotnet build --configuration "$CONFIGURATION"

echo ""
echo "Packing all source projects..."
for csproj in src/*/*.csproj; do
    echo "  Packing: $csproj"
    dotnet pack "$csproj" \
        --configuration "$CONFIGURATION" \
        --output "$ARTIFACTS_DIR" \
        --no-build
done

echo ""
echo "Packages created in $ARTIFACTS_DIR:"
echo ""
echo "NuGet packages (.nupkg):"
ls -la "$ARTIFACTS_DIR"/*.nupkg 2>/dev/null || echo "  (none)"
echo ""
echo "Symbol packages (.snupkg):"
ls -la "$ARTIFACTS_DIR"/*.snupkg 2>/dev/null || echo "  (none)"
