# Keep your original structure but optimize base image
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS lib-build

WORKDIR /deps

RUN apk add --no-cache \
    nodejs \
    npm \
    ffmpeg \
    libsodium-dev \
    opus-dev \
    yt-dlp \
    tzdata && \
    ln -sf /usr/share/zoneinfo/Asia/Singapore /etc/localtime

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

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS final

RUN apk add --no-cache \
    ffmpeg \
    tzdata \
    yt-dlp && \
    ln -sf /usr/share/zoneinfo/Asia/Singapore /etc/localtime
    
WORKDIR /app/run

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV TZ=Asia/Singapore

COPY --from=publish /app/publish .

RUN mkdir -p wwwroot

EXPOSE 5000
ENTRYPOINT ["dotnet", "Worker.dll"]