FROM fedora:latest AS lib-build

WORKDIR /deps

RUN dnf upgrade -y && \
    dnf install -y libsodium-devel opus-devel yt-dlp \
                   ffmpeg dotnet-runtime-9.0 dotnet-sdk-9.0 tzdata \
                   nodejs npm && \
    ln -sf /usr/share/zoneinfo/Asia/Singapore /etc/localtime

FROM lib-build AS build

ARG BUILD_CONFIGURATION=Release
ARG PROJECT_PATH=src/Worker/Worker.csproj

WORKDIR /app

COPY . .

# Go back to app root for dotnet commands
WORKDIR /app

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

FROM lib-build AS final

WORKDIR /app/run

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
ENV TZ=Asia/Singapore

COPY --from=publish /app/publish .

RUN cp /usr/lib64/libsodium.so* . && \
    cp /usr/bin/yt-dlp . && \
    cp /usr/lib64/libopus.so* . || true

RUN mkdir -p wwwroot

EXPOSE 5000
ENTRYPOINT ["dotnet", "Worker.dll"]