FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src
COPY . .
RUN dotnet build --nologo -c RELEASE \
        Websocket.Relay.csproj && \
    dotnet publish --nologo -c RELEASE -o /app \
        Websocket.Relay.csproj

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Websocket.Relay.dll" ]
