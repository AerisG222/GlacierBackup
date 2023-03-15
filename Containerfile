FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine-amd64 AS build

WORKDIR /src

COPY GlacierBackup.sln .
COPY src/. ./src/

RUN dotnet restore
RUN dotnet publish -o /app -c Release -r linux-musl-x64 --self-contained false


# build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine-amd64

WORKDIR /glacier-backup

COPY --from=build /app .

ENTRYPOINT [ "/glacier-backup/GlacierBackup" ]
