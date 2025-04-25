# Debug phase
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Build phase
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
ARG APPNAME=BigBang1112cz
WORKDIR /src
# copy csproj and restore as distinct layers
COPY $APPNAME/*.csproj $APPNAME/
RUN dotnet restore $APPNAME/$APPNAME.csproj -a $TARGETARCH
# copy and publish app and libraries
COPY $APPNAME $APPNAME
RUN dotnet publish $APPNAME -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app -p:PublishReadyToRun=true -p:UseAppHost=false --no-restore


# Final phase
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BigBang1112cz.dll"]