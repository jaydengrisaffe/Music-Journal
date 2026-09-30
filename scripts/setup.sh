#!/bin/bash

set -e

echo "Setting up Music Journal..."

PROJECT="src/MusicJournal/MusicJournal.csproj"

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

# Configure Spotify API credentials
echo ""
echo "Spotify API credentials are required."
read -p "Spotify Client ID: " SPOTIFY_CLIENT_ID
read -s -p "Spotify Client Secret: " SPOTIFY_CLIENT_SECRET
echo ""

dotnet user-secrets set "Spotify:ClientId" "$SPOTIFY_CLIENT_ID" --project "$PROJECT"
dotnet user-secrets set "Spotify:ClientSecret" "$SPOTIFY_CLIENT_SECRET" --project "$PROJECT"

echo "Spotify credentials configured."

# Create/update the SQLite database
echo "Setting up database..."
dotnet tool run dotnet-ef database update \
    --project "$PROJECT" \
    --startup-project "$PROJECT"

echo ""
echo "Music Journal setup complete."
echo "Run the application with:"
echo "dotnet run --project $PROJECT"