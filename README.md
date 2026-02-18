# LAPLACE Event Bridge C# SDK

C# SDK for LAPLACE Event Bridge with AOT support. Includes [Lua FFI Wrapper](Laplace.EventBridge.LuaWrapper/).

> **Note:** This is a C# port of the original [LAPLACE Event Bridge](https://github.com/laplace-live/event-bridge).

## Quick Start (C#)

```csharp
using Laplace.EventBridge.Sdk;

// Create a client
var client = new LaplaceEventBridgeClient(new ConnectionOptions
{
    Url = "ws://localhost:9696",
    Token = "your-auth-token", // Optional
    Reconnect = true,
    ReconnectInterval = 3000,
    MaxReconnectAttempts = 10
});

// Listen for message events
client.On<MessageEvent>(evt =>
{
    Console.WriteLine($"{evt.Username}: {evt.Message}");
});

// Listen for system events
client.On<SystemEvent>(evt =>
{
    Console.WriteLine($"[SYSTEM] {evt.Message}");
});

// Listen for connection state changes
client.OnConnectionStateChange(state =>
{
    Console.WriteLine($"Connection state: {state}");
});

// Connect to the bridge
await client.ConnectAsync();

// Keep running
await Task.Delay(Timeout.Infinite);

// Disconnect when done
await client.DisconnectAsync();
```

## Quick Start (Lua)

```lua
local leb = require("laplace_event_bridge")

local client = leb.LaplaceEventBridgeClient:new({
    url = "ws://localhost:9696"
})

client:on("message", function(event)
    print(event.username .. ": " .. event.message)
end)

client:connect()

while true do
    client:processEvents()
end
```

📚 **Lua documentation:** [Laplace.EventBridge.LuaWrapper/](Laplace.EventBridge.LuaWrapper/)

## Build

```bash
# C# SDK
dotnet build

# Lua wrapper (Native AOT)
cd Laplace.EventBridge.LuaWrapper
dotnet publish -c Release -r win-x64
```

## License

MIT
