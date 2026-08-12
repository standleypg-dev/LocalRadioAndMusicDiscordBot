# Discord Music Bot

A .NET 10 Discord bot for music playback, supporting YouTube, SoundCloud, Spotify (playlist metadata), and radio streams. Supports multiple servers concurrently - each server gets its own queue, player, and voice connection. Includes a React-based web dashboard for statistics and radio-source management.

**Note:** This application is designed to run on Linux via Docker. A Dockerfile and docker-compose setup are provided.

## Installation

1. Create a Discord bot and add it to your server. Follow the [Discord Developer docs](https://discord.com/developers/docs/intro).
2. Set up the necessary bot permissions. See [Permissions](https://discord.com/developers/docs/topics/permissions).
3. Obtain a Discord bot token. See [OAuth2 Bots](https://discord.com/developers/docs/topics/oauth2#bots).
4. Clone this repository.
5. Configure `deployment/.env` with your values (Discord token, Spotify credentials, database, JWT, etc.).
6. Build and run with Docker:
   ```
   docker-compose -f deployment/docker-compose.yml up --build
   ```

## Development

```bash
# Build the whole solution (requires Bun for the frontend build)
dotnet build discord-project.slnx

# Run the Worker (Discord bot + web API on :5000)
dotnet run --project src/Worker/Worker.csproj

# Run all tests (integration tests need a local Docker daemon for Testcontainers)
dotnet test src/Tests/Tests.csproj

# Unit tests only (no Docker required)
dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~Tests.Unit"
```

Tests run automatically in CI (`.github/workflows/ci.yml`) on pushes and pull requests to `master`; pushes to `master` are deployed via `.github/workflows/release.yml`.

### Frontend

Run from `src/UI/App/` (Bun is the package manager):

```bash
bun run dev           # Vite dev server
bun run build         # Type-check + production build (output: src/Worker/wwwroot/)
bun run lint          # ESLint
bun run format        # Prettier write
```

### Dev Container

`.devcontainer/` provides a full-stack VS Code Dev Container (separate from `deployment/`, not used in production) with the SDK, Bun, and all native audio dependencies (libsodium, libopus, libdave, ffmpeg, yt-dlp) preinstalled, plus its own Postgres service.

1. `cp .devcontainer/.env.example .devcontainer/.env` and fill in a dev/test Discord bot token (do not reuse your production token).
2. Reopen the folder in the container (VS Code: "Dev Containers: Reopen in Container").
3. Inside the container, `dotnet build discord-project.slnx`, `dotnet run --project src/Worker/Worker.csproj`, and `dotnet test src/Tests/Tests.csproj` (including Testcontainers integration tests, via the bind-mounted Docker socket) all work out of the box.

Pinned to `linux/amd64` — on Apple Silicon/arm64 hosts, Docker Desktop must emulate via Rosetta/QEMU or the native library paths resolve to nothing at runtime.

> If you ever change `POSTGRES_USER`/`PASSWORD`/`DB` in `.devcontainer/.env` or `deployment/.env` after Postgres has already started once, the existing volume keeps the old credentials — Postgres only applies those env vars on first init of an empty data directory. Fix with `docker-compose down -v` on the relevant stack to force a clean re-init.

## Architecture

One bot process serves many servers. Commands address a guild through `IGuildMusicService`; `GuildPlayerManager` lazily creates one `GuildPlayer` per guild, each with its own queue, consumer loop, voice client, and FFmpeg process, so playback in one server never affects another. Statistics and the blacklist are shared across all servers.

```mermaid
flowchart TD
    UA["User in Server A"] -->|"/play /skip /stop"| CMD
    UB["User in Server B"] -->|"/play /skip /stop"| CMD

    subgraph Worker["Worker process"]
        CMD["Slash commands + NetCordInteraction"]
        CMD -->|"Enqueue(guildId, PlayRequest)"| MGR
        CMD -->|"EventType.Skip / Stop (GuildId)"| PH["EventDispatcher -> PlayerHandler"]
        PH -->|"Skip(guildId) / Stop(guildId)"| MGR
        MGR["GuildPlayerManager<br/>(IGuildMusicService)<br/>one GuildPlayer per guild"]

        subgraph PA["GuildPlayer - Server A"]
            QA["MusicQueueService<br/>(list + channel signal)"]
            LA["Consumer loop<br/>(retry x3, per-track cancellation)"]
            AA["AudioPlayerService<br/>(voice client A)"]
            FA["FfmpegProcessService<br/>(ffmpeg process A)"]
            QA -->|"signal wakes"| LA
            LA -->|"PlayTrackAsync"| AA
            AA -->|"spawn"| FA
        end

        subgraph PB["GuildPlayer - Server B"]
            QB["MusicQueueService"]
            LB["Consumer loop"]
            AB["AudioPlayerService<br/>(voice client B)"]
            FB["FfmpegProcessService"]
            QB -->|"signal wakes"| LB
            LB -->|"PlayTrackAsync"| AB
            AB -->|"spawn"| FB
        end

        MGR -->|"guild A ops"| QA
        MGR -->|"guild B ops"| QB

        SS["Stream resolvers<br/>yt-dlp / YoutubeExplode /<br/>SoundCloud / radio URL"]
        DB[("PostgreSQL<br/>stats + blacklist<br/>shared by all servers")]
        AA -.->|"resolve stream URL"| SS
        AB -.-> SS
        AA -.->|"log play"| DB
        AB -.-> DB
    end

    FA -->|"PCM to Opus stream"| VA(("Voice channel<br/>Server A"))
    FB -->|"PCM to Opus stream"| VB(("Voice channel<br/>Server B"))
```

## Technologies Used

### Backend
- [.NET 10](https://dotnet.microsoft.com) / ASP.NET Core
- [NetCord](https://github.com/NetCordDev/NetCord) - Discord bot framework
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + [PostgreSQL](https://www.postgresql.org)
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) / [YoutubeDLSharp](https://github.com/Bluegrams/YoutubeDLSharp) + [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [SoundCloudExplode](https://github.com/jerry08/SoundCloudExplode)
- [Spotify Web API](https://developer.spotify.com/documentation/web-api) - playlist metadata
- [FFmpeg](https://ffmpeg.org) - audio processing
- [libopus](https://opus-codec.org) / [libsodium](https://doc.libsodium.org) / [libdave](https://github.com/discord/libdave) - voice encoding and encryption
- [xUnit](https://xunit.net) + [NSubstitute](https://nsubstitute.github.io) + [Testcontainers](https://dotnet.testcontainers.org) - testing

### Frontend
- [React 19](https://react.dev) with TypeScript
- [TanStack Router](https://tanstack.com/router) / [TanStack Query](https://tanstack.com/query)
- [Recharts](https://recharts.org)
- [Vite](https://vite.dev) + [Bun](https://bun.sh)
