# Stage 1: Base with system + native dependencies
FROM fedora:latest AS lib-build

ARG OPUS_VERSION=1.5.2
ARG LIBSODIUM_VERSION=1.0.20

WORKDIR /deps

RUN dnf upgrade -y && \
    dnf install -y libsodium-devel opus-devel \
                   ffmpeg dotnet-sdk-9.0 dotnet-runtime-9.0

# Stage 2: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/radio-discord-bot.csproj", "src/"]
RUN dotnet restore "src/radio-discord-bot.csproj"
COPY . .
WORKDIR /src/src
RUN dotnet build "radio-discord-bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish the application
FROM build AS publish

ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "radio-discord-bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r linux-x64 # --self-contained true

# Stage 4: Final runtime image
FROM lib-build AS final

WORKDIR /app

# Optional: if dynamically linked to shared libraries
ENV LD_LIBRARY_PATH="/usr/local/lib:$LD_LIBRARY_PATH"

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "radio-discord-bot.dll"]
# If you use self-contained: 
# ENTRYPOINT ["./radio-discord-bot"]
