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
So I don't have to log in every time.

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
So I don't lose work.

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

## Epic 10: UX & Usability Improvements

---

### Story 10.1 — Streamlined Playlist Creation Workflow

**As a user**
I want a streamlined, step-by-step workflow
So I can create a playlist without excessive scrolling or clicking.

**Acceptance Criteria**

* Playlist creation follows a clear, linear workflow with visual indicators for current step
* Each step is focused and shows only what's necessary for that step
* Previous steps are collapsed to reduce visual clutter
* Users can navigate between steps without losing their data
* Responsive layout ensures no excessive scrolling on desktop or mobile
* Progress indicator shows: Select Source → Configure → Fetch & Match → Curate → Create
* Step transitions are smooth with brief loading states

---

### Story 10.2 — Automatic Fetch and Match by Default

**As a user**
I want data fetching and Spotify matching to happen automatically
So I can see matched results without manual triggering.

**Acceptance Criteria**

* After selecting filters, fetching and matching occurs automatically without requiring separate clicks
* Progress indicators show when fetching and matching are in progress
* Results appear in real-time as matching completes
* User can see which tracks matched and which failed to match
* A toggle option allows advanced users to "fetch before match" if they want to review raw data first
* The default behavior reduces required interactions from 2+ clicks to 0

---

### Story 10.3 — Smart Defaults and Preset Discovery

**As a user**
I want sensible defaults and discoverable presets
So I can create a playlist with minimal configuration.

**Acceptance Criteria**

* Presets are prominently displayed during source selection
* Each preset shows a brief description of what it does (e.g., "Top 50 tracks from last month")
* Selecting a preset auto-populates all filters with sensible values
* Presets automatically trigger fetch + match without additional clicks
* Default preset for each source is pre-selected
* User can customize preset values before execution
* At least 3-4 presets per data source are available (e.g., for Last.fm: "This Week", "This Year", "All Time Favorites", "Custom")

---

### Story 10.4 — Progressive Disclosure of Advanced Options

**As a user**
I want simple options visible by default and advanced options hidden
So I don't get overwhelmed by form controls.

**Acceptance Criteria**

* Basic configuration (source selection, preset choice) is always visible
* Advanced filters (custom date ranges, max results) are hidden behind an "Advanced" or "More Options" toggle
* Showing advanced options doesn't cause page reflow or excessive scrolling
* When advanced options are visible, basic options remain accessible
* Advanced options toggle state persists across source changes
* Users who don't need advanced options never see them

---

### Story 10.5 — Compact Form Layout

**As a user**
I want forms to be compact and scannable
So I can quickly see and modify my configuration.

**Acceptance Criteria**

* Configuration forms use compact spacing and minimal whitespace
* Related form fields are grouped together logically
* No more than 3-4 visible fields at once on the main configuration view
* Form labels are clear and concise (3-5 words max)
* Helper text is shown on hover or in collapsed state, not taking up space
* Buttons are appropriately sized and positioned for mobile and desktop
* Configuration section takes up no more than 25% of viewport height on initial load

---

### Story 10.6 — Results Preview and Validation

**As a user**
I want to see a preview of my results before creating the playlist
So I can verify the results match my expectations.

**Acceptance Criteria**

* Results display shows: total tracks found, tracks matched to Spotify, unmatched tracks
* A summary card shows key stats (e.g., "50 of 52 tracks matched successfully")
* Users can scroll through matched tracks to preview before creation
* Match quality indicators (confidence scores, match method) are visible
* Unmatched tracks are clearly flagged and separated from matched tracks
* A preview can be expanded inline without additional page navigation
* Performance: results load and display within 2 seconds

---

### Story 10.7 — Mobile-First Responsive Design

**As a user**
I want the playlist creation workflow to work seamlessly on mobile
So I can create playlists on any device.

**Acceptance Criteria**

* Layout adapts to mobile screens (320px+) without horizontal scrolling
* Touch targets are at least 48x48 pixels for easy tapping
* Form inputs are optimized for mobile (date pickers use native HTML5 inputs, dropdowns are accessible)
* Step indicator and workflow are easy to follow on small screens
* Results list is scrollable independently of the rest of the page
* Curate and reorder functionality works with touch (drag-and-drop or swipe gestures)
* Performance on mobile is optimized (lazy loading, minimal animations)

---

### Story 10.8 — Clear User Feedback and Error Handling

**As a user**
I want clear feedback on what's happening and what went wrong
So I can understand the state and recover from errors.

**Acceptance Criteria**

* Loading states show a spinner with estimated time or percentage completion
* Success states show a brief confirmation message (e.g., "50 tracks matched!")
* Errors show a specific message with a suggested action (e.g., "Last.fm account not found. Check username and try again.")
* Warnings are shown inline (e.g., "No matches found for 5 tracks" with option to view them)
* Error messages are helpful and actionable, not technical jargon
* Users can retry failed operations with a single click
* Toast notifications for non-blocking updates don't block interaction

---
