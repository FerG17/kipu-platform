FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bodega.Platform/Bodega.Platform.csproj", "Bodega.Platform/"]
RUN dotnet restore "Bodega.Platform/Bodega.Platform.csproj"
COPY . .
WORKDIR "/src/Bodega.Platform"
RUN dotnet build "./Bodega.Platform.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Bodega.Platform.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bodega.Platform.dll"]
