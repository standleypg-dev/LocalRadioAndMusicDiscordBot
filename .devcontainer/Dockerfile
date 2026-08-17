# Dev-only image. Mirrors deployment/Dockerfile's toolchain (keep native
# dependency versions in sync manually when that file changes) but is not
# multi-stage: source is bind-mounted by docker-compose.yml, not copied in.
# Platform is pinned to linux/amd64 in docker-compose.yml, not here - native
# lib paths below are Debian amd64 multiarch and only resolve correctly there.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble

RUN apt-get update && apt-get install -y --no-install-recommends \
    libsodium-dev \
    libopus-dev \
    ffmpeg \
    tzdata \
    python3 \
    curl \
    wget \
    unzip \
    git \
    && rm -rf /var/lib/apt/lists/*

# Bun - Worker.csproj's BuildFrontend target shells out to `bun` on every
# `dotnet build`, so it must be on PATH here (not just in CI/production).
RUN curl -fsSL https://bun.sh/install | bash \
    && ln -s /root/.bun/bin/bun /usr/local/bin/bun

# libdave (Discord E2EE voice lib) - version pinned to match deployment/Dockerfile.
RUN wget -O /tmp/libdave.zip \
    https://github.com/discord/libdave/releases/download/v1.1.1/cpp/libdave-Linux-X64-boringssl.zip \
    && unzip /tmp/libdave.zip -d /tmp/libdave \
    && cp /tmp/libdave/lib/*.so /usr/lib/ \
    && rm -rf /tmp/libdave /tmp/libdave.zip

# yt-dlp - same fetch method as deployment/Dockerfile.
ADD https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp /usr/local/bin/yt-dlp
RUN chmod a+rx /usr/local/bin/yt-dlp

# bgutil-ytdlp-pot-provider plugin - same fetch method as deployment/Dockerfile, tracks latest.
ADD https://github.com/Brainicism/bgutil-ytdlp-pot-provider/releases/latest/download/bgutil-ytdlp-pot-provider.zip /root/.yt-dlp/plugins/bgutil-ytdlp-pot-provider.zip

ENV TZ=Asia/Singapore
RUN ln -sf /usr/share/zoneinfo/$TZ /etc/localtime

WORKDIR /workspace
