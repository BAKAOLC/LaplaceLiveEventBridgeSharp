# LAPLACE Event Bridge Lua Wrapper

Lua FFI wrapper for LAPLACE Event Bridge with Native AOT support.

## Build

**⚠️ MUST use `dotnet publish`, NOT `dotnet build`**

```bash
# Windows
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64

# macOS
dotnet publish -c Release -r osx-x64
```

Output: `bin/Release/net10.0/{runtime}/publish/Laplace.EventBridge.LuaWrapper.dll`

## Usage

```lua
local leb = require("laplace_event_bridge")

local client = leb.LaplaceEventBridgeClient:new({
    url = "ws://localhost:9696"
})

client:on("message", function(event)
    print(event.username .. ": " .. event.message)
end)

-- Blocking connection
client:connect()

-- OR async connection
client:connectAsync()

-- Main loop
while running do
    client:processEvents()
    -- Your game logic
end

client:disconnect()
client:destroy()
```

## API

### Connection

- `client:connect()` - Sync (blocks until connected)
- `client:connectAsync()` - Async (returns immediately)
- `client:disconnect()` - Disconnect
- `client:isConnected()` - Check if connected
- `client:isConnecting()` - Check if connecting
- `client:getState()` - Get connection state (0=Disconnected, 1=Connecting, 2=Connected, 3=Reconnecting)

### Events

- `client:on(eventType, handler)` - Register handler for specific event type
- `client:onAny(handler)` - Register handler for all events
- `client:processEvents()` - Process all pending events (call in main loop)
- `client:pollEvent()` - Poll single event (returns event or nil)
- `client:getEventCount()` - Get pending event count
- `client:clearEvents()` - Clear all pending events

### Configuration

- `client:setMaxQueueSize(size)` - Set max queue size (default: 1000)

## Example

See [example.lua](example.lua) for complete example.

```bash
luajit example.lua
```

## License

MIT
