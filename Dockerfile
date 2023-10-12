FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

WORKDIR /app

COPY radio-discord-bot.csproj .

RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o out --self-contained true

FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app

RUN apt update && apt install -y wget make tar gcc automake libtool pkg-config libopus0 opus-tools ffmpeg && wget https://ftp.osuosl.org/pub/xiph/releases/opus/opus-1.4.tar.gz && tar -zxvf opus-1.4.tar.gz

COPY --from=build-env /app/out .

RUN ./opus-1.4/configure --prefix=/usr/local && make && make install && cp /usr/local/lib/libopus.so /app

EXPOSE 8002

ENTRYPOINT ["dotnet", "radio-discord-bot.dll"]