FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS base
WORKDIR /app
EXPOSE 7065

ENV ASPNETCORE_URLS=http://+:7065

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /src
COPY ["Api.csproj", "./"]
COPY ["nuget.config", "./"]
RUN dotnet nuget add source https://nuget.pkg.github.com/yodobrin/index.json
RUN dotnet restore "Api.csproj" --configfile nuget.config
COPY . .
WORKDIR "/src/."
RUN dotnet build "Api.csproj" --configfile nuget.config -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" --configfile nuget.config -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
