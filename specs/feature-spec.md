Below is a **product + system feature specification** you can hand directly to **AI agents (or human devs)** to incrementally design and build **Re:Play**.
It's written to be **implementation-agnostic**, but precise enough to drive architecture, API design, UI flows, and task breakdowns.

---

# Re:Play — Feature & Capability Specification

## 1. Product Overview

**Re:Play** is a web application that lets users:

1. Aggregate listening and collection data from external music services
2. Convert that data into matched Spotify tracks
3. Curate and generate Spotify playlists based on historical behaviour, ownership, or live concerts

Supported **data sources**:

* **Last.fm** (listening history)
* **Discogs** (physical/digital collection)
* **Setlist.fm** (concerts attended)

Supported **playlist destination**:

* **Spotify**

---

## 2. Core User Journey (Happy Path)

### Current Optimized Path (Preferred)

1. User opens Re:Play
2. User logs in with Spotify (OAuth)
3. User selects a **data source** and **preset** (Last.fm / Discogs / Setlist.fm)
4. User optionally configures external profile (username, collection, etc.)
5. Re:Play **automatically fetches and matches** data from the selected source
6. User reviews matched results (with summary stats)
7. User optionally curates tracks (reorder, remove, add more)
8. User creates a playlist on Spotify
9. Playlist is saved to their Spotify account

### Advanced Path (For Power Users)

1-3. Same as above
4. User configures external profile
5. User toggles "Advanced: Fetch before match" to review raw data first
6. User applies **filters** to refine data
7. User manually triggers match when satisfied with raw data
8-9. Same as above

---

## 3. Authentication & Accounts

### 3.1 Spotify Authentication (Required)

* OAuth login
* Required scopes:

  * `playlist-modify-private`
  * `playlist-modify-public`
  * `user-read-email` (optional)
* Store:

  * Spotify user ID
  * Access token (short-lived)
  * Refresh token

### 3.2 External Source Configuration (Per User)

Each source is **optional** and configured independently.

| Source     | Required Config          |
| ---------- | ------------------------ |
| Last.fm    | Username                 |
| Discogs    | Username / Collection ID |
| Setlist.fm | Username or User ID      |

Tokens or API keys should be stored securely and scoped to read-only access.

---

## 4. Data Sources & Filters

### 4.1 Last.fm

**Supported Data Types**

* Tracks
* Albums
* Artists

**Filters**

* Time period:

  * `last7days`
  * `last1month`
  * `last3months`
  * `last6months`
  * `last12months`
  * `overall`
  * `custom` (start date + end date)
* Max number of tracks in playlist

**Example Queries**

* Top 50 tracks from last 3 months
* Top albums listened to in 2022
* Most played artists overall

---

### 4.2 Discogs

**Source**

* User's personal collection

**Filters**

* Release year (range)
* Media format:

  * Vinyl
  * CD
  * Cassette
  * Digital
* Year added to collection
* Max number of tracks in playlist

**Behaviour**

* Albums are expanded into tracklists
* Track order should respect album sequencing where possible

**Example Queries**

* Vinyl albums added in 2024
* Albums released between 1990–1999
* Random 100 tracks from entire collection

---

### 4.3 Setlist.fm

**Source**

* Concerts attended by the user

**Filters**

* Time period (date range)
* Max number of concerts
* Max number of tracks in playlist

**Behaviour**

* Extract:

  * Artists played
  * Tracks played
* Deduplicate repeated songs across concerts

**Example Queries**

* All concerts attended in 2023
* Last 5 concerts attended
* Festival shows only (future enhancement)

---

## 5. Presets (UX Simplification Layer)

Presets are **predefined filter configurations** that reduce cognitive load and accelerate workflow.

### Preset Characteristics

* **Discoverable**: Prominently displayed during source selection
* **Quick-start**: Selecting a preset auto-populates filters AND automatically triggers fetch + match
* **Informative**: Each preset shows a clear description (e.g., "Top 50 tracks from last month")
* **Customizable**: User can edit preset values before execution
* **Persistent**: At least 3-4 presets available per data source

### Example Presets

| Preset Name           | Source     | Configuration                     | Auto-Trigger |
| --------------------- | ---------- | --------------------------------- | ------------- |
| This Week             | Last.fm    | top tracks from last 7 days       | Yes           |
| This Year             | Last.fm    | top tracks from last 12 months    | Yes           |
| All Time Favorites    | Last.fm    | top tracks overall                | Yes           |
| Custom                | Last.fm    | user-configured filters           | No (manual)   |
| Vinyl Collection      | Discogs    | vinyl only, all years             | Yes           |
| Recent Additions      | Discogs    | all formats, added this year      | Yes           |
| Concert Memories      | Setlist.fm | last 10 concerts attended        | Yes           |

Presets should be:

* Editable after selection
* Discoverable in UI (not buried in menus)
* Easy to add via config (not hard-coded)
* Trigger automatic fetch + match by default

---

## 6. Matching Engine (Core Intelligence)

### 6.1 Matching Inputs

* Artist name
* Track name
* Album name (when available)

### 6.2 Matching Strategy (Ordered)

1. Exact match (artist + track)
2. Normalised match (case, punctuation, diacritics)
3. Fuzzy match (string similarity threshold)
4. Album-aware matching (when album context exists)

### 6.3 Match Confidence

Each match should include:

* Confidence score (0–100)
* Match method (exact / fuzzy / album-based)

### 6.4 Unmatched Items

* Flagged clearly in UI
* User can:

  * Retry match
  * Manually search Spotify
  * Remove item

---

## 7. Playlist Curation UI

### Required Capabilities

* Reorder tracks (drag & drop)
* Remove tracks
* See source metadata (play count, album, concert date, etc.)
* Add more tracks from original query
* See unmatched items

### Results Preview & Validation

* Display summary stats: Total tracks found, matched count, unmatched count
* Show matches with confidence indicators
* Clearly flag unmatched tracks separately
* Performance target: Results load and display within 2 seconds

### Optional Enhancements

* Sort by:

  * Popularity
  * Date
  * Artist
* Preview track (Spotify embed)

---

## 8. Playlist Creation on Spotify

### Playlist Options

* Name (auto-generated, editable)
* Description (auto-generated, editable)
* Public / Private toggle

### Creation Flow

1. User confirms playlist
2. Tracks are created on Spotify in chosen order
3. Confirmation + deep link to Spotify playlist

---

## 9. UX & Usability Requirements (NEW)

### 9.1 Streamlined Workflow

* **Stepped Experience**: Workflow follows clear progression: Select Source → Configure → Fetch & Match → Curate → Create
* **Visual Progress**: Step indicator shows current position and completion
* **Collapsed History**: Previous steps are hidden to reduce visual clutter
* **State Preservation**: Users can navigate between steps without losing configuration

### 9.2 Automatic Fetch & Match (Default Behavior)

* After user selects a preset or applies filters, fetching and matching occur **automatically**
* No separate "Fetch" button required
* Real-time progress indicators show current operation
* Results stream in as matches complete
* **Advanced toggle** allows power users to "Fetch before match" if they want to review raw data first

### 9.3 Progressive Disclosure

* **Basic view by default**: Source selection, preset choice, minimal configuration
* **Advanced options hidden**: Custom date ranges, max results, fetch-before-match toggle
* **Clean layout**: Max 3-4 visible fields in configuration view
* **No page reflow**: Showing advanced options doesn't cause unexpected layout changes

### 9.4 Compact Forms

* **Minimal whitespace**: Tight spacing to reduce scrolling
* **Logical grouping**: Related fields grouped together
* **Clear labels**: 3-5 word labels without jargon
* **Inline help**: Helper text shown on hover or in collapsed state
* **Limit**: Configuration section takes ≤25% of viewport height

### 9.5 Mobile-First Responsive Design

* Adapts to screens 320px+ without horizontal scrolling
* Touch targets: Minimum 48x48 pixels
* Native form inputs: Date pickers use HTML5 inputs, dropdowns accessible
* Scrollable results: Independent scrolling for results vs. controls
* Touch gestures: Drag-and-drop or swipe support for track reordering
* Performance optimized: Lazy loading, minimal animations

### 9.6 Clear Feedback & Error Handling

* **Loading states**: Spinner with estimated time or percentage
* **Success feedback**: Brief confirmation messages (e.g., "50 tracks matched!")
* **Clear errors**: Specific message + suggested action (e.g., "Last.fm account not found. Check username.")
* **Warnings**: Inline display of non-blocking issues (e.g., "No matches found for 5 tracks")
* **Actionable**: No technical jargon; users can retry or dismiss
* **Non-blocking notifications**: Toast messages don't block interaction

---

## 10. Non-Functional Requirements

### Performance

* Pagination for large datasets
* Background jobs for matching
* Rate-limit handling per API
* Results display within 2 seconds

### Security

* OAuth token encryption
* Read-only access to external APIs
* No storage of listening history beyond session unless explicitly saved

### Reliability

* Partial failures should not break the flow
* Graceful degradation when APIs are unavailable
* Failed matches do not block playlist creation

---

## 11. Observability & Debugging

### Required Telemetry

* API request success/failure
* Match success rate
* Playlist creation success
* Workflow completion rate and drop-off points

### Admin / Debug Mode (Optional)

* View raw source data
* Inspect matching decisions
* Replay failed jobs

---

## 12. Future Enhancements (Out of Scope for MVP)

* Multiple destination platforms (Apple Music, local M3U)
* Mixing data sources in one playlist
* Scheduled playlist regeneration
* Saved searches & auto-refresh
* Social sharing of presets
* Collaborative playlists

---

## 13. AI-Agent-Friendly Task Decomposition

This document supports decomposition into:

* Auth agent (Spotify OAuth)
* Source ingestion agents (Last.fm / Discogs / Setlist.fm)
* Matching agent (Spotify search + scoring)
* UI/UX agent (workflow, forms, responsive design)
* Playlist publishing agent
* Preset configuration agent

Each section can be independently implemented and validated.
