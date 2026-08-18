# Keep your original structure but optimize base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS lib-build

WORKDIR /deps

RUN apt update && apt install -y --no-install-recommends \
    curl \
    libsodium-dev \
    libopus-dev \
    wget \
    unzip \
    && curl -fsSL https://bun.sh/install | bash \
    && ln -s /root/.bun/bin/bun /usr/local/bin/bun \
    && rm -rf /var/lib/apt/lists/*

RUN wget -O /tmp/libdave.zip \
    https://github.com/discord/libdave/releases/download/v1.1.1/cpp/libdave-Linux-X64-boringssl.zip && \
    unzip /tmp/libdave.zip -d /tmp/libdave && \
    cp /tmp/libdave/lib/*.so /usr/lib/ && \
    rm -rf /tmp/libdave /tmp/libdave.zip

FROM lib-build AS build

ARG BUILD_CONFIGURATION=Release
ARG PROJECT_PATH=src/Worker/Worker.csproj

# The frontend calls the API via a relative /api path (see src/UI/App/.env.production),
# so the same image works locally and in deployment without a baked-in URL.

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
    curl \
    unzip \
    && rm -rf /var/lib/apt/lists/* \
    && ln -sf /usr/share/zoneinfo/Asia/Singapore /etc/localtime

# Install nightly yt-dlp directly from GitHub releases (more frequent extractor fixes than
# the "latest" stable tag, at the cost of being less battle-tested)
ARG YT_DLP_CACHE_BUST=latest
ADD https://github.com/yt-dlp/yt-dlp/releases/download/nightly/yt-dlp /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp

# Deno - JS runtime for yt-dlp's bundled EJS challenge solver, which handles YouTube's
# signature/n-parameter challenges on web-flavored clients (see
# https://github.com/yt-dlp/yt-dlp/wiki/EJS). Without it, yt-dlp silently drops formats
# and can fail with "Requested format is not available".
RUN curl -fsSL https://deno.land/install.sh | sh -s -- -y \
    && ln -s /root/.deno/bin/deno /usr/local/bin/deno

# bgutil-ytdlp-pot-provider plugin - mints PO tokens so yt-dlp's requests look like a real
# browser session, working around YouTube blocking/throttling datacenter IPs. Tracks latest
# like yt-dlp above; the bgutil-provider server image in docker-compose.yml also tracks
# :latest, so a plain workflow re-run picks up new releases on both sides without a code change.
ADD https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/latest/download/bgutil-ytdlp-pot-provider.zip /root/.yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip

WORKDIR /app/run

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV TZ=Asia/Singapore

COPY --from=publish /app/publish .

RUN mkdir -p wwwroot

EXPOSE 5000
ENTRYPOINT ["dotnet", "Worker.dll"]