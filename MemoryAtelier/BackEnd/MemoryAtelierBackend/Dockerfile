FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MemoryAtelier/BackEnd/MemoryAtelierBackend/MemoryAtelierBackend.csproj MemoryAtelier/BackEnd/MemoryAtelierBackend/
RUN dotnet restore MemoryAtelier/BackEnd/MemoryAtelierBackend/MemoryAtelierBackend.csproj
COPY MemoryAtelier/BackEnd/MemoryAtelierBackend/ MemoryAtelier/BackEnd/MemoryAtelierBackend/
RUN dotnet publish MemoryAtelier/BackEnd/MemoryAtelierBackend/MemoryAtelierBackend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MemoryAtelierBackend.dll"]
