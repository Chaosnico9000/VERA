FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VERA.Shared/VERA.Shared.csproj VERA.Shared/
COPY VERA.Server/VERA.Server.csproj VERA.Server/
RUN dotnet restore VERA.Server/VERA.Server.csproj

COPY VERA.Shared/ VERA.Shared/
COPY VERA.Server/ VERA.Server/
RUN dotnet publish VERA.Server/VERA.Server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 vera && \
    adduser  --system --uid 1001 --ingroup vera vera

RUN mkdir -p /data && chown vera:vera /data

COPY --from=build --chown=vera:vera /app/publish .

USER vera

VOLUME ["/data"]
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DataDirectory=/data

ENTRYPOINT ["dotnet", "VERA.Server.dll"]
