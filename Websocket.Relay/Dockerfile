FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder
WORKDIR /src
COPY . .
RUN dotnet build --nologo -c RELEASE \
        Websocket.Relay.csproj && \
    dotnet publish --nologo -c RELEASE -o /app \
        Websocket.Relay.csproj

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Websocket.Relay.dll" ]
