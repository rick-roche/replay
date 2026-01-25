Below is a **product + system feature specification** you can hand directly to **AI agents (or human devs)** to incrementally design and build **Re:Play**.
It’s written to be **implementation-agnostic**, but precise enough to drive architecture, API design, UI flows, and task breakdowns.

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

1. User opens Re:Play
2. User logs in with Spotify (OAuth)
3. User selects a **data source** (Last.fm / Discogs / Setlist.fm)
4. User configures their external profile (username, collection, etc.)
5. User applies **filters or presets**
6. Re:Play fetches data from the selected source
7. Re:Play attempts to **match tracks to Spotify**
8. User reviews and curates matched tracks
9. User creates a playlist on Spotify
10. Playlist is saved to their Spotify account

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

* User’s personal collection

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

Presets are **predefined filter configurations** that reduce cognitive load.

### Example Presets

| Preset Name           | Source     | Configuration                  |
| --------------------- | ---------- | ------------------------------ |
| Top 100 Tracks (2020) | Last.fm    | tracks + custom date range     |
| Vinyl Favourites      | Discogs    | vinyl only, random order       |
| Gig Memories          | Setlist.fm | last 10 concerts               |
| Year in Review        | Last.fm    | top tracks from last 12 months |

Presets should be:

* Editable after selection
* Discoverable in UI
* Easy to add via config (not hard-coded)

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

## 9. Non-Functional Requirements

### Performance

* Pagination for large datasets
* Background jobs for matching
* Rate-limit handling per API

### Security

* OAuth token encryption
* Read-only access to external APIs
* No storage of listening history beyond session unless explicitly saved

### Reliability

* Partial failures should not break the flow
* Graceful degradation when APIs are unavailable

---

## 10. Observability & Debugging

### Required Telemetry

* API request success/failure
* Match success rate
* Playlist creation success

### Admin / Debug Mode (Optional)

* View raw source data
* Inspect matching decisions
* Replay failed jobs

---

## 11. Future Enhancements (Out of Scope for MVP)

* Multiple destination platforms (Apple Music, local M3U)
* Mixing data sources in one playlist
* Scheduled playlist regeneration
* Saved searches & auto-refresh
* Social sharing of presets
* Collaborative playlists

---

## 12. AI-Agent-Friendly Task Decomposition

This document supports decomposition into:

* Auth agent (Spotify OAuth)
* Source ingestion agents (Last.fm / Discogs / Setlist.fm)
* Matching agent (Spotify search + scoring)
* UI agent (filters, presets, curation)
* Playlist publishing agent
* Preset configuration agent

Each section can be independently implemented and validated.
