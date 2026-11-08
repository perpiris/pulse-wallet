# Multi-stage build: compile with the full SDK image, run on the much smaller ASP.NET runtime image.
# Build context is the repository root (see compose.yaml), so paths below are relative to it.

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the files restore needs first. Docker caches this layer, so packages are only
# restored again when a project file or package version changes, not on every code change.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/PulseWallet.Domain/PulseWallet.Domain.csproj src/PulseWallet.Domain/
COPY src/PulseWallet.Application/PulseWallet.Application.csproj src/PulseWallet.Application/
COPY src/PulseWallet.Infrastructure/PulseWallet.Infrastructure.csproj src/PulseWallet.Infrastructure/
COPY src/PulseWallet.Api/PulseWallet.Api.csproj src/PulseWallet.Api/
RUN dotnet restore src/PulseWallet.Api/PulseWallet.Api.csproj

# Now copy the source and publish.
COPY src/ src/
RUN dotnet publish src/PulseWallet.Api/PulseWallet.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The .NET images include a non-root "app" user. Running as it limits the damage if the app is compromised.
USER $APP_UID

COPY --from=build /app/publish .

# ASP.NET Core in these images listens on port 8080 by default.
EXPOSE 8080

ENTRYPOINT ["dotnet", "PulseWallet.Api.dll"]
