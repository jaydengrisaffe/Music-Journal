#!/bin/bash

set -e

echo "Setting up Music Journal..."

# Make sure .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET 9 SDK is required."
    exit 1
fi

# Make sure .NET 9 is being used
DOTNET_VERSION=$(dotnet --version)

if [[ "$DOTNET_VERSION" != 9.* ]]; then
    echo "ERROR: .NET 9 SDK is required. Found version $DOTNET_VERSION."
    exit 1
fi

echo ".NET $DOTNET_VERSION found."

# Restore local .NET tools
echo "Restoring .NET tools..."
dotnet tool restore

# Restore project dependencies
echo "Restoring project dependencies..."
dotnet restore

# Create/update the SQLite database
echo "Setting up database..."
dotnet tool run dotnet-ef database update \
    --project src/MusicJournal/MusicJournal.csproj \
    --startup-project src/MusicJournal/MusicJournal.csproj

echo ""
echo "Music Journal setup complete."
echo "Run the application with:"
echo "dotnet run --project src/MusicJournal/MusicJournal.csproj"