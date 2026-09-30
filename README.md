# ♫ 🎸 Music Journal 🎸 ♫
Music Journal is a web application that allows users to keep a personal journal of music they listen to. The application is being developed as an ASP.NET Core MVC application.

## Technology Stack
- .NET 9
- ASP.NET Core MVC
- ASP.NET Core Identity
- Entity Framework Core
- SQLite

## Current Features
- User registration, login, and logout 
- Secure, persistent user accounts using ASP.NET Core Identity and SQLite
- Email verification is not currently required
- Browse Spotify trending songs by year 
- Search and save songs to personal music journal
- Generate song recommendations based on favorited songs

## Requirements
Music Journal requires the .NET 9 SDK.

### Install .NET 9
Download and install the .NET 9 SDK from Microsoft's official .NET download page:
https://dotnet.microsoft.com/download/dotnet/9.0

After installation, verify it with:
```bash
dotnet --version
```

The version should begin with `9.`.

## Setup
Clone the repository: 
```bash
git clone https://github.com/jaydengrisaffe/Music-Journal.git
```

Enter the project directory:
```bash
cd Music-Journal
```

Run the setup script:
```bash
./scripts/setup.sh
```

Spotify API credentials are required for Spotify-related features. When prompted during setup, users should provide their own Spotify Client ID and Client Secret.

 An example Spotify configuration with placeholder values is provided in src/MusicJournal/appsettings.Example.json. Actual credentials are configured during setup and stored locally using .NET User Secrets.

For Milestone 1 grading, please refer to `Milestone 1 Verification Guide` section and the submitted **Milestone 1 Report**.

The setup script will:

- Verify that .NET 9 is installed.
- Restore the required .NET tools.
- Restore project dependencies.
- Prompt for the Spotify Client ID and Client Secret.
- Store the Spotify credentials locally using .NET User Secrets.
- Create or update the local SQLite database.


## Run

Start the application with:
```bash
dotnet run --project src/MusicJournal/MusicJournal.csproj
```

Open the localhost address displayed in the terminal.

## Milestone 1 Verification Guide

Follow the setup instructions provided above.

After running `./scripts/setup.sh`, you will be prompted for a Spotify Client ID and Client Secret. The Spotify credentials required for Milestone 1 testing are provided privately in the submitted **Milestone 1 Report**.

After opening the application, verify the following:

### 1. Authentication
- Register a new account.
- Log in and log out successfully.
- Reject invalid credentials.

### 2. Favorite Tracks
- Navigate to Favorites.
- Search for a song or artist.
- Add a song to Favorites.
- Verify that the song appears in the Favorites list.
- Attempt to add the same song again and verify that a duplicate is not created.
- Delete a song from Favorites.

### 3. Music Recommendations
- Add at least one song to Favorites.
- Navigate to Recommendations.
- Generate music recommendations.
- Generate recommendations using selected favorite tracks.
- Verify that recommended tracks are displayed and can be added to Favorites.

### 4. Trending Songs
- Navigate to Trending.
- Select a year from the year dropdown.
- Verify that tracks for the selected year are displayed.
- Add a track from the Trending page to Favorites.

### 5. Responsive Interface
- Resize the browser window or use a mobile viewport.
- Verify that the navigation collapses on smaller screens.
- Verify that tables remain accessible through horizontal scrolling.

## Definition of Done
A backlog item is Done when:
- [ ] The feature has been fully implemented.
- [ ] The feature works as described in the backlog item.
- [ ] The feature has been manually tested in the application.
- [ ] The feature works with the existing application without breaking other core features.
- [ ] Any necessary documentation has been updated.
- [ ] The completed work has been committed to the project repository.

## Process
See docs/BACKLOG.md for the current product backlog.