# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Discord Music Bot - A .NET 10 application that plays music in Discord voice channels from YouTube, SoundCloud, Spotify (playlist metadata only), and radio streams. Supports multiple servers concurrently: each guild gets its own queue, player loop, voice connection, and FFmpeg process, while statistics and the blacklist are shared globally. Ships with a React dashboard for stats and radio-source management. Designed to run on Linux via Docker; native voice dependencies (libdave, libsodium, libopus) make local Windows runs impractical.

## Build & Run Commands

```bash
# Recommended: build and run everything (bot + Postgres) via Docker
docker-compose -f deployment/docker-compose.yml up --build

# Build the whole solution (uses .slnx format)
dotnet build discord-project.slnx

# Run the Worker (composition root, hosts Discord bot + web API on :5000)
dotnet run --project src/Worker/Worker.csproj

# Release configuration
dotnet run --project src/Worker/Worker.csproj -c Release

# Run tests (unit tests always; integration tests need a local Docker daemon for Testcontainers)
dotnet test src/Tests/Tests.csproj

# Unit tests only (no Docker required)
dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~Tests.Unit"
```

Frontend (run from `src/UI/App/`, Bun is the package manager):

```bash
bun run dev           # Vite dev server
bun run build         # Type-check + production build (output: src/Worker/wwwroot/)
bun run build:dev     # Same, but development mode
bun run lint          # ESLint
bun run format        # Prettier write
bun run format:check  # Prettier check
```

Tests live in `src/Tests/` (xUnit + NSubstitute; integration tests use Testcontainers PostgreSQL against the real migrations). The test project references Domain/Application/Infrastructure but deliberately NOT Worker (whose build shells out to Bun for the frontend).

## Architecture

### Clean Architecture Layers

- **Domain** (`src/Domain/`) - Entities (`Song`, `User`, `PlayHistory`, `RadioSource`), event handler interfaces (`IEventHandler<T>`, `IAsyncEventHandler<T>`), and enums.
- **Application** (`src/Application/`) - Business services (`SpotifyService`, `JokeService`, `QuoteService`, `HttpRequestService`), DTOs, config binding helpers, the in-process event dispatcher plus `AddEventing` registration (`Application.Eventing`), and the `GlobalStore` singleton.
- **Infrastructure** (`src/Infrastructure/`) - NetCord commands and interactions, audio pipeline (`GuildPlayerManager`, `GuildPlayer`, `AudioPlayerService`, `FfmpegProcessService`, `MusicQueueService`), `PlayerHandler` (thin guild-scoped Skip/Stop event adapter), YouTube/SoundCloud stream services, EF Core `DiscordBotContext` with compiled models, and radio/user/blacklist/statistics services.
- **Tests** (`src/Tests/`) - xUnit test project: `Unit/` (queue, guild player, guild player manager, eventing) and `Integration/` (blacklist/statistics/user services against Testcontainers PostgreSQL).
- **UI** (`src/UI/`)
  - `Api/` - ASP.NET Core minimal-API endpoints (see `ControllerExtensions.cs`) with JWT bearer auth. Anonymous stats endpoints (`/api/statistics-all`, `/api/statistics-today` for today's top songs, `/api/users`); radio-source CRUD and token validation require authorization. Login checks against `JwtSettings:InternalPassword` (no user password hashing).
  - `App/` - React 19 + TypeScript SPA, responsive down to phone widths (breakpoints at 1024/768/480px in `index.css`; `hide-sm`/`hide-md` classes drop table columns on small screens). The index route is an Overview landing page (`pages/Overview.tsx` + `components/RankList.tsx`) with headline stats, top-songs-today, all-time favorites, top listeners, and a latest-activity feed derived from each user's recent songs. The song/user dashboards support search, sortable columns, pagination (`Pagination`/`SortableTh` components, 10 rows per page), an all-time/today filter (songs), relative "last played" timestamps (`utils/time.ts`), and auto-refresh every 60s via TanStack Query `refetchInterval`. Charts adapt to mobile via the `useIsMobile` hook. The API base URL is hardcoded in `services/api.ts`: relative `/api` for builds (the Worker serves the SPA and API from the same origin, so this works for local docker and deployment alike) and `http://localhost:5000/api` only when `import.meta.env.DEV` is true (the Vite dev server). Do NOT reintroduce `VITE_API_BASE_URL` env files: bun auto-loads `.env*` into `process.env`, which outranks `.env.production` in Vite and once leaked the dev URL into a deployed production build. Note that `deployment/docker-compose.override.yml` (VS debug tooling) sets `BUILD_CONFIGURATION: Debug` and a `--wait-for-debugger` entrypoint; it is auto-applied if you run `docker compose` from `deployment/` without `-f`, so always use the explicit `-f deployment/docker-compose.yml` form for real runs. Build output writes into `src/Worker/wwwroot/`, which is served by the Worker as static files with SPA fallback to `index.html`.
- **Worker** (`src/Worker/`) - Composition root. Program.cs builds a `WebApplication` that hosts the NetCord Discord gateway (registered via `AddDiscordGateway`), the ASP.NET Core web API, and the `GuildPlayerManager` hosted service in one process on port 5000.

### Key Patterns

**Event system**: Custom in-process dispatcher (`IEventDispatcher`, `IAsyncEventDispatcher`) plus a `HandlerRegistry`. `Application.Eventing.EventingServiceCollectionExtensions.AddEventing(assemblies...)` scans supplied assemblies for `IEventHandler<T>` / `IAsyncEventHandler<T>` implementations and registers them as scoped. When adding a new event handler, ensure its assembly is passed to `AddEventing` in `DependencyInjection.cs` (currently `Application.AssemblyMarker` and `Infrastructure.Services.AssemblyMarker`). Only `EventType.Skip` / `EventType.Stop` are dispatched today; both carry a `GuildId` and are handled by `PlayerHandler`, which forwards to `IGuildMusicService`. `EventType.Play` is not dispatched anymore because enqueueing wakes the guild's channel consumer directly.

**Per-guild players**: `GuildPlayerManager` (a `BackgroundService`, registered as `IGuildMusicService`) owns one `GuildPlayer` per guild, created lazily on the first enqueue for that guild via a component factory (wired in `Worker/DependencyInjection.cs`). Each `GuildPlayer` owns its own `MusicQueueService` (a lock-protected list paired with an unbounded `System.Threading.Channels` signal channel, per the Microsoft queue-service guidance), its own `AudioPlayerService` (voice client) and `FfmpegProcessService` instance, and a consumer loop: it dequeues a `PlayRequest` into the `NowPlaying` slot, awaits `AudioPlayerService.PlayTrackAsync` (retrying failed tracks up to 3 times), and disconnects from that guild's voice channel when its queue runs empty. Skip/Stop cancel the guild's per-track linked `CancellationTokenSource`; commands address a guild through `IGuildMusicService` with `Context.Guild.Id`. None of `MusicQueueService`, `AudioPlayerService`, or `FfmpegProcessService` are DI-registered anymore - the manager constructs them per guild.

**Keyed services**: Multiple implementations of `IStreamService` and `IRandomService` are registered by name and resolved with `[FromKeyedServices(nameof(...))]`:

```csharp
services.AddKeyedScoped<IStreamService, YoutubeService>(nameof(YoutubeService));
services.AddKeyedScoped<IStreamService, SoundCloudService>(nameof(SoundCloudService));
services.AddKeyedScoped<IRandomService, JokeService>(nameof(JokeService));
services.AddKeyedScoped<IRandomService, QuoteService>(nameof(QuoteService));
```

**Discord commands** (`src/Infrastructure/Commands/`):

- `PlayCommand` (in `MusicPlayCommands.cs`) - `/play music` and radio subcommands
- `MusicActionCommands` - stop, skip, playlist, rewind, statistics
- `AdminCommands` - admin-only actions (blacklist management)
- `MiscCommands` - help, joke, motivate

Commands and the `NetCordInteraction` component module are wired in `Worker.DependencyInjection.AddWebApplication`.

**Scoped work from singletons**: `IScopeExecutor` (`ScopeExecutor`) is used by singleton services (like commands calling into scoped `DiscordBotContext`) to open a DI scope on demand.

### Database

PostgreSQL + EF Core 10. Context: `Infrastructure/Data/DiscordBotContext.cs`. Uses **compiled models** (`Infrastructure.CompiledModels.DiscordBotContextModel`) for startup performance; if you change the model, regenerate the compiled model in addition to adding a migration. Migrations live in `src/Infrastructure/Data/Migrations/` and are applied automatically at startup by `context.Database.MigrateAsync()` in `Program.cs`.

### Audio Pipeline

A play interaction enqueues a `PlayRequest` via `IGuildMusicService` into the guild's `MusicQueueService`; the channel signal wakes that guild's `GuildPlayer` loop, which calls `AudioPlayerService.PlayTrackAsync`. That method joins the voice channel if needed, resolves the stream URL (`YoutubeExplode` / `YoutubeDLSharp` via `yt-dlp` at runtime, or a radio source URL when the selection is a Guid), logs the play via `IStatisticsService` (radio plays are logged with the station name as the title), spawns FFmpeg through `FfmpegProcessService` (`Ffmpeg:Path` config, default `/usr/bin/ffmpeg`), copies FFmpeg stdout into a NetCord `OpusEncodeStream`, then awaits process exit and maps the exit code to a `TrackPlayResult` (`Completed`/`Failed`/`Skipped`/`NotInVoiceChannel`). There are no C# events in this pipeline; user-facing messages flow through `PlayRequest.Callbacks` and failures are retried by the guild's player loop.

### Native Dependencies (installed in the Docker image)

- FFmpeg
- libsodium, libopus
- libdave (fetched from the Discord libdave releases zip in the Dockerfile)
- yt-dlp (pulled from the yt-dlp `latest` release at image build time; `YT_DLP_CACHE_BUST` build arg forces a re-download)
- python3 (required by yt-dlp)

## Configuration

.NET config keys (colon separators become double-underscore in env vars, per `deployment/docker-compose.yml`):

- `Discord:Token`
- `SpotifySettings:ClientId` / `SpotifySettings:ClientSecret`
- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Secret` / `Issuer` / `Audience` / `InternalPassword`
- `WebsiteSettings:Url`
- `Cors:AllowedOrigins` (JSON array in env var form: `[origin1,origin2]`)
- `Ffmpeg:Path` (optional, defaults to `/usr/bin/ffmpeg`)

For local Docker runs, populate `deployment/.env` (see `.github/workflows/release.yml` for the exact key list).

## Toolchain

- .NET SDK: `global.json` pins 9.0.3 with `rollForward: latestMajor`, so SDK 10+ satisfies it. All projects target `net10.0` via `src/Directory.Build.props` (nullable + implicit usings enabled).
- Central package management: all NuGet versions live in `src/Directory.Packages.props`.
- Frontend: React 19, Vite 8, TanStack Router + Query, Recharts, Sonner. Uses `babel-plugin-react-compiler` via the Vite React plugin.

## Deployment

`.github/workflows/release.yml` deploys on push to `master` via a self-hosted Linux runner: tears down the running compose stack, writes `deployment/.env` from GitHub secrets, then rebuilds and restarts with `docker compose up -d`. The build passes `YT_DLP_CACHE_BUST=$(date +%Y%m%d)` so yt-dlp refreshes daily.

`.github/workflows/ci.yml` runs `dotnet test src/Tests/Tests.csproj` on GitHub-hosted Ubuntu for pushes and PRs (Docker is preinstalled there, so the Testcontainers integration tests run too). It intentionally does not build the full `.slnx` because the Worker frontend build requires Bun.
