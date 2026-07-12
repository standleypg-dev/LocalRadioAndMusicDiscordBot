# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Discord Music Bot - A .NET 10 application that plays music in Discord voice channels from YouTube, SoundCloud, Spotify (playlist metadata only), and radio streams. Ships with a React dashboard for stats and radio-source management. Designed to run on Linux via Docker; native voice dependencies (libdave, libsodium, libopus) make local Windows runs impractical.

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
- **Infrastructure** (`src/Infrastructure/`) - NetCord commands and interactions, audio pipeline (`MusicPlayerBackgroundService`, `AudioPlayerService`, `FfmpegProcessService`, `MusicQueueService`), `PlayerHandler` (thin Skip/Stop event adapter), YouTube/SoundCloud stream services, EF Core `DiscordBotContext` with compiled models, and radio/user/blacklist/statistics services.
- **Tests** (`src/Tests/`) - xUnit test project: `Unit/` (queue, player background service, eventing) and `Integration/` (blacklist/statistics/user services against Testcontainers PostgreSQL).
- **UI** (`src/UI/`)
  - `Api/` - ASP.NET Core minimal-API endpoints (see `ControllerExtensions.cs`) with JWT bearer auth. Login checks against `JwtSettings:InternalPassword` (no user password hashing).
  - `App/` - React 19 + TypeScript SPA. Build output writes into `src/Worker/wwwroot/`, which is served by the Worker as static files with SPA fallback to `index.html`.
- **Worker** (`src/Worker/`) - Composition root. Program.cs builds a `WebApplication` that hosts the NetCord Discord gateway (registered via `AddDiscordGateway`), the ASP.NET Core web API, and the `MusicPlayerBackgroundService` hosted service in one process on port 5000.

### Key Patterns

**Event system**: Custom in-process dispatcher (`IEventDispatcher`, `IAsyncEventDispatcher`) plus a `HandlerRegistry`. `Application.Eventing.EventingServiceCollectionExtensions.AddEventing(assemblies...)` scans supplied assemblies for `IEventHandler<T>` / `IAsyncEventHandler<T>` implementations and registers them as scoped. When adding a new event handler, ensure its assembly is passed to `AddEventing` in `DependencyInjection.cs` (currently `Application.AssemblyMarker` and `Infrastructure.Services.AssemblyMarker`). Only `EventType.Skip` / `EventType.Stop` are dispatched today (handled by `PlayerHandler`, which forwards to `IMusicPlayerController`); `EventType.Play` is not dispatched anymore because enqueueing wakes the player's channel consumer directly.

**Queue service pattern**: `MusicQueueService` (singleton) pairs a lock-protected list with an unbounded `System.Threading.Channels` signal channel, following the Microsoft queue-service guidance. `MusicPlayerBackgroundService` (a `BackgroundService`, also registered as `IMusicPlayerController`) is the single consumer: it dequeues a `PlayRequest` into the `NowPlaying` slot, awaits `AudioPlayerService.PlayTrackAsync` (retrying failed tracks up to 3 times), and disconnects from voice when the queue runs empty. Skip/Stop cancel the per-track linked `CancellationTokenSource`.

**Keyed services**: Multiple implementations of `IStreamService` and `IRandomService` are registered by name and resolved with `[FromKeyedServices(nameof(...))]`:

```csharp
services.AddKeyedScoped<IStreamService, YoutubeService>(nameof(YoutubeService));
services.AddKeyedScoped<IStreamService, SoundCloudService>(nameof(SoundCloudService));
services.AddKeyedScoped<IRandomService, JokeService>(nameof(JokeService));
services.AddKeyedScoped<IRandomService, QuoteService>(nameof(QuoteService));
```

**Discord commands** (`src/Infrastructure/Commands/`):
- `PlayCommand` (in `MusicPlayCommands.cs`) - `/play music` and radio subcommands
- `MusicActionCommands` - pause, skip, stop, volume
- `AdminCommands` - admin-only actions
- `MiscCommands` - help, joke, motivate

Commands and the `NetCordInteraction` component module are wired in `Worker.DependencyInjection.AddWebApplication`.

**Scoped work from singletons**: `IScopeExecutor` (`ScopeExecutor`) is used by singleton services (like commands calling into scoped `DiscordBotContext`) to open a DI scope on demand.

### Database

PostgreSQL + EF Core 10. Context: `Infrastructure/Data/DiscordBotContext.cs`. Uses **compiled models** (`Infrastructure.CompiledModels.DiscordBotContextModel`) for startup performance; if you change the model, regenerate the compiled model in addition to adding a migration. Migrations live in `src/Infrastructure/Data/Migrations/` and are applied automatically at startup by `context.Database.MigrateAsync()` in `Program.cs`.

### Audio Pipeline

A play interaction enqueues a `PlayRequest` into `MusicQueueService`; the channel signal wakes `MusicPlayerBackgroundService`, which calls `AudioPlayerService.PlayTrackAsync`. That method joins the voice channel if needed, resolves the stream URL (`YoutubeExplode` / `YoutubeDLSharp` via `yt-dlp` at runtime, or a radio source URL when the selection is a Guid), logs the play via `IStatisticsService` (radio plays are logged with the station name as the title), spawns FFmpeg through `FfmpegProcessService` (`Ffmpeg:Path` config, default `/usr/bin/ffmpeg`), copies FFmpeg stdout into a NetCord `OpusEncodeStream`, then awaits process exit and maps the exit code to a `TrackPlayResult` (`Completed`/`Failed`/`Skipped`/`NotInVoiceChannel`). There are no C# events in this pipeline; user-facing messages flow through `PlayRequest.Callbacks` and failures are retried by the background service.

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
