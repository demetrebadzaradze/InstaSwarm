# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root
ARG UID=1000
ARG GID=1000
RUN usermod -u $UID app && groupmod -g $GID app

# Install yt-dlp and dependencies as root.
USER root
# Install necessary packages and yt-dlp
# i think we need python3, pip, curl, ffmpeg but it takes a lot of time to install so we might want to optimize this later
RUN apt-get update && \
    apt-get install -y --no-install-recommends python3 python3-pip curl ffmpeg && \
    curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp && \
    chmod +x /usr/local/bin/yt-dlp && \
    /usr/local/bin/yt-dlp --version && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    chown app:app /usr/local/bin/yt-dlp && \
    ln -s /usr/local/bin/yt-dlp /app/yt-dlp.exe  # Symlink for potential Windows path compatibility

# Set the working directory and ensure the video directory exists
RUN mkdir /app/video
RUN chmod -R 777 /app/video
RUN chown -R app /app

USER app

VOLUME /app/video 

# Copy the HTTPS certificate file into the container
COPY https-dev.pfx /app/https-dev.pfx


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InstaSwarm.csproj", "."]
RUN dotnet restore "./InstaSwarm.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./InstaSwarm.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InstaSwarm.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "InstaSwarm.dll"] 
