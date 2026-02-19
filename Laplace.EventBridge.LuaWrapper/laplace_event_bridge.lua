-- LAPLACE Event Bridge Lua FFI Binding
-- Requires LuaJIT or Lua with FFI support

local ffi = require("ffi")

-- C API declarations
ffi.cdef [[
    // Client management
    int leb_create_client(const char* url, const char* token);
    int leb_connect(int clientId);
    int leb_connect_async(int clientId);
    int leb_disconnect(int clientId);
    int leb_destroy_client(int clientId);
    
    // Connection state
    int leb_get_state(int clientId);
    int leb_get_client_id(int clientId, char* buffer, int bufferSize);
    
    // Event polling
    int leb_poll_event(
        int clientId,
        char* eventType, int eventTypeSize,
        char* id, int idSize,
        char* username, int usernameSize,
        char* message, int messageSize,
        char* giftName, int giftNameSize,
        int* giftCount,
        int64_t* timestamp,
        int* origin,
        int64_t* uid
    );
    int leb_get_event_count(int clientId);
    int leb_clear_events(int clientId);
    
    // Configuration
    int leb_set_max_queue_size(int clientId, int maxSize);
]]

-- Module configuration
local M = {}

-- Load the native library
-- You must call init() before using the library
-- Example: 
--   local leb = require("laplace_event_bridge")
--   leb:init()  -- Auto-detect from current directory: ./windows/x64/xxx.dll
--   leb:init("/path/to/libs/")  -- Auto-detect from custom directory
--   leb:init("/path/to/libs/", "custom.dll")  -- Custom file in custom directory
--   leb:init(nil, "custom.dll")  -- Custom file in current directory
local leb

function M:init(custom_dir, custom_file)
    if leb then
        return
    end  -- Already initialized

    local lib_name
    
    if custom_file then
        -- Custom file specified
        local dir = custom_dir or ""
        lib_name = dir .. custom_file
    else
        -- Auto-detect platform and architecture
        local dir = custom_dir or ""
        local arch = ffi.arch
        local platform_dir = ""
        local arch_dir = ""
        
        -- Determine platform directory
        if ffi.os == "Windows" then
            platform_dir = "windows/"
        elseif ffi.os == "Linux" then
            platform_dir = "linux/"
        elseif ffi.os == "OSX" then
            platform_dir = "osx/"
        else
            error("Unsupported platform: " .. ffi.os)
        end
        
        -- Determine architecture directory
        if arch == "x64" or arch == "x86_64" then
            arch_dir = "x64/"
        elseif arch == "x86" then
            arch_dir = "x86/"
        elseif arch == "arm64" or arch == "aarch64" then
            arch_dir = "arm64/"
        else
            error("Unsupported architecture: " .. arch)
        end
        
        -- Determine library name
        local lib_file = ""
        if ffi.os == "Windows" then
            lib_file = "Laplace.EventBridge.LuaWrapper.dll"
        elseif ffi.os == "Linux" then
            lib_file = "libLaplace.EventBridge.LuaWrapper.so"
        elseif ffi.os == "OSX" then
            lib_file = "libLaplace.EventBridge.LuaWrapper.dylib"
        end
        
        lib_name = dir .. platform_dir .. arch_dir .. lib_file
    end

    leb = ffi.load(lib_name)
end

-- Connection states
local ConnectionState = {
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3
}

-- Event Bridge Client class
local LaplaceEventBridgeClient = {}
LaplaceEventBridgeClient.__index = LaplaceEventBridgeClient

function LaplaceEventBridgeClient:new(options)
    options = options or {}
    local url = options.url or "ws://localhost:9696"
    local token = options.token

    local clientId = leb.leb_create_client(url, token)
    if clientId < 0 then
        error("Failed to create client: " .. clientId)
    end

    local self = setmetatable({
        _clientId = clientId,
        _eventHandlers = {},
        _anyHandlers = {}
    }, LaplaceEventBridgeClient)

    return self
end

function LaplaceEventBridgeClient:connect()
    local result = leb.leb_connect(self._clientId)
    if result < 0 then
        error("Failed to connect: " .. result)
    end
end

function LaplaceEventBridgeClient:connectAsync()
    local result = leb.leb_connect_async(self._clientId)
    if result < 0 then
        error("Failed to start async connect: " .. result)
    end
end

function LaplaceEventBridgeClient:isConnected()
    local state = self:getState()
    return state == ConnectionState.Connected
end

function LaplaceEventBridgeClient:isConnecting()
    local state = self:getState()
    return state == ConnectionState.Connecting or state == ConnectionState.Reconnecting
end

function LaplaceEventBridgeClient:disconnect()
    local result = leb.leb_disconnect(self._clientId)
    if result < 0 then
        error("Failed to disconnect: " .. result)
    end
end

function LaplaceEventBridgeClient:getState()
    local state = leb.leb_get_state(self._clientId)
    if state < 0 then
        return nil, "Failed to get state: " .. state
    end
    return state
end

function LaplaceEventBridgeClient:getStateString()
    local state = self:getState()
    if not state then
        return "Unknown"
    end

    if state == ConnectionState.Disconnected then
        return "Disconnected"
    elseif state == ConnectionState.Connecting then
        return "Connecting"
    elseif state == ConnectionState.Connected then
        return "Connected"
    elseif state == ConnectionState.Reconnecting then
        return "Reconnecting"
    else
        return "Unknown"
    end
end

function LaplaceEventBridgeClient:getClientId()
    local buffer = ffi.new("char[256]")
    local len = leb.leb_get_client_id(self._clientId, buffer, 256)
    if len < 0 then
        return nil
    elseif len == 0 then
        return nil
    end
    return ffi.string(buffer, len)
end

function LaplaceEventBridgeClient:pollEvent()
    local eventType = ffi.new("char[64]")
    local id = ffi.new("char[256]")
    local username = ffi.new("char[256]")
    local message = ffi.new("char[4096]")
    local giftName = ffi.new("char[256]")
    local giftCount = ffi.new("int[1]")
    local timestamp = ffi.new("int64_t[1]")
    local origin = ffi.new("int[1]")
    local uid = ffi.new("int64_t[1]")

    local result = leb.leb_poll_event(
            self._clientId,
            eventType, 64,
            id, 256,
            username, 256,
            message, 4096,
            giftName, 256,
            giftCount,
            timestamp,
            origin,
            uid
    )

    if result < 0 then
        return nil, "Error polling event: " .. result
    elseif result == 0 then
        return nil -- No events
    end

    -- Build event table
    local event = {
        type = ffi.string(eventType),
        id = ffi.string(id),
        username = ffi.string(username),
        message = ffi.string(message),
        giftName = ffi.string(giftName),
        giftCount = giftCount[0],
        timestamp = tonumber(timestamp[0]),
        origin = origin[0],
        uid = tonumber(uid[0])
    }

    return event
end

function LaplaceEventBridgeClient:getEventCount()
    return leb.leb_get_event_count(self._clientId)
end

function LaplaceEventBridgeClient:clearEvents()
    leb.leb_clear_events(self._clientId)
end

function LaplaceEventBridgeClient:setMaxQueueSize(maxSize)
    leb.leb_set_max_queue_size(self._clientId, maxSize)
end

function LaplaceEventBridgeClient:on(eventType, handler)
    if not self._eventHandlers[eventType] then
        self._eventHandlers[eventType] = {}
    end
    table.insert(self._eventHandlers[eventType], handler)
end

function LaplaceEventBridgeClient:onAny(handler)
    table.insert(self._anyHandlers, handler)
end

function LaplaceEventBridgeClient:processEvents()
    while true do
        local event = self:pollEvent()
        if not event then
            break
        end

        -- Call type-specific handlers
        local handlers = self._eventHandlers[event.type]
        if handlers then
            for _, handler in ipairs(handlers) do
                local ok, err = pcall(handler, event)
                if not ok then
                    print("Error in event handler: " .. tostring(err))
                end
            end
        end

        -- Call any handlers
        for _, handler in ipairs(self._anyHandlers) do
            local ok, err = pcall(handler, event)
            if not ok then
                print("Error in any event handler: " .. tostring(err))
            end
        end
    end
end

function LaplaceEventBridgeClient:destroy()
    if self._clientId then
        leb.leb_destroy_client(self._clientId)
        self._clientId = nil
    end
end

-- Export module
M.LaplaceEventBridgeClient = LaplaceEventBridgeClient
M.ConnectionState = ConnectionState

return M
