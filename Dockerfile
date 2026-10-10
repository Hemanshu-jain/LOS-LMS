# LOS/LMS backend as a container (Render, Docker runtime). Backend only: no launcher, tunnel or updater.
# Build:  docker build -t los-lms --build-arg COMMIT=$(git rev-parse --short HEAD) .
# Needs no master-key, licence or tunnel file: those are only baked in by publish.ps1 for the desktop build.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore first, from the project files and lock files alone, so this layer is cached until a dependency changes.
COPY Directory.Build.props ./
COPY LosLms/LosLms.csproj LosLms/packages.lock.json LosLms/
COPY LosLms.Licensing/LosLms.Licensing.csproj LosLms.Licensing/packages.lock.json LosLms.Licensing/
RUN dotnet restore LosLms/LosLms.csproj --locked-mode
COPY LosLms.Licensing LosLms.Licensing
COPY LosLms LosLms
# Native libraries for every OS ship in runtimes/; the container only runs linux-x64, so drop the rest (about 70 MB).
RUN dotnet publish LosLms/LosLms.csproj -c Release -o /out --no-restore -p:UseAppHost=false \
    && cd /out/runtimes && ls | grep -v '^linux-x64$' | xargs -r rm -rf

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# QuestPDF (CAM, agreement and welcome-letter PDFs) needs fontconfig and a font on Linux.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .

ARG COMMIT=unknown
ENV ASPNETCORE_ENVIRONMENT=Web \
    Hosting__Cloud=true \
    Hosting__Commit=${COMMIT} \
    DOTNET_gcServer=0 \
    DOTNET_TieredPGO=0
USER app
EXPOSE 10000
# Render supplies $PORT. --urls (not ASPNETCORE_URLS) because appsettings.json's own "Urls" would beat the env var.
ENTRYPOINT ["sh", "-c", "exec dotnet LosLms.dll --urls http://0.0.0.0:${PORT:-10000}"]
