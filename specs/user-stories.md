Below is a **structured set of user stories with acceptance criteria** derived directly from the Re:Play feature specification.
This is written to be **AI-agent executable**: each story is independently buildable, testable, and composable.

---

# Re:Play — User Stories & Acceptance Criteria

## Epic 1: User Authentication & Account Setup

---

### Story 1.1 — Login with Spotify

**As a user**
I want to log in using my **Spotify** account
So that Re:Play can create playlists on my behalf.

**Acceptance Criteria**

* Apps landing page prompts for login with Spotify if not already authenticated (or an invalid refresh token)
* User can initiate Spotify OAuth login
* On success, user is redirected back to Re:Play
* Spotify access token and refresh token are stored securely
* Required scopes include playlist creation and modification
* If login fails, user sees a clear error message
* If login succeeds, the user should see that they are logged in and the logged in users details

---

### Story 1.2 — Persist User Session

**As a user**
I want my login session to persist
So I don’t have to log in every time.

**Acceptance Criteria**

* Authenticated user remains logged in across page refreshes
* Expired tokens are refreshed automatically
* User is logged out if refresh fails
* Logout action clears session data

---

## Epic 2: External Data Source Configuration

---

### Story 2.1 — Configure Last.fm Profile

**As a user**
I want to link my **Last.fm** profile
So Re:Play can fetch my listening history.

**Acceptance Criteria**

* User can enter a Last.fm username
* Username is validated via API
* Validation errors are surfaced clearly
* Configuration is saved per user

---

### Story 2.2 — Configure Discogs Profile 

**As a user**
I want to link my **Discogs** collection
So Re:Play can access my owned albums.

**Acceptance Criteria**

* User can enter Discogs username or collection ID 
* Collection is validated and accessible 
* Read-only access is enforced 
* Configuration persists between sessions 

---

### Story 2.3 — Configure Setlist.fm Profile

**As a user**
I want to link my **Setlist.fm** account
So Re:Play can retrieve concerts I attended.

**Acceptance Criteria**

* User can enter Setlist.fm username or ID
* User profile is validated
* Errors are displayed clearly
* Configuration is stored per user

---

## Epic 3: Data Source Selection & Filtering

---

### Story 3.1 — Select Data Source

**As a user**
I want to choose a data source
So I can decide where my playlist data comes from.

**Acceptance Criteria**

* User can select exactly one data source
* UI updates dynamically based on source
* Previously selected source can be changed
* Changing source resets incompatible filters

---

### Story 3.2 — Filter Last.fm Data

**As a user**
I want to filter my Last.fm data
So I can generate playlists from specific listening periods.

**Acceptance Criteria**

* User can select tracks, albums, or artists
* User can choose predefined time ranges
* User can specify a custom date range
* User can set a maximum track count
* Invalid date ranges are rejected

---

### Story 3.3 — Filter Discogs Collection

**As a user**
I want to filter my Discogs collection
So playlists reflect specific parts of my collection.

**Acceptance Criteria**

* User can filter by release year range
* User can filter by media format
* User can filter by year added to collection
* User can set a maximum track count
* Filters combine correctly

---

### Story 3.4 — Filter Setlist.fm Concerts

**As a user**
I want to filter concerts I attended
So playlists reflect specific periods or shows.

**Acceptance Criteria**

* User can select a date range
* User can set maximum number of concerts
* User can set maximum track count
* Duplicate tracks are deduplicated

---

## Epic 4: Presets

---

### Story 4.1 — Use Presets

**As a user**
I want to choose from presets
So I can create playlists quickly.

**Acceptance Criteria**

* Presets are visible and selectable
* Selecting a preset auto-populates filters
* User can edit preset values before running
* Presets work across all data sources

---

### Story 4.2 — Preset Transparency

**As a user**
I want to understand what a preset does
So I can trust the results.

**Acceptance Criteria**

* Preset shows underlying filter configuration
* User can modify preset configuration
* Preset changes update results immediately

---

## Epic 5: Data Source Selection & Filtering

---

### Story 5.1 — Fetch Source Data

**As a user**
I want Re:Play to fetch data from my selected source
So I can preview what will become my playlist.

**Acceptance Criteria**

* Data is fetched using configured filters
* Progress indicator is shown
* Large datasets are paginated
* API errors are handled gracefully

---

### Story 5.2 — Normalise Source Data

**As a system**
I want to normalise incoming data
So it can be matched consistently.

**Acceptance Criteria**

* Artist, track, and album names are cleaned
* Case, punctuation, and diacritics are normalised
* Original metadata is preserved

---

## Epic 6: Spotify Matching Engine

---

### Story 6.1 — Match Tracks to Spotify

**As a user**
I want tracks matched to Spotify
So they can be added to a playlist.

**Acceptance Criteria**

* Matching attempts exact match first
* Fallback to fuzzy matching
* Album context is used where available
* Each match has a confidence score

---

### Story 6.2 — Handle Unmatched Tracks

**As a user**
I want to see unmatched tracks
So I can decide what to do with them.

**Acceptance Criteria**

* Unmatched tracks are clearly flagged
* User can retry matching
* User can manually search Spotify
* User can remove unmatched tracks

---

## Epic 7: Playlist Curation

---

### Story 7.1 — Review Matched Tracks

**As a user**
I want to review matched tracks
So I can ensure playlist quality.

**Acceptance Criteria**

* Track list is visible with metadata
* Match confidence is shown
* Source info is displayed (play count, concert date, etc.)

---

### Story 7.2 — Reorder & Remove Tracks

**As a user**
I want to reorder or remove tracks
So the playlist is exactly how I want it.

**Acceptance Criteria**

* Drag-and-drop reordering works
* Tracks can be removed
* Order is preserved for playlist creation

---

### Story 7.3 — Fetch More Tracks

**As a user**
I want to fetch more data from the same source
So I can expand the playlist.

**Acceptance Criteria**

* Additional data is fetched without losing state
* New tracks are matched and appended
* Duplicates are avoided

---

## Epic 8: Playlist Creation on Spotify

---

### Story 8.1 — Configure Playlist Details

**As a user**
I want to configure playlist details
So it looks right in Spotify.

**Acceptance Criteria**

* User can edit playlist name
* User can edit description
* User can choose public or private
* Defaults are auto-generated

---

### Story 8.2 — Create Playlist

**As a user**
I want to create the playlist on Spotify
So I can listen immediately.

**Acceptance Criteria**

* Playlist is created successfully
* Tracks are added in chosen order
* User receives confirmation
* Deep link to Spotify playlist is shown

---

## Epic 9: Reliability & Observability

---

### Story 9.1 — Handle Partial Failures

**As a user**
I want partial failures handled gracefully
So I don’t lose work.

**Acceptance Criteria**

* Failed matches do not block playlist creation
* API failures show actionable errors
* User can retry failed steps

---

### Story 9.2 — Capture Telemetry

**As a system**
I want to record operational metrics
So reliability can be monitored.

**Acceptance Criteria**

* API request success/failure is logged
* Match success rate is tracked
* Playlist creation success is tracked

---
