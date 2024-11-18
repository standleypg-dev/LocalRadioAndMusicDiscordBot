FROM fedora:latest as base
ARG OPUS_VERSION=1.5.2
ARG LIBSODIUM_VERSION=1.0.20
ARG FFMPEG_VERSION=6.1.1
WORKDIR /app
RUN dnf update -y &&\
    dnf upgrade -y &&\
    dnf install -y ffmpeg dotnet-sdk-8.0 dotnet-runtime-8.0


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/radio-discord-bot.csproj", "src/"]
RUN dotnet restore "./src/radio-discord-bot.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "./radio-discord-bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./radio-discord-bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r linux-x64 #--self-contained true 

FROM base AS lib-build
# install needed dnf packages
RUN dnf install -y wget make tar gcc automake
# download libsodium and opus
RUN wget https://downloads.xiph.org/releases/opus/opus-${OPUS_VERSION}.tar.gz &&\
    tar -zxvf opus-${OPUS_VERSION}.tar.gz &&\
    wget https://download.libsodium.org/libsodium/releases/libsodium-${LIBSODIUM_VERSION}-stable.tar.gz &&\
    tar -zxvf libsodium-${LIBSODIUM_VERSION}-stable.tar.gz 

# build and install libsodium and opus
RUN cd opus-${OPUS_VERSION} &&\
    ./configure --prefix=/usr/local &&\
    make &&\
    make install  &&\
    cd ../libsodium-stable &&\
    ./configure --prefix=/usr/local &&\
    make &&\
    make install

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=lib-build /usr/local/lib/libsodium.so .
COPY --from=lib-build /usr/local/lib/libopus.so .
# this is needed if the build is not self-contained
# also, if you wish to debug on docker, this is needed
ENTRYPOINT ["dotnet", "radio-discord-bot.dll"]
# this is needed if the build is self-contained
#ENTRYPOINT ["./radio-discord-bot"]