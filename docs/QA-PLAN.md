# Quality Assurance (QA) Plan & Checklist

## 1. Scope & Purpose
This QA plan outlines the validation of the Music Journal application for Milestone 2. Its purpose is to verify that Milestone 1 baseline features (Authentication, Favorites, Recommendations, Trending) remain unbroken, newly introduced Milestone 2 features function as intended, automated .NET test suites pass cleanly, and the UI displays properly across standard device viewports.

---

## 2. QA Checklist & Verification

Before any release tag, the reviewer must run through the following checklist:

### A. Environment & Setup
- [ ] Fresh setup succeeds via `./scripts/setup.sh` or `dotnet restore`.
- [ ] Local SQLite database builds and migrates cleanly.
- [ ] Spotify User Secrets are recognized without exceptions.
- [ ] Application starts without runtime errors via `dotnet run --project src/MusicJournal/MusicJournal.csproj`.

### B. Milestone 1 Regression Checks (Core Flows)
- [ ] **Authentication:** Register a new user, log in, log out, and verify that invalid credentials are rejected.
- [ ] **Favorites:** Add a track to Favorites, confirm it appears in the list, confirm duplicate additions are prevented, and delete a favorite track.
- [ ] **Recommendations:** Generate recommendations from favorited tracks and verify recommended tracks can be added to Favorites.
- [ ] **Trending Songs:** Load trending songs by selecting a year from the dropdown and add a trending song to Favorites.

### C. Milestone 2 New Feature Verification (Filter / Search)
- [ ] The new feature (search/filter entries or favorites) displays on the page.
- [ ] Typing a query filters the displayed tracks dynamically or on submission (by artist, title, or genre).
- [ ] Clearing the query restores the complete list.
- [ ] Searching for a non-matching query displays an empty state message (e.g., "No entries found").

### D. Automated Tests & Code Quality
- [ ] Automated test runner executes with zero failures: `dotnet test`.
- [ ] Code coverage runner generates a report: `dotnet test /p:CollectCoverage=true`.
- [ ] Line and branch coverage meets the project target (≥75% on core business/service logic).
- [ ] No API keys, Spotify Client Secrets, or production credentials are committed to Git.

### E. Security & Defensive Controls
- [ ] At least one designated security control is active (e.g., Anti-Forgery token validation on POSTs, input sanitization against XSS, or unauthorized route redirection).
- [ ] Prohibited input (e.g., `<script>alert(1)</script>`) is escaped, blocked, or sanitized.

### F. Responsive & Cross-Viewport Testing
- [ ] **Desktop Viewport (1280px+):** Navigation, track lists, and action buttons render without misaligned tables or broken grid wrapping.
- [ ] **Mobile Viewport (375px–414px):** 
  - Navbar collapses to mobile menu toggle.
  - Tables maintain accessibility via horizontal scrolling or responsive rows.
  - Buttons and forms remain accessible without horizontal page overflow.

---

## 3. Defect Severity & Triage Policy

| Severity | Definition | Action Required |
| :--- | :--- | :--- |
| **P0 - Blocker** | Application fails to start, database migration fails, or core authentication/favorites crash. | Immediate fix; blocks release. |
| **P1 - Major** | A primary feature (e.g., recommendations, trending lookup, or new filter) fails on valid input. | Must be fixed before Milestone submission. |
| **P2 - Minor** | Minor layout misalignment, styling glitch, or non-critical formatting bug. | Documented and fixed if time permits. |