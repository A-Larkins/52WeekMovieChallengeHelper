# 52 Week Movie Challenge Helper

Looks up a movie by title and prints the fields the [52 Week Movie
Challenge](https://www.reddit.com/r/52weeksofmovies/) logbook wants filled in:
rating, runtime, genre, cast, director, producer, writer, composer, and Oscar
count. Started as a console app; there's now an Avalonia desktop UI on top of
the same lookup code.

## What it does

Given a title, it:

1. Queries the **OMDb API** for title, year, rated, runtime, genre, actors,
   director, writer, and awards. Awards text gets reduced to an Oscar count
   (`"X Oscars"` if the OMDb `Awards` string mentions one, else `"No
   Oscars"`).
2. Queries the **TMDB API** for that title's producer(s) and composer(s),
   pulled from the first search result's credits (`crew` entries with job
   `Producer` / `Original Music Composer`).
3. Builds a Google search URL (`"<title> oscars won and aspect ratio"`) and an
   IMDb quotes-page URL (`https://www.imdb.com/title/<imdbID>/quotes/`) — OMDb
   doesn't return aspect ratio or quotes, so those two fields are left as
   links to look up by hand instead of scraped.

If TMDB fails or returns nothing, the OMDb result is still shown with
producer/composer as `N/A` — TMDB is a bonus lookup, not a dependency for the
core result.

## Structure

Two projects sharing one `.sln`:

| Project | What it is |
|---|---|
| `52WeekMovieChallengeHelper` | Class library + console entry point (`ConsoleApp.cs`). Contains `OmdbClient`, `TmdbClient`, `Movie`, and `AppConfiguration`. |
| `52WeekMovieChallengeHelper.Ui` | Avalonia desktop app (`net10.0`). References the console project directly and reuses `OmdbClient`/`TmdbClient` as-is. |

The console app prompts for a title, reprompts on a miss, prints the result to
stdout, and opens the Google search and IMDb quotes URLs in the default
browser (`open <url>`, so this is macOS-specific as written).

The UI is a single window: a title field, a search button, a results card,
and buttons to open the same two URLs. It also remembers window position/size
between launches in
`~/Library/Application Support/52WeekMovieChallengeHelper/window.json`.

## APIs and configuration

Both API clients read their key from `AppConfiguration`, which loads
`appsettings.json` (via `Microsoft.Extensions.Configuration`) and throws if a
key is missing rather than silently sending an empty one:

```json
{
  "TMDB": { "ApiKey": "..." },
  "OMDB": { "ApiKey": "..." }
}
```

- **OMDb** — get a free key at <https://www.omdbapi.com/apikey.aspx>, used as
  `OMDB:ApiKey`.
- **TMDB** — get a v4 read access token at
  <https://www.themoviedb.org/settings/api>, used as `TMDB:ApiKey` (sent as a
  Bearer token, not the older v3 query-param key).

`appsettings.json` is not checked in. `AppConfiguration` looks for it in two
places, first match wins:

1. Next to the running executable (`AppContext.BaseDirectory`) — covers
   `dotnet run`, a published build, and the `.app` bundle.
2. `/usr/local/bin/appsettings.json` — where `publish.sh` installs it
   alongside the console binary.

The `.Ui` project's `.csproj` also links `appsettings.json` from the console
project's folder into its own output, so the GUI build carries its own copy
and doesn't depend on the console app having been installed system-wide.

## Running it

### Console app

```bash
cd 52WeekMovieChallengeHelper
dotnet run
```

Or build and install it as a standalone `52wmovie` command:

```bash
./52WeekMovieChallengeHelper/publish.sh
```

This does a self-contained `osx-arm64` publish and copies the binary and
`appsettings.json` to `/usr/local/bin` (uses `sudo`).

### Desktop UI

```bash
dotnet run --project 52WeekMovieChallengeHelper.Ui
```

Or open `52WeekMovieChallengeHelper.sln` in an IDE and run the
`52WeekMovieChallengeHelper.Ui` project.

Either way, `appsettings.json` needs to exist in
`52WeekMovieChallengeHelper/` first — both projects' build steps copy it from
there into their own output directories.
