# Malaysia Local Radio and Music Discord Bot Player

**_Disclaimer_**: _This code is meant for educational purposes only. Please do not use this code for malicious purposes or to violate the [Discord](https://discord.com), [Youtube](https://www.youtube.com), [Radio Televisyen Malaysia (RTM)](https://www.rtm.gov.my) and [Astro](https://www.astro.com.my) Terms of Service._

**Note:** This code has been tested on a Linux environment. If you are using a different operating system, you may need to modify the code accordingly. You also can run this application using Docker and WSL2. Dockerfile is provided in this repository.

## Installation

To use this application, follow these steps:

1. You need to create a Discord bot and add it to your server. You can follow the instructions [here](https://discord.com/developers/docs/intro).
2. Setup up neccessary bot access permissions. You can follow the instructions [here](https://discord.com/developers/docs/topics/permissions).
3. Clone this repository.
4. Install the required native libraries. For Debian-based Linux distributions, you can use the following command:
   ```
   sudo apt update && sudo apt upgrade
   sudo apt install wget make tar gcc automake libtool pkg-config libopus0 opus-tools ffmpeg
   ```
5. Install Opus native library from source. You can use the following command:
   ```
   wget https://ftp.osuosl.org/pub/xiph/releases/opus/opus-1.4.tar.gz
   tar -xvf opus-1.4.tar.gz
   cd opus-1.4
   ./configure
   make
   sudo make install
   ```
6. Install Sodium native library from source. You can use the following command:
   ```
   wget https://download.libsodium.org/libsodium/releases/libsodium-1.0.18.tar.gz
   tar -xvf libsodium-1.0.18.tar.gz
   cd libsodium-1.0.18
   ./configure
   make
   sudo make install
   ```
7. Both of these libraries are required to run the application and must be placed in the same directory as the executable file. Example:
   ```
   /home/user/RadioTempatanBot/bin/Debug/net7.0/
   ```
8. You might encounter some issues with the audio stream, please check your installed ffmpeg version. If you're using ffmpeg version lower than 4.3.6, you need to update it.
   You can check the installed ffmpeg version again using the following command:
   ```
   ffmpeg -version
   ```
9. If you're using Windows, you might need to change the code below at `CreateStream()` at AudioService.cs file according to your ffmpeg installation path.
   ```
    private Process CreateStream(string audioUrl)
    {
        var ffmpeg = new ProcessStartInfo
        {
            FileName = "/usr/bin/ffmpeg",
            Arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i {audioUrl} -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        return Process.Start(ffmpeg);
    }
   ```
10. Also, if you're using Windows, you might encounter some issues with this application as it is not tested on Windows environment. You can try to run this application using Docker and WSL2.
11. Feel free to modify the code according to your needs and register any issues that you encounter.
12. You need to host this application on a server or your local machine to in order to use it.
13. Pull requests are welcome :)
14. Please refer to the [Discord.NET](https://discordnet.dev) for more information on how to create a Discord bot and how to use the Discord.NET library.

