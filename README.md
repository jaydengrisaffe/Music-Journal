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
- Browse Spotify trending songs by year
- Search and save songs to personal Favorites
- Remove songs from Favorites
- Generate song recommendations based on favorited songs
- Generate recommendations using selected favorite tracks
- Add recommended and trending tracks directly to Favorites

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

Spotify API credentials are required for Spotify-related features. When prompted during setup, users should provide their own Spotify Client ID and Client Secret. For Milestone 2 grading, working Spotify credentials are provided privately in the submitted **Milestone 2 Report**.

 An example Spotify configuration with placeholder values is provided in src/MusicJournal/appsettings.Example.json. Actual credentials are configured during setup and stored locally using .NET User Secrets.

The setup script will:

- Verify that .NET 9 is installed.
- Restore the required .NET tools.
- Restore project dependencies.
- Prompt for the Spotify Client ID and Client Secret.
- Store the Spotify credentials locally using .NET User Secrets.
- Apply the Entity Framework Core migrations to create or update the local SQLite database.

No seed data or preconfigured test account is required. A test account can be created using the application's registration page.

## Run

Start the application with:
```bash
dotnet run --project src/MusicJournal/MusicJournal.csproj
```

Open the localhost address displayed in the terminal.

## Milestone 2 Verification Guide

Follow the setup instructions above before beginning verification.

After running `./scripts/setup.sh`, you will be prompted for a Spotify Client ID and Client Secret. The Spotify credentials required for Milestone 2 testing are provided privately in the submitted **Milestone 2 Report**.

### 1. Verify the Application Starts
Run:
```bash
dotnet run --project src/MusicJournal/MusicJournal.csproj
```
Open the localhost address displayed in the terminal and verify that the application loads successfully.

### 2. Verify the Milestone 1 Workflow

#### 1. Authentication
- Register a new account.
- Log in and log out successfully.
- Verify that invalid credentials are rejected.

#### 2. Favorite Tracks
- Navigate to Favorites.
- Search for a song or artist.
- Add a song to Favorites.
- Verify that the song appears in the Favorites list.
- Attempt to add the same song again and verify that a duplicate is not created.
- Remove a song from Favorites.

#### 3. Music Recommendations
- Add at least one song to Favorites.
- Navigate to Recommendations.
- Generate music recommendations.
- Generate recommendations using selected favorite tracks.
- Verify that recommended tracks are displayed and can be added to Favorites.

#### 4. Trending Songs
- Navigate to Trending.
- Select a year from the year dropdown.
- Verify that tracks for the selected year are displayed.
- Add a track from the Trending page to Favorites.

#### 5. Responsive Interface
- Resize the browser window or use a mobile viewport.
- Verify that the navigation collapses on smaller screens.
- Verify that tables remain accessible through horizontal scrolling.

### 3. Verify Milestone 2 UI Improvements
The Milestone 1 workflow above also verifies the Milestone 2 UI improvements, including updated button styling, action labels, navigation, and visual feedback across Favorites, Recommendations, and Trending.

### 4. Run Automated Tests and Coverage
From the repository root, run:
```bash
./run-coverage.sh
```
Verify that the automated tests complete successfully.  
The script generates a coverage summary at: `coverage-report/Summary.txt`  
The HTML coverage report is available at: `coverage-report/index.html`

### 5. Verify Security Controls
#### Access Control
1. Log out of the application.
2. Attempt to navigate directly to Favorite Tracks functionality.
3. Verify that the application redirects to the login page instead of allowing unauthenticated access.
4. Log back in and verify that Favorite Tracks functionality works normally.

#### Dependency Vulnerability Check
From the repository root, run:
```bash
dotnet list src/MusicJournal/MusicJournal.csproj package --vulnerable --include-transitive
```
Verify that the scan reports no vulnerable packages from the configured package sources.

Additional security documentation is available at:
- `docs/security/OWASP-TOP-10-REVIEW.md`
- `docs/security/THREAT-MODEL.md`

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

## Testing & Code Coverage

The unit test suite exercises core domain entities, data annotations, and database controller workflows using xUnit, Moq, and an in-memory SQLite provider. Code coverage metrics are tracked with Coverlet and compiled into human-readable text and interactive HTML reports via ReportGenerator.

### Environment Setup

Before running tests, ensure that the .NET global tools directory is added to your environment path with `export PATH="$PATH:$HOME/.dotnet/tools"`. If ReportGenerator is not yet installed on your machine, install it globally by running `dotnet tool install -g dotnet-reportgenerator-globaltool`.

### Running Tests and Generating Coverage

A script located at `./run-coverage.sh` handles testing and report compilation in a single execution. Running this script from the repository root executes `dotnet test` with Coverlet instrumentation, filters compiler and framework noise, and prints a formatted terminal summary from `coverage-report/Summary.txt`. For visual inspection, the script simultaneously builds an interactive HTML report that can be opened in any web browser via `open coverage-report/index.html`.

### Coverage Strategy and Component Scope

Our testing strategy achieves a justified coverage target of 100% across all functional business logic, domain models, view models, database context operations, and MVC controller actions. Specifically, complete 100% coverage was attained for all primary controllers (`FavoriteTracksController`, `GlobalTracksController`, `HomeController`, and `RecommendationsController`), all domain models and view models (`FavoriteTrack`, `GlobalTrack`, `RecTrack`, `ApplicationUser`, `ErrorViewModel`, `ProfileViewModel`, `CreateFavoriteViewModel`, and `TrackResult`), and the underlying `ApplicationDbContext`.

### Exclusions & Architectural Justification

Framework-generated database schema migrations (`MusicJournal.Migrations.*`) and Razor-compiled presentation templates (`AspNetCoreGeneratedDocument.*` and `MusicJournal.Views.*`) were intentionally excluded from coverage thresholds because they represent auto-generated markup and historical schema snapshots rather than testable business logic. Direct external network calls in `SpotifyService` and `AIService` were bypassed during controller execution in favor of deterministic custom `HttpMessageHandler` stubs and interface mocks, isolating controller execution paths while reserving live network verification for dedicated integration environments.