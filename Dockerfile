FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PeKeyMngr.csproj", "./"]
RUN dotnet restore "PeKeyMngr.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./PeKeyMngr.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PeKeyMngr.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir /app/Data
RUN chown -R $APP_UID:$APP_UID /app/Data
USER $APP_UID

ENTRYPOINT ["dotnet", "PeKeyMngr.dll"]