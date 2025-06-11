# Local Radio and Music Bot Player for Discord

**_Disclaimer_**: _This code is meant for educational purposes only. Please do not use this code for malicious purposes or to violate the [Discord](https://discord.com), [Youtube](https://www.youtube.com), [Radio Televisyen Malaysia (RTM)](https://www.rtm.gov.my) and [Astro](https://www.astro.com.my) Terms of Service._

**Note:** This code has been tested on a Linux environment. If you are using a different operating system, you may need to modify the code accordingly. You also can run this application using Docker and WSL2. Dockerfile is provided in this repository.

## Installation

To use this application, follow these steps:

1. You need to create a Discord bot and add it to your server. You can follow the instructions [here](https://discord.com/developers/docs/intro).
2. Setup up neccessary bot access permissions. You can follow the instructions [here](https://discord.com/developers/docs/topics/permissions).
3. Discord bot token is required to run this application. You can create a bot token by following the instructions [here](https://discord.com/developers/docs/topics/oauth2#bots).
3. Clone this repository.
4. Use docker-compose to build and run the application. It will automatically install the required dependencies.
   ```
   docker-compose up --build
   ```
5. Please refer to the [Discord.NET](https://discordnet.dev) for more information on how to create a Discord bot and how to use the Discord.NET library.

## Technologies Used
1. [Discord.NET](https://discordnet.dev)
2. [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)
3. [FFmpeg](https://ffmpeg.org)
4. [Opus](https://opus-codec.org)
5. [Sodium](https://doc.libsodium.org)