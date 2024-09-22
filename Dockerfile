# FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_SDK_VERSION AS build-env
FROM ubuntu:24.04 AS build-env

# Define build arguments for versioning
ARG OPUS_VERSION=1.5.2
ARG LIBSODIUM_VERSION=1.0.20
ARG FFMPEG_VERSION=6.1.1

# Define build arguments for versioning
ARG DOTNET_SDK_VERSION=8.0

RUN apt update -y && apt upgrade -y

# install dotnet
RUN apt install -y dotnet-sdk-8.0

# install needed apt packages
RUN apt install -y wget make tar gcc automake

# download libsodium and opus
RUN wget https://downloads.xiph.org/releases/opus/opus-${OPUS_VERSION}.tar.gz && tar -zxvf opus-${OPUS_VERSION}.tar.gz && wget https://download.libsodium.org/libsodium/releases/libsodium-${LIBSODIUM_VERSION}-stable.tar.gz && tar -zxvf libsodium-${LIBSODIUM_VERSION}-stable.tar.gz 

# build and install libsodium and opus
RUN cd opus-${OPUS_VERSION} && ./configure --prefix=/usr/local && make && make install  && cd ../libsodium-stable && ./configure --prefix=/usr/local && make && make install

WORKDIR /app

COPY radio-discord-bot.csproj .

RUN dotnet restore

COPY . .

RUN dotnet publish Discord_Music_Bot.sln -c Release -o out --self-contained true

# FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_SDK_VERSION}
FROM ubuntu:24.04 AS runtime

WORKDIR /app

COPY --from=build-env /app/out .

# update and upgrade apt
RUN apt update -y && apt upgrade -y

# install needed apt packages
RUN apt install -y ffmpeg

EXPOSE 8002

# this is needed if the build is not self-contained
# ENTRYPOINT ["dotnet", "radio-discord-bot.dll"]

# this is needed if the build is self-contained
ENTRYPOINT ["./radio-discord-bot"]