# Stage 1: Base with system + native dependencies
FROM fedora:latest AS lib-build

WORKDIR /deps

RUN dnf upgrade -y && \
    dnf install -y libsodium-devel opus-devel yt-dlp \
                   ffmpeg dotnet-runtime-9.0 dotnet-sdk-9.0

# Stage 2: Build the application
FROM lib-build AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /app

# Copy all project files first
COPY ["src/radio-discord-bot.csproj", "src/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["Domain/Domain.csproj", "Domain/"]

# Restore dependencies
RUN dotnet restore "src/radio-discord-bot.csproj"

# Copy the rest of the source code
COPY src/ src/
COPY Data/ Data/
COPY Domain/ Domain/

# Set working directory to the main project
WORKDIR /app/src

# Build the application
RUN dotnet build "radio-discord-bot.csproj" -c $BUILD_CONFIGURATION -o /app/build --os linux --p:DebugSymbols=true --p:DebugType=portable

# Stage 3: Publish the application
FROM build AS publish

ARG BUILD_CONFIGURATION=Release
WORKDIR /app/src
RUN dotnet publish "radio-discord-bot.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --os linux \
    --self-contained false \
    --no-restore

# Stage 4: Final runtime image
FROM lib-build AS final

WORKDIR /app

# Set production environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production

# Copy published application
COPY --from=publish /app/publish .

RUN cp /usr/lib64/libsodium.so* . && \
    cp /usr/bin/yt-dlp . && \
    cp /usr/lib64/libopus.so* . || true

ENTRYPOINT ["dotnet", "radio-discord-bot.dll"]
# If you use self-contained: 
# ENTRYPOINT ["./radio-discord-bot"]