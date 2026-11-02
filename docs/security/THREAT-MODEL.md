# Music Journal Threat Model

## Purpose

This threat model identifies the main security risks in the Music Journal application and documents the controls used to reduce those risks. The main areas considered are user authentication, access to user-specific data, state-changing requests, external API communication, database access, and credential management.

## Main Attack Surfaces

### 1. User Authentication
Music Journal uses ASP.NET Core Identity for user registration, login, and account management. An attacker could attempt to access features that are intended only for authenticated users.

### 2. User-Specific Data
Favorite tracks and recommendations are associated with individual user accounts. The application must prevent users from accessing or modifying another user's favorites.

### 3. State-Changing Requests
Actions such as adding or deleting favorite tracks modify application data. These requests could be targeted by Cross-Site Request Forgery (CSRF) attacks if anti-forgery protection is not enforced.

### 4. External Services
Music Journal communicates with the Spotify API and the application's recommendation service. Data received from external services should not automatically be considered trusted.

### 5. Database
The application stores user accounts, favorite tracks, and track information using Entity Framework Core and SQLite. Database operations must prevent unauthorized access to another user's data.

### 6. Application Credentials
Spotify API credentials are required by the application. Credentials must not be stored in source code or committed to the public GitHub repository.

## STRIDE Analysis, with Risk Rating

| Component | Threat | Category | Likelihood | Impact | Risk Rating | Mitigation |
|---|---|---|---|---|---|---|
| FavoriteTracksController | An unauthenticated user accesses favorite-track functionality intended for logged-in users | Spoofing / Elevation of Privilege | High | Medium | High | Controller-level `[Authorize]` requires authentication before accessing favorite-track actions |
| Favorite track data | A user attempts to access or modify another user's favorite tracks | Elevation of Privilege | Medium | High | High | Queries are filtered using the authenticated user's ID, and `UserId` is assigned server-side |
| State-changing POST requests | An attacker causes an authenticated user's browser to submit an unwanted request | Tampering | Medium | High | High | POST actions use anti-forgery validation; protection was added to `AddFromTrending` and `GenerateFromSelected` |
| Spotify credentials | API credentials are exposed through source control | Information Disclosure | Medium | High | High | Previously exposed credentials were rotated, and current Spotify credentials are stored using .NET User Secrets rather than tracked configuration files |
| Database queries | Malicious input attempts to alter database queries | Tampering | Low | High | Medium | Entity Framework Core LINQ queries are used instead of constructing raw SQL from user input |
| Error handling | Detailed application errors reveal implementation information | Information Disclosure | Low | Medium | Low | Production uses `UseExceptionHandler("/Home/Error")`; detailed development errors are not intended for production |
| External services | Unexpected data from Spotify or recommendation services causes incorrect application behavior | Tampering | Low | Medium | Low | External results are checked in application workflows before use; remaining risk is accepted for the current project scope |

## Security Controls Implemented for Milestone 2

### 1. Authentication and Access Control

During security testing, the `FavoriteTracksController` allowed some favorite-track pages to be accessed without authentication. For example, an unauthenticated user could access the Add Favorite page. Attempting to search from this page also caused a null-reference exception because no authenticated user existed.

Controller-level `[Authorize]` protection was added to `FavoriteTracksController`. This requires authentication before any action in the controller can be accessed.

**Before:** An unauthenticated user could access `/FavoriteTracks/Create` and attempting a search caused an application error.

**After:** An unauthenticated request to `/FavoriteTracks/Create` is redirected to the login page.

### 2. CSRF Protection

A review of state-changing POST actions found that some actions did not explicitly validate anti-forgery tokens.

`[ValidateAntiForgeryToken]` was added to:
- `FavoriteTracksController.AddFromTrending`
- `RecommendationsController.GenerateFromSelected`

The Generate From Selected form was also updated to use ASP.NET Core form tag helpers so an anti-forgery token is included with the request. The Trending page was already sending an anti-forgery token with its AJAX request.

Both workflows were manually tested after the changes to verify that adding a trending track to favorites and generating recommendations from selected favorites still function correctly.

### 3. Vulnerable Dependency Remediation

A dependency vulnerability scan identified `SQLitePCLRaw.lib.e_sqlite3` version 2.1.10 as a High-severity vulnerable transitive dependency included through `Microsoft.EntityFrameworkCore.Sqlite` 9.0.10.

`Microsoft.EntityFrameworkCore.Sqlite` was updated from version 9.0.10 to 9.0.22. After the update, the dependency vulnerability scan was run again and reported no vulnerable packages.

**Before:** `SQLitePCLRaw.lib.e_sqlite3` version 2.1.10 was reported as a High-severity vulnerable package.

**After:** The vulnerability scan reported no vulnerable packages.