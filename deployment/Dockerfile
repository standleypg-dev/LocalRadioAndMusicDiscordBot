# Keep your original structure but optimize base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS lib-build

WORKDIR /deps

RUN apt update && apt install -y --no-install-recommends \
    curl \
    libsodium-dev \
    libopus-dev \
    wget \
    unzip \
    && curl -fsSL https://deb.nodesource.com/setup_24.x | bash - \
    && apt install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

RUN wget -O /tmp/libdave.zip \
    https://github.com/discord/libdave/releases/download/v1.1.1/cpp/libdave-Linux-X64-boringssl.zip && \
    unzip /tmp/libdave.zip -d /tmp/libdave && \
    cp /tmp/libdave/lib/*.so /usr/lib/ && \
    rm -rf /tmp/libdave /tmp/libdave.zip

FROM lib-build AS build

ARG BUILD_CONFIGURATION=Release
ARG PROJECT_PATH=src/Worker/Worker.csproj

WORKDIR /app

COPY . .

RUN dotnet restore "$PROJECT_PATH"

RUN dotnet build "$PROJECT_PATH" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --os linux \
    --p:DebugSymbols=true \
    --p:DebugType=portable

FROM build AS publish

ARG BUILD_CONFIGURATION=Release
WORKDIR /app

RUN dotnet publish "$PROJECT_PATH" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --os linux \
    --self-contained false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt update && apt install -y --no-install-recommends \
    ffmpeg \
    tzdata \
    python3 \
    && rm -rf /var/lib/apt/lists/* \
    && ln -sf /usr/share/zoneinfo/Asia/Singapore /etc/localtime

# Install latest yt-dlp directly from GitHub releases
ADD https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp
    
WORKDIR /app/run

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV TZ=Asia/Singapore

COPY --from=publish /app/publish .

RUN mkdir -p wwwroot

EXPOSE 5000
ENTRYPOINT ["dotnet", "Worker.dll"]