# BigBang1112.cz

[![CI](https://img.shields.io/github/actions/workflow/status/BigBang1112/bigbang1112cz/ci.yml?branch=main&style=for-the-badge&logo=github&label=CI)](https://github.com/BigBang1112/bigbang1112cz/actions/workflows/ci.yml)
[![GitHub last commit (branch)](https://img.shields.io/github/last-commit/bigbang1112/bigbang1112cz/main?style=for-the-badge&logo=github)](#)
[![Website](https://img.shields.io/website?url=https%3A%2F%2Fbigbang1112.cz&style=for-the-badge&label=bigbang1112.cz)](https://bigbang1112.cz)

Source code of my personal website - [bigbang1112.cz](https://bigbang1112.cz).

It is a small ASP.NET Core Razor Pages app on .NET 10, but it is **also the home of my Trackmania manialinks**, including the TMF horn collection, which is the reason why there is a database behind a personal website.

For any questions, open an issue or message me via Discord DM: `bigbang1112`

## What's inside

- Home, Projects, Links, Music, Blog, and Contact pages - server-rendered, with no JavaScript of its own
- **Music and blog written in Markdown**, in [`Content/Music`](BigBang1112cz/Content/Music) and [`Content/Blog`](BigBang1112cz/Content/Blog), with an RSS feed at `/blog/feed.xml`
- Trackmania manialinks under `/trackmania/manialink/`:
  - **The horny collection** for TMF - 50+ horns to preview, download, and comment on
  - ManiaPlanet landing page, minigames (Breakout, Pipes, Paint), and the Universe Map Plugin Set installer
  - Maniacode that installs any skin from a URL

## Writing music and blog posts

Copy `_template.md` in [`Content/Music`](BigBang1112cz/Content/Music/_template.md) or [`Content/Blog`](BigBang1112cz/Content/Blog/_template.md) to `yyyy-MM-dd-some-title.md` - the template explains every field.

Running locally, saving the file updates the page right away. Blog posts with a future `published` time (UTC, to the minute) stay hidden on the live site until then. Mistakes fail loudly with the file name, and the tests check every file, so a broken one doesn't make it through CI.

## Running locally

Requires .NET 10 SDK, Git, and Docker (or your own MariaDB).

```
docker compose up --build
```

This starts the website (see its port with `docker compose ps`), MariaDB, phpMyAdmin on [localhost:9010](http://localhost:9010), and Seq on [localhost:8081](http://localhost:8081). In Visual Studio, just pick the **Docker Compose** launch profile. The compose override mounts folders from `%APPDATA%`, so it works on Windows as it is.

Without Docker, have MariaDB on `localhost:3306` with a passwordless `root`, then `dotnet run --project BigBang1112cz` serves the website on [localhost:5176](http://localhost:5176). Migrations are applied automatically in Development. New ones are created with `dotnet tool restore` and `dotnet ef migrations add Name --project BigBang1112cz`.

## Tests

```
dotnet test --solution BigBang1112cz.slnx
```

Tests use [TUnit](https://tunit.dev/) and an in-memory database, so no MariaDB is needed. The solution has to be named, because the root also contains `docker-compose.dcproj`.

## CI and deployment

Pushes and pull requests to `main` build and test on Windows and Linux using [BigBang1112/workflows](https://github.com/BigBang1112/workflows). The Docker image (`amd64` and `arm64`) is published to Docker Hub and the GitHub Container Registry by running **Docker Image Publish** from the GitHub Actions tab.
