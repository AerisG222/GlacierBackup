FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine-amd64 AS build

WORKDIR /src

COPY GlacierBackup.sln .
COPY src/. ./src/

RUN dotnet restore
RUN dotnet publish src/GlacierBackup/GlacierBackup.csproj -o /app -c Release -r linux-musl-x64 --self-contained false


# build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-amd64

WORKDIR /glacier-backup

COPY --from=build /app .

ENTRYPOINT [ "/glacier-backup/GlacierBackup" ]
