FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pricewatcheruserbot/pricewatcheruserbot.csproj", "pricewatcheruserbot/"]
RUN dotnet restore "pricewatcheruserbot/pricewatcheruserbot.csproj"
COPY . .
WORKDIR "/src/pricewatcheruserbot"
RUN dotnet build "pricewatcheruserbot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "pricewatcheruserbot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
WORKDIR /app/publish
COPY --from=build /app/build/playwright.ps1 ./

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN pwsh ./playwright.ps1 install firefox
RUN pwsh ./playwright.ps1 install-deps firefox
RUN apt-get update
RUN apt-get install xvfb -y
RUN apt-get install xauth -y
VOLUME /data
ENV DbConnectionString="Data Source = /data/app.db"
ENV TelegramSessionFilePath="/data/telegram-session"
ENV BrowserSessionFilePath="/data/browser-session"
ENV StorageDirectoryPath="/data/"
CMD ["/bin/sh", "-c", "/usr/bin/xvfb-run --server-args='-screen 0, 1920x1080x24' dotnet pricewatcheruserbot.dll", ""]
