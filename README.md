# Discord Music Bot

A .NET 10 Discord bot for music playback, supporting various audio sources and radio streams. Includes a React-based web dashboard.

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

## Technologies Used

### Backend
- [.NET 10](https://dotnet.microsoft.com) / ASP.NET Core
- [NetCord](https://github.com/NetCordDev/NetCord) - Discord bot framework
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) + [PostgreSQL](https://www.postgresql.org)
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) / [YoutubeDLSharp](https://github.com/Bluegrams/YoutubeDLSharp) + [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [SoundCloudExplode](https://github.com/jerry08/SoundCloudExplode)
- [NAudio](https://github.com/naudio/NAudio) / [FFmpeg](https://ffmpeg.org) - audio processing
- [libopus](https://opus-codec.org) / [libsodium](https://doc.libsodium.org) - voice encryption and encoding

### Frontend
- [React 19](https://react.dev) with TypeScript
- [TanStack Router](https://tanstack.com/router) / [TanStack Query](https://tanstack.com/query)
- [Recharts](https://recharts.org)
- [Vite](https://vite.dev) + [Bun](https://bun.sh)