# WebSocket Relay

This is a simple WebSocket server that broadcasts messages from one (or more) sender to all
receiver in the same group.

One can create a new group with a call to the `/api/new` endpoint. This will return the group id and
the sender authentification token.

```curl
curl http://domain.example/api/new
```
```json
{
    "id": "0123456789abcdef",
    "token": "fedcba9876543210"
}
```

After that the sender or receiver can connect to the WebSocket endpoint `/ws`:

```javascript
let socket = new WebSocket('ws://domain.example/ws?id=0123456789abcdef');
```

Now the sender can JSON Packages to the WebSocket endpoint. The payload has to have the type, the
token and the value. The type is constant, the token was provided before and the value can be any
json value what you application supports.

```javascript
socket.send(JSON.stringify({
    "$type": "Relay",
    "token": "fedcba9876543210",
    "value": { "any": "valid", "json": [ null ] }
}));
```

The receiver will receive a JSON string that looks like this:

```json
{
    "$type": "Relay",
    "value": { "any": "valid", "json": [ null ] }
}
```

Your application can provide a method to share the token. Anyone that has the valid token is able
to broadcast messages.

If in the sending message the token is missing or invalid the message is discarded. If the time
delay since the last successful sending is less than 50 ms the message is also discarded. It is up
to you to always provide the correct token and reduce the messages per seconds (if you don't want to
loose data).

There is no message replay or notification if a new receiver has connected to your group. There is
also no notification if anyone left the group. If the last member of the group left, the group is
discarded and can no longer used. You have to create a new one.

It is also recommended to reduce the data you intent to send. Large packets will take it's time to
send to every receiver (if their internet connection is slow).
