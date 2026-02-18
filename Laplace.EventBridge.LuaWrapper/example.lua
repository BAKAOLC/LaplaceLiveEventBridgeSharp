#!/usr/bin/env luajit

-- Example usage of LAPLACE Event Bridge Lua wrapper

local leb = require("laplace_event_bridge")

-- Create client
print("Creating LAPLACE Event Bridge client...")
local client = leb.LaplaceEventBridgeClient:new({
    url = "ws://localhost:9696",
    token = nil  -- Set your token here if needed
})

-- Register event handlers
client:on("message", function(event)
    print(string.format("[%s] %s: %s", 
        os.date("%H:%M:%S", math.floor(event.timestamp / 1000)),
        event.username,
        event.message))
end)

client:on("system", function(event)
    print(string.format("[%s] [SYSTEM] %s", 
        os.date("%H:%M:%S", math.floor(event.timestamp / 1000)),
        event.message))
end)

client:on("gift", function(event)
    print(string.format("[%s] %s sent %dx %s", 
        os.date("%H:%M:%S", math.floor(event.timestamp / 1000)),
        event.username,
        event.giftCount,
        event.giftName))
end)

client:on("established", function(event)
    print(string.format("Connected! Client ID: %s", event.id))
end)

-- Listen to all events for debugging
client:onAny(function(event)
    -- Uncomment to see all events
    -- print(string.format("[DEBUG] Event type: %s", event.type))
end)

-- Connect to server
print("Connecting to server...")
local ok, err = pcall(function()
    client:connect()
end)

if not ok then
    print("Failed to connect: " .. tostring(err))
    client:destroy()
    os.exit(1)
end

print("Connected! Connection state: " .. client:getStateString())
print("Listening for events... Press Ctrl+C to exit")
print()

-- Main event loop
local running = true

-- Handle Ctrl+C gracefully (platform dependent)
if pcall(require, "signal") then
    local signal = require("signal")
    signal.signal(signal.SIGINT, function()
        running = false
    end)
end

-- Event processing loop
while running do
    -- Process all pending events
    client:processEvents()
    
    -- Check connection state periodically
    local state = client:getState()
    if state == leb.ConnectionState.Disconnected then
        print("\nDisconnected from server")
        break
    end
    
    -- Sleep a bit to avoid busy-waiting
    -- You can adjust this based on your needs
    if package.config:sub(1,1) == '\\' then
        -- Windows
        os.execute("timeout /t 0 /nobreak >nul 2>&1")
    else
        -- Unix-like
        os.execute("sleep 0.01")
    end
end

-- Cleanup
print("\nShutting down...")
client:disconnect()
client:destroy()
print("Done!")
