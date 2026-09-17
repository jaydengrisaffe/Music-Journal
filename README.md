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

The setup script will:

- Verify that .NET 9 is installed
- Restore the required .NET tools
- Restore project dependencies
- Create or update the local SQLite database

## Run

Start the application with:
```bash
dotnet run --project src/MusicJournal/MusicJournal.csproj
```

Open the localhost address displayed in the terminal.

## Definition of Done
A backlog item is Done when:
- [ ] The implementation is complete.
- [ ] The code has been reviewed. 
- [ ] Relevant tests have been added or updated. 
- [ ] Automated CI checks pass. 
- [ ] The feature works in the deployed/development environment. 
- [ ] Documentation is updated when necessary. 
- [ ] The associated GitHub issue is closed. 

## Process
See docs/BACKLOG.md for the current product backlog.