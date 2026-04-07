# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Discord Music Bot - A .NET 10 application that provides music playback functionality for Discord servers. Supports various audio sources, Spotify (for playlist fetching), and radio streams.

## Build & Run Commands

```bash
# Build and run with Docker (recommended)
docker-compose -f deployment/docker-compose.yml up --build

# Build .NET solution
dotnet build discord-project.slnx

# Run the Worker (main entry point)
dotnet run --project src/Worker/Worker.csproj

# Run with specific configuration
dotnet run --project src/Worker/Worker.csproj -c Release

# Frontend dev server (from src/UI/App)
bun run dev

# Frontend production build
bun run build

# Frontend development build
bun run build:dev
```

## Architecture

### Clean Architecture Layers

- **Domain** (`src/Domain/`) - Core entities (Song, User, PlayHistory, RadioSource), event interfaces (`IEventHandler<T>`, `IAsyncEventHandler<T>`), and common abstractions
- **Application** (`src/Application/`) - Business logic services, DTOs, configuration models, event dispatching system, and service interfaces
- **Infrastructure** (`src/Infrastructure/`) - Discord bot implementation using NetCord, audio processing with FFmpeg/NAudio, database access via EF Core, YouTube/SoundCloud integration
- **UI** (`src/UI/`) - Contains two sub-projects:
  - `Api/` - ASP.NET Core REST API with JWT authentication
  - `App/` - React TypeScript frontend (Vite build, Bun package manager)
- **Worker** (`src/Worker/`) - Main entry point that composes all layers, hosts the Discord bot and web API

### Key Patterns

**Event System**: Custom domain event dispatching via `IEventHandler<T>` and `IAsyncEventHandler<T>`. Handlers are auto-discovered from assemblies at startup via `AddEventing()`.

**Service Registration**: Keyed services pattern for multiple implementations:
```csharp
services.AddKeyedScoped<IStreamService, YoutubeService>(nameof(YoutubeService));
services.AddKeyedScoped<IStreamService, SoundCloudService>(nameof(SoundCloudService));
```

**Discord Commands**: Organized in `src/Infrastructure/Commands/`:
- `MusicPlayCommands.cs` - Play/queue commands
- `MusicActionCommands.cs` - Pause, skip, volume, etc.
- `AdminCommands.cs` - Admin functionality
- `MiscCommands.cs` - Utility commands

### Database

PostgreSQL with EF Core. Context: `Infrastructure/Data/DiscordBotContext.cs`. Migrations run automatically on startup.

### Audio Pipeline

Audio flows through: YoutubeDLSharp/YoutubeExplode -> FFmpeg processing (`FfmpegProcessService`) -> NAudio conversion -> NetCord voice client

### Native Dependencies (Linux/Docker)

- FFmpeg
- libsodium
- libopus
- yt-dlp

## Configuration

Environment variables (via docker-compose or appsettings.json):
- `Discord__Token` - Discord bot token
- `SpotifySettings__ClientId/ClientSecret` - Spotify API credentials
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `JwtSettings__Secret/Issuer/Audience` - JWT configuration
- `WebsiteSettings__Url` - Base URL for the web UI
- `Cors__AllowedOrigins` - Allowed CORS origins

## Frontend Stack

React 19 with TypeScript, TanStack Router, TanStack Query, Recharts. Uses Bun as the package manager and Vite 8 for bundling. Build output goes to `src/Worker/wwwroot/`.
