// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

/**
 * [API] DISCORD
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_DiscordAPI : CPHInlineBase {

    private const string API_GUILDS_URL       = "https://discord.com/api/v10/users/@me/guilds";
    private const string API_MEMBER_URL       = "https://discord.com/api/v10/guilds/{0}/members/{1}";
    private const string API_CHANNEL_LIST_URL = "https://discord.com/api/v10/guilds/{0}/channels";
    private const string API_CHANNEL_INFO_URL = "https://discord.com/api/v10/channels/{0}";
    private const string DISCORD_GATEWAY      = "wss://gateway.discord.gg/?v=10&encoding=json";
    
    private const string MESSAGE_RECEIVED_EVENT = "MESSAGE_RECEIVED";
    private const string THREAD_MESSAGE_EVENT   = "THREAD_MESSAGE_RECEIVED";
    private const string MESSAGE_REACTION_ADDED = "REACTION_ADDED";
    private const string VOICE_GENERIC_EVENT    = "VOICE_GENERIC";
    private const string VOICE_JOIN_EVENT       = "VOICE_JOIN";
    private const string VOICE_LEFT_EVENT       = "VOICE_LEFT";
    private const string VOICE_MOVE_EVENT       = "VOICE_MOVE";
    private const string VOICE_UPDATE_EVENT     = "VOICE_UPDATE";
    private const string MEMBER_JOIN            = "MEMBER_JOIN";
    private const string MEMBER_LEFT            = "MEMBER_LEFT";
    private const string MEMBER_UPDATE          = "MEMBER_UPDATE";
    
    private static readonly Dictionary<string, string>         serverNames    = new();
    private static readonly Dictionary<string, DiscordChannel> serverChannels = new();
    private static readonly Dictionary<string, string>         userToVChannel = new();
    
    private static bool includeJsons = true; 
    private string discordToken;
    
    private void init() {
        includeJsons = getProperty("discord.json.included", true);
        discordToken = getProperty("discord.token", "DUMMY_TOKEN");
        _ = getDiscordServers();
        _ = connectToGateway();
    }
    
    //----------------------------------------------------------------
    // PUBLIC METHODS
    //----------------------------------------------------------------
    public bool updateGuildUserRole() {
        var overwrite = getProperty("discord.roles.override", false);
        var guildId = getProperty("discord.guild.id", "DUMMY");
        var userId = getProperty("discord.user.id", "DUMMY");
        var roleId = getProperty("discord.role.id", "");
        
        if (guildId.Equals("DUMMY") || userId.Equals("DUMMY")) {
            return false;
        }
        
        _ = updateGuildUserRole(overwrite, guildId, userId, roleId);
        return true;
    }
    
    //----------------------------------------------------------------
    // DISCORD EVENTS HANDLER
    //----------------------------------------------------------------
    private void onEventReceived(DiscordGatewayMessage gatewayMessage) {
        try {
            Action handleEvent = gatewayMessage.T switch {
                "MESSAGE_CREATE"     => () => onMessageReceived(deserializeEntity<DiscordMessage>(gatewayMessage.D)),
                "VOICE_STATE_UPDATE" => () => onVoiceStateChange(deserializeEntity<DiscordVoiceState>(gatewayMessage.D)),
                "GUILD_MEMBER_ADD"    => () => onGuildMember(MEMBER_JOIN, deserializeEntity<DiscordMember>(gatewayMessage.D)),
                "GUILD_MEMBER_REMOVE" => () => onGuildMember(MEMBER_LEFT, deserializeEntity<DiscordMember>(gatewayMessage.D)),
                "GUILD_MEMBER_UPDATE" => () => onGuildMember(MEMBER_UPDATE, deserializeEntity<DiscordMember>(gatewayMessage.D)),
                "MESSAGE_REACTION_ADD" => () => onMessageReaction(deserializeEntity<DiscordReaction>(gatewayMessage.D)),
                _ => () => INFO(() => $"GATEWAY: unhandled Discord Event {gatewayMessage.T}")
            };
            handleEvent();
        } catch (Exception e) {
            DEBUG(() => $"{e.Message}");
        }
    }
    
    private void onMessageReceived(DiscordMessage discordMessage) {
        DEBUG(() => $"EVENT: MESSAGE_RECEIVED({discordMessage.channelId}): {discordMessage.Author.userName} > {discordMessage.content}");
        
        var properties = new Dictionary<string, object> ().addAll(discordMessage.toCollection());
        var channel = serverChannels.getValueOrDefault($"{discordMessage.guildId}_{discordMessage.channelId}");
        var parentId = channel?.parentId ?? "NO_CATEGORY";
        
        if (channel != null && channel.typeName.Contains("THREAD")) {
            var parentChannel = serverChannels.getValueOrDefault($"{discordMessage.guildId}_{parentId}");
            var parentCategory = parentChannel?.parentId ?? "NO_CATEGORY";
            
            CPH.TriggerCodeEvent(THREAD_MESSAGE_EVENT + parentId, properties);
            CPH.TriggerCodeEvent(THREAD_MESSAGE_EVENT + parentCategory, properties);
            
            DEBUG(() => $"EXECUTE TRIGGER: {THREAD_MESSAGE_EVENT + parentId}");
            return;
        }
        
        CPH.TriggerCodeEvent(MESSAGE_RECEIVED_EVENT + discordMessage.channelId, properties);
        CPH.TriggerCodeEvent(MESSAGE_RECEIVED_EVENT + parentId, properties);
        
        DEBUG(() => $"EXECUTE TRIGGER: {MESSAGE_RECEIVED_EVENT + discordMessage.channelId}");
    }
    
    private void onMessageReaction(DiscordReaction discordReaction) {
        DEBUG(() => $"EVENT: REACTION ADD {discordReaction.userId}");

        var properties = new Dictionary<string, object> ().addAll(discordReaction.toCollection());
        var channel = serverChannels.getValueOrDefault($"{discordReaction.guildId}_{discordReaction.channelId}");
        var parentId = channel?.parentId ?? "NO_CATEGORY";
        
        if (channel != null && channel.typeName.Contains("THREAD")) {
            var parentChannel = serverChannels.getValueOrDefault($"{discordReaction.guildId}_{parentId}");
            var parentCategory = parentChannel?.parentId ?? "NO_CATEGORY";
            
            CPH.TriggerCodeEvent(MESSAGE_REACTION_ADDED + parentId, properties);
            CPH.TriggerCodeEvent(MESSAGE_REACTION_ADDED + parentCategory, properties);
            
            DEBUG(() => $"EXECUTE TRIGGER: {MESSAGE_REACTION_ADDED + parentId}");
            return;
        }
        
        CPH.TriggerCodeEvent(MESSAGE_REACTION_ADDED + discordReaction.channelId, properties);
        CPH.TriggerCodeEvent(MESSAGE_REACTION_ADDED + parentId, properties);
        
        DEBUG(() => $"EXECUTE TRIGGER: {MESSAGE_REACTION_ADDED + discordReaction.channelId}");
    }
    
    private void onVoiceStateChange(DiscordVoiceState discordVoiceState) {
        DEBUG(() => $"EVENT: VOICE STATE {discordVoiceState.Member.User.userName}");

        var properties = new Dictionary<string, object> ().addAll(discordVoiceState.toCollection());
        var channel = serverChannels.getValueOrDefault($"{discordVoiceState.guildId}_{discordVoiceState.channelId}");
        var parentId = channel?.parentId ?? "NO_CATEGORY";
        
        CPH.TriggerCodeEvent(discordVoiceState.voiceStateEvent + channel?.id, properties);
        CPH.TriggerCodeEvent(VOICE_GENERIC_EVENT + channel?.id, properties);
        if (channel != null) {
            CPH.TriggerCodeEvent(discordVoiceState.voiceStateEvent + parentId, properties);
            CPH.TriggerCodeEvent(VOICE_GENERIC_EVENT + parentId, properties);
        }
        DEBUG(() => $"TRIGGERED EVENT: {VOICE_GENERIC_EVENT + channel?.id}");
    }
    
    private void onGuildMember(string eventName, DiscordMember discordMember) {
        DEBUG(() => $"EVENT: {eventName} {discordMember.User.userName}");
        var properties = new Dictionary<string, object> ().addAll(discordMember.toCollection());
        CPH.TriggerCodeEvent(eventName + discordMember.guildId, properties);
        DEBUG(() => $"EXECUTE TRIGGER: {eventName + discordMember.guildId}");
    }
    
    //----------------------------------------------------------------
    // HELPER UTILITY METHODS
    //----------------------------------------------------------------
    private T deserializeEntity<T>(string jsonData) where T: class {
        DEBUG(() => $"GATEWAY: JSON DATA OBJECT {jsonData}");
        try {
            var dataObject = JsonConvert.DeserializeObject<T>(jsonData);
            if (dataObject == null) {
                throw new Exception($"{typeof(T).Name.ToUpper()} IS NULL");
            }
            
            var authorProperty = typeof(T).GetProperty("Author");
            var memberProperty = typeof(T).GetProperty("Member");
            if (authorProperty != null) {
                var author = (DiscordUser) authorProperty.GetValue(dataObject);
                if (author?.isBot == true) {
                    throw new Exception("GATEWAY: Author is BOT account, ignore");
                }
            } else if (memberProperty != null) {
                var member = (DiscordMember) memberProperty.GetValue(dataObject);
                if (member?.User is { isBot: true }) {
                    throw new Exception("GATEWAY: Member is BOT account, ignore");
                }
            }
            
            // set C# Discord Api instance into Entity for API calls
            var cdaProperty = typeof(T).GetProperty("cda");
            // ReSharper disable once InvertIf
            if (cdaProperty != null) {
                var setCdaMethod = typeof(T).GetMethod("set_cda");
                if (setCdaMethod != null) {
                    setCdaMethod.Invoke(dataObject, [this]);
                }
            }
            
            return dataObject;
        } catch (Exception e) {
            throw new Exception($"Cannot read: {typeof(T).Name.ToUpper()} because {e.Message}");
        }
    }
        
    //----------------------------------------------------------------
    // DISCORD UPDATE LISTENER
    //----------------------------------------------------------------
    private const int GUILDS = 1 << 0; // 1 - events related to guilds (servers).
        // GUILD_CREATE      - guild is created.
        // GUILD_UPDATE      - guild is updated.
        // GUILD_DELETE      - guild is deleted.
        // GUILD_ROLE_CREATE - guild role is created.
        // GUILD_ROLE_UPDATE - guild role is updated.
        // GUILD_ROLE_DELETE - guild role is deleted.
        // CHANNEL_CREATE    - channel is created.
        // CHANNEL_UPDATE    - channel is updated.
        // CHANNEL_DELETE    - channel is deleted.
        // CHANNEL_PINS_UPDATE - when a channel's pins are updated.
        // THREAD_CREATE     - thread is created.
        // THREAD_UPDATE     - thread is updated.
        // THREAD_DELETE     - thread is deleted.
        // THREAD_LIST_SYNC  - current user gains access to a channel.
        // THREAD_MEMBER_UPDATE - thread member is updated.
        // THREAD_MEMBERS_UPDATE * - multiple thread members are updated.
        // STAGE_INSTANCE_CREATE - stage instance is created.
        // STAGE_INSTANCE_UPDATE - stage instance is updated.
        // STAGE_INSTANCE_DELETE - stage instance is deleted.

    private const int GUILD_MEMBERS = 1 << 1; // 2 - events related to guild members.
        // GUILD_MEMBER_ADD    - new member joins a guild.
        // GUILD_MEMBER_UPDATE - guild member is updated.
        // GUILD_MEMBER_REMOVE - member leaves or is removed from a guild.
        // THREAD_MEMBERS_UPDATE * - multiple thread members are updated.

    private const int GUILD_MODERATION = 1 << 2; // 4 - events related to guild bans.
        // GUILD_AUDIT_LOG_ENTRY_CREATE - audit log is created.
        // GUILD_BAN_ADD    - user is banned from a guild.
        // GUILD_BAN_REMOVE - user is unbanned from a guild.

    private const int GUILD_EXPRESSIONS = 1 << 3; // 8 - events related to emojis and stickers.
        // GUILD_EMOJIS_UPDATE   - guild's emojis are updated.
        // GUILD_STICKERS_UPDATE - guild's stickers are updated.
        // GUILD_SOUNDBOARD_SOUND_CREATE
        // GUILD_SOUNDBOARD_SOUND_UPDATE
        // GUILD_SOUNDBOARD_SOUND_DELETE
        // GUILD_SOUNDBOARD_SOUNDS_UPDATE

    private const int GUILD_INTEGRATIONS = 1 << 4; // 16 - events related to integrations.
        // GUILD_INTEGRATIONS_UPDATE - guild's integrations are updated.
        // INTEGRATION_CREATE
        // INTEGRATION_UPDATE
        // INTEGRATION_DELETE

    private const int GUILD_WEBHOOKS = 1 << 5; // 32 - events related to webhooks.
        // WEBHOOKS_UPDATE - guild's webhooks are updated.

    private const int GUILD_INVITES = 1 << 6; // 64 - events related to invites.
        // INVITE_CREATE - new invite is created.
        // INVITE_DELETE - invite is deleted.

    private const int GUILD_VOICE_STATES = 1 << 7; // 128 - events related to voice states.
        // VOICE_CHANNEL_EFFECT_SEND
        // VOICE_STATE_UPDATE - user's voice state is updated.

    private const int GUILD_PRESENCES = 1 << 8; // 256 - events related to presences.
        // PRESENCE_UPDATE - user's presence is updated.

    private const int GUILD_MESSAGES = 1 << 9; // 512 - events related to messages in guilds.

        // MESSAGE_CREATE - message is created.
        // MESSAGE_UPDATE - message is updated.
        // MESSAGE_DELETE - message is deleted.
        // MESSAGE_DELETE_BULK - multiple messages are deleted at once.

    private const int GUILD_MESSAGE_REACTIONS = 1 << 10; // 1024 - events related to message reactions in guilds.
        // MESSAGE_REACTION_ADD        - reaction is added to a message.
        // MESSAGE_REACTION_REMOVE     - reaction is removed from a message.
        // MESSAGE_REACTION_REMOVE_ALL - all reactions are removed from a message.
        // MESSAGE_REACTION_REMOVE_EMOJI - all reactions for a specific emoji are removed from a message.

    private const int GUILD_MESSAGE_TYPING = 1 << 11; // 2048 - events related to typing in guilds.
        // TYPING_START - user starts typing in a channel.

    private const int DIRECT_MESSAGES = 1 << 12; // 4096 - events related to direct messages.
        // MESSAGE_CREATE - direct message is created.
        // MESSAGE_UPDATE - direct message is updated.
        // MESSAGE_DELETE - direct message is deleted.
        // CHANNEL_PINS_UPDATE

    private const int DIRECT_MESSAGE_REACTIONS = 1 << 13; // 8192 - events related to direct message reactions.
        // MESSAGE_REACTION_ADD        - reaction is added to a direct message.
        // MESSAGE_REACTION_REMOVE     - reaction is removed from a direct message.
        // MESSAGE_REACTION_REMOVE_ALL - reactions are removed from a direct message.
        // MESSAGE_REACTION_REMOVE_EMOJI - all reactions for a specific emoji are removed from a direct message.

    private const int DIRECT_MESSAGE_TYPING = 1 << 14; // 16384 - events related to typing in direct messages.
        // TYPING_START                - user starts typing in a direct message.

    private const int MESSAGE_CONTENT = 1 << 15; // 32768 - required to receive the content of messages.

    private const int GUILD_SCHEDULED_EVENTS = 1 << 16; // 65536 - events related to scheduled events in guilds.
        // GUILD_SCHEDULED_EVENT_CREATE - scheduled event is created.
        // GUILD_SCHEDULED_EVENT_UPDATE - scheduled event is updated.
        // GUILD_SCHEDULED_EVENT_DELETE - scheduled event is deleted.
        // GUILD_SCHEDULED_EVENT_USER_ADD - user subscribes to a scheduled event.
        // GUILD_SCHEDULED_EVENT_USER_REMOVE - unsubscribes from a scheduled event.

    private const int AUTO_MODERATION_CONFIGURATION = 1 << 20; // 1048576 - events related to auto-moderation configuration.
        // AUTO_MODERATION_RULE_CREATE - auto-moderation rule is created.
        // AUTO_MODERATION_RULE_UPDATE - auto-moderation rule is updated.
        // AUTO_MODERATION_RULE_DELETE - auto-moderation rule is deleted.

    private const int AUTO_MODERATION_EXECUTION = 1 << 21; // 2097152 - events related to auto-moderation execution.
        // AUTO_MODERATION_ACTION_EXECUTION - auto-moderation action is executed.

    private const int GUILD_MESSAGE_POLLS  = 1 << 24; // 16777216
        // MESSAGE_POLL_VOTE_ADD
        // MESSAGE_POLL_VOTE_REMOVE
  
    private const int DIRECT_MESSAGE_POLLS = 1 << 25; // 33554432
        // MESSAGE_POLL_VOTE_ADD
        // MESSAGE_POLL_VOTE_REMOVE

    
    private async Task connectToGateway() {
        while (true) {
            try {
                await connectToDiscordGateway();
            } catch (Exception e) {
                INFO(() => $"GATEWAY: Connection is interrupted: {e.Message}");
                await Task.Delay(10000);
            }
        }
    }
    
    private async Task connectToDiscordGateway() {
        if(webSocket is { State: WebSocketState.Open }) {
            DEBUG(() => "GATEWAY: Closing connection on init");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            webSocket.Dispose();
        }
        
        webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(new Uri(DISCORD_GATEWAY), CancellationToken.None);
        DEBUG(() => "GATEWAY: Connected to Discord GATEWAY");
        var identifyPayload = new {
            op = 2,
            d = new {
                token = discordToken,
                intents = GUILD_MESSAGES | MESSAGE_CONTENT | GUILD_MESSAGE_REACTIONS | GUILD_VOICE_STATES | GUILD_MEMBERS ,
                properties = new {
                    os = "linux",
                    browser = "custom",
                    device = "custom"
                },
                presence = new {
                    status = "online", // offline, idle, dnd, invisible
                    activities = new[] {
                        new {
                            name = "with code!",
                            type = 0 // 0 = playing, 1 = streaming, 2 = listening, 3 = watching
                        }
                    },
                    afk = false
                }
            }
        };
        await sendPayloadAsync(identifyPayload);
        
        var receiveBuffer = new byte[4096];
        while (webSocket.State == WebSocketState.Open) {
            DEBUG(() => "GATEWAY: waiting for incoming messages while connection opened");
            
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                DEBUG(() => "GATEWAY: DisCONNECTED from Discord GATEWAY");
                throw new Exception("webSocket connection closed");
            }

            var receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
            handleWSMessage(receivedMessage);
        }
    }
    
    private void handleWSMessage(string message) {
        DEBUG(() => $"GATEWAY: Received message - {message}");
        DiscordGatewayMessage gatewayMessage = null;
        try {
            gatewayMessage = JsonConvert.DeserializeObject<DiscordGatewayMessage>(message);
        } catch (Exception e) {
            DEBUG(() => $"GATEWAY: Exception - {e.Message}");
        }
        
        if (gatewayMessage == null) {
            DEBUG(() => "GATEWAY: gatewayMessage is null, returning");
            return;
        }
        
        DEBUG(() => $"GATEWAY: Gateway Message OP - {gatewayMessage.Op}");
        switch (gatewayMessage.Op) {
            case 10: { // handle Hello message and start heartbeat
                DEBUG(() => "GATEWAY: received HeartBeat message, initializing heartBeat");
                var gatewayData = JsonConvert.DeserializeObject<DiscordGatewayData>(gatewayMessage.D);
                startHeartbeat(gatewayData.heartbeatInterval);
                return;
            }
            case 11: { // handle Heartbeat ACK
                DEBUG(() => "GATEWAY: Heartbeat ACK received");
                return;
            }
            case 0 when gatewayMessage.D != null: { // Event Received
                onEventReceived(gatewayMessage);
                return;
            }
        }
    }
    
    //----------------------------------------------------------------
    // DISCORD API CALLS
    //----------------------------------------------------------------
    private ClientWebSocket webSocket;
    private Timer heartbeatTimer;
    
    private async Task updateBotStatusAsync(string status, string activityName, int activityType) {
        DEBUG(() => $"GATEWAY: Sending new Bot Status: {status}, {activityName}, {activityType}");
        var presencePayload = new {
            op = 3,
            d = new {
                since = (long?) null,
                activities = new[] {
                    new {
                        name = activityName,
                        type = activityType // 0 = playing, 1 = streaming, 2 = listening, 3 = watching
                    }
                },
                status = status, // online, idle, dnd, invisible
                afk = false
            }
        };
        await sendPayloadAsync(presencePayload);
    }
    
    private void startHeartbeat(int heartbeatInterval) {
        DEBUG(() => $"GATEWAY: Start HEARTBEAT with interval: {heartbeatInterval}ms");
        heartbeatTimer = new Timer(
            async _ => await sendPayloadAsync(new {
                op = 1,
                d = (int?)null
            }), 
            null, 
            0, 
            heartbeatInterval
        );
    }
    
    private async Task sendPayloadAsync(object payload) {
        if (webSocket == null) {
            DEBUG(() => "GATEWAY: WebSocket is not created");
            return;
        }
        
        if (webSocket.State != WebSocketState.Open) {
            DEBUG(() => "GATEWAY: WebSocket connection is not open");
            return;
        }
        
        try {
            var payloadJson = JsonConvert.SerializeObject(payload);
            var bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payloadJson));
            await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
            DEBUG(() => "GATEWAY: Payload been sent");
        } catch (ObjectDisposedException e) {
            DEBUG(() => $"GATEWAY: webSocket is Disposed and cannot be executed: {e.Message}");
        } catch (Exception e) {
            DEBUG(() => $"GATEWAY: webSocket Error: {e.Message}");
        }
    }
    
    private async Task updateGuildUserRole(bool overwrite, string guildId, string userId, string roleId) {
        DEBUG(() => $"SET ROLE {roleId} FOR USER {userId} IN {guildId}");
        
        var url = string.Format(API_MEMBER_URL, guildId, userId);
        List<string> currentRoles = [];
        if (overwrite == false) { // if roles dont needs to be overridden, get current user roles first
            var getRequest = (HttpWebRequest)WebRequest.Create(url);
            getRequest.Method = "GET";
            getRequest.Headers.Add("Authorization", "Bot " + discordToken);
            
            try
            {
                using var response = (HttpWebResponse)await getRequest.GetResponseAsync();
                using var stream = response.GetResponseStream();
                if (stream is null) {
                    return;
                }
                
                var reader = new StreamReader(stream, Encoding.UTF8);
                var memberJson = await reader.ReadToEndAsync();
                currentRoles = JsonConvert.DeserializeObject<DiscordMember>(memberJson).roles;
            } catch (Exception e) {
                DEBUG(() => $"CANNOT GET ROLES: {e.Message}");
                return;
            }
        }
        
        if (!currentRoles.Contains(roleId)) {
            currentRoles.Add(roleId);
        }
        
        object payload;
        if (roleId is null or "") {
            payload = new {
                roles = new string[] { }
            };
        } else {
            payload = new {
                roles = currentRoles
            };
        }
        
        var jsonPayload = JsonConvert.SerializeObject(payload);
        var byteArray = Encoding.UTF8.GetBytes(jsonPayload);
        
        var patchRequest = (HttpWebRequest)WebRequest.Create(
            string.Format(API_MEMBER_URL, guildId, userId)
        );
        patchRequest.Method = "PATCH";
        patchRequest.Headers.Add("Authorization", "Bot " + discordToken);
        patchRequest.ContentType = "application/json";
        patchRequest.ContentLength = byteArray.Length;
        
        using (var dataStream = patchRequest.GetRequestStream()) {
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
        }
        
        try
        {
            using var response = (HttpWebResponse)await patchRequest.GetResponseAsync();
            using var stream = response.GetResponseStream();
            if (stream is null) {
                return;
            }
            
            var reader = new StreamReader(stream, Encoding.UTF8);
            var responseText = await reader.ReadToEndAsync();
            DEBUG(() => $"ROLE ASSIGNED TO USER {userId} IN {guildId}: {responseText}");
        } catch (Exception e) {
            DEBUG(() => $"CANNOT SET ROLE: {e.Message}");
        }
    }
    
    private DiscordChannel getDiscordChannelById(string channelId) {
        DEBUG(() => "GETTING CHANNEL INFO");
        
        var request = (HttpWebRequest)WebRequest.Create(
            string.Format(API_CHANNEL_INFO_URL, channelId)
        );
        request.Method = "GET";
        request.Headers.Add("Authorization", "Bot " + discordToken);

        try
        {
            using var response = (HttpWebResponse) request.GetResponse();
            using var stream = response.GetResponseStream();
            if (stream is null) {
                return null;
            }
            
            var reader = new StreamReader(stream, Encoding.UTF8);
            var responseText = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<DiscordChannel>(responseText);
        } catch (Exception e) {
            DEBUG(() => $"CANNOT GET CHANNEL: {e.Message}");
            return null;
        }
    }
    
    private async Task getDiscordServers() {
        DEBUG(() => "REGISTERING SERVERS LIST");
        
        var request = (HttpWebRequest) WebRequest.Create(API_GUILDS_URL);
        request.Method = "GET";
        request.Headers.Add("Authorization", "Bot " + discordToken);
        
        try {
            using var response = (HttpWebResponse) await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            if (stream is null) {
                return;
            }
            
            var reader = new StreamReader(stream, Encoding.UTF8);
            var responseText = await reader.ReadToEndAsync();
            var channels = JsonConvert.DeserializeObject<List<DiscordChannel>>(responseText);
            foreach (var channel in channels) {
                _ = getDiscordServerChannels(channel.id, channel.name);
            }
        } catch (Exception e) {
            DEBUG(() => $"CANNOT REGISTER SERVERS: {e.Message}");
        }
    }
    
    private async Task getDiscordServerChannels(string guildId, string guildName) {
        DEBUG(() => "_______________________________");
        DEBUG(() => $"REGISTERING CHANNELS LIST FOR: {guildName}");
        
        var request = (HttpWebRequest) WebRequest.Create(
            string.Format(API_CHANNEL_LIST_URL, guildId)
        );
        request.Method = "GET";
        request.Headers.Add("Authorization", "Bot " + discordToken);
        
        try {
            using var response = (HttpWebResponse) await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            if (stream is null) {
                return;
            }
            
            var reader = new StreamReader(stream, Encoding.UTF8);
            var responseText = await reader.ReadToEndAsync();
            var channels = JsonConvert.DeserializeObject<List<DiscordChannel>>(responseText, new JsonSerializerSettings {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                ContractResolver = new DefaultContractResolver(),
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });
                
            DEBUG(() => "LIST OF AVAILABLE CHANNELS");
            var categoryGroups   = new Dictionary<string, string>();
            var categoryChannels = new Dictionary<string, List<DiscordChannel>>();
                    
            if (!serverNames.ContainsKey(guildId)) {
                serverNames[guildId] = guildName;
            }
                    
            foreach (var channel in channels) {
                var categoryId = channel.parentId ?? "NO_CATEGORY";
                if (channel.type != 4) { // is Channel
                    if (!categoryChannels.ContainsKey(categoryId)) {
                        categoryChannels[categoryId] = [];
                    }
                    categoryChannels[categoryId].Add(channel);
                    serverChannels[guildId + "_" + channel.id] = channel;
                } else { // is Category
                    if (!categoryGroups.ContainsKey(channel.id)) {
                        categoryGroups[channel.id] = channel.name;
                    }
                }
            }
                    
            foreach (var category in categoryChannels) {
                var categoryName = category.Key != "NO_CATEGORY" ? categoryGroups[category.Key] : "NO_CATEGORY";
                DEBUG(() => $"CATEGORY: {categoryName}");
                        
                var isTextChannels = false;
                var isVoiceChannels = false;
                var isThreadChannels = false;
                foreach (var channel in category.Value) {
                    string channelType;
                    switch (channel.type) {
                        case 0: // Text Channel
                            isTextChannels = true;
                            isThreadChannels = true;
                            channelType = "[T]";
                            registerEvent(THREAD_MESSAGE_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_RECEIVED_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_REACTION_ADDED, channelType, channel, guildName, categoryName);
                            DEBUG(() => $"TEXT  CHANNEL   : {channel.id} - {channel.name}");
                            break;
                        case 2: // Voice Channel
                            isTextChannels = true;
                            isVoiceChannels = true;
                            channelType = "[V]";
                            registerEvent(MESSAGE_RECEIVED_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_REACTION_ADDED, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_GENERIC_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_JOIN_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_LEFT_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_MOVE_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_UPDATE_EVENT, channelType, channel, guildName, categoryName);
                            DEBUG(() => $"VOICE CHANNEL   : {channel.id} - {channel.name}");
                            break;
                        case 5: // Announcement Channel
                            isTextChannels = true;
                            isThreadChannels = true;
                            channelType = "[A]";
                            registerEvent(THREAD_MESSAGE_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_RECEIVED_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_REACTION_ADDED, channelType, channel, guildName, categoryName);
                            DEBUG(() => $"ANNOUNCE CHANNEL: {channel.id} - {channel.name}");
                            break;
                        case 13: // Stage Channel
                            isTextChannels = true;
                            isVoiceChannels = true;
                            channelType = "[S]";
                            registerEvent(MESSAGE_RECEIVED_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_REACTION_ADDED, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_GENERIC_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_JOIN_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_LEFT_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_MOVE_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(VOICE_UPDATE_EVENT, channelType, channel, guildName, categoryName);
                            DEBUG(() => $"STAGE CHANNEL   : {channel.id} - {channel.name}");
                            break;
                        case 15: // Forum Channel
                            isThreadChannels = true;
                            channelType = "[F]";
                            registerEvent(THREAD_MESSAGE_EVENT, channelType, channel, guildName, categoryName);
                            registerEvent(MESSAGE_REACTION_ADDED, channelType, channel, guildName, categoryName);
                            DEBUG(() => $"FORUM CHANNEL   : {channel.id} - {channel.name}");
                            break;
                        default:
                            DEBUG(() => $"UNKNOWN CHANNEL : {channel.id} - {channel.name}");
                            break;
                    }
                }
                        
                if (isTextChannels) {
                    registerCategoryEvent(MESSAGE_RECEIVED_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(MESSAGE_REACTION_ADDED, guildName, category.Key, categoryName);
                }
                if (isThreadChannels) {
                    registerCategoryEvent(THREAD_MESSAGE_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(MESSAGE_REACTION_ADDED, guildName, category.Key, categoryName);
                }
                if (isVoiceChannels) {
                    registerCategoryEvent(MESSAGE_REACTION_ADDED, guildName, category.Key, categoryName);
                    registerCategoryEvent(VOICE_GENERIC_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(VOICE_JOIN_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(VOICE_LEFT_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(VOICE_MOVE_EVENT, guildName, category.Key, categoryName);
                    registerCategoryEvent(VOICE_UPDATE_EVENT, guildName, category.Key, categoryName);
                }
                DEBUG(() => $"{categoryName}: REGISTERED");
            }
                    
            registerServerEvent(MEMBER_JOIN, guildId, guildName);
            registerServerEvent(MEMBER_LEFT, guildId, guildName);
            registerServerEvent(MEMBER_UPDATE, guildId, guildName);
        } catch (Exception e) {
            DEBUG(() => $"CANNOT REGISTER CHANNELS: {e.Message}");
        }
    }
    
    private void registerEvent(
        string eventName, 
        string channelType, 
        DiscordChannel channel, 
        string guildName,
        string categoryName
    ) {
        CPH.RegisterCustomTrigger(eventName + ": " + channel.name, eventName + channel.id, new [] { 
            "Discord",
            "Server: " + guildName,
            "Category: " + categoryName,
            channelType + " Channel: " + channel.name
        });
        DEBUG(() => $"REGISTERED EVENT: {eventName + channel.id}");
    }
    
    private void registerCategoryEvent(
        string eventName, 
        string guildName, 
        string categoryId,
        string categoryName
    ) {
        CPH.RegisterCustomTrigger(eventName + ": " + categoryName, eventName + categoryId, new [] { 
            "Discord",
            "Server: " + guildName,
            "Category: " + categoryName,
            "GENERIC: ANY",
        });
        DEBUG(() => $"REGISTERED CATEGORY EVENT: {eventName + categoryId}");
    }
    
    private void registerServerEvent(
        string eventName,
        string guildId,
        string guildName
    ) {
        CPH.RegisterCustomTrigger(eventName + ": " + guildName, eventName + guildId, new [] {
            "Discord",
            "Server: " + guildName,
            "Guild: EVENT"
        });
        DEBUG(() => $"REGISTERED SERVER EVENT: {eventName + guildId}");
    }
    
    private void destroy() {
        if (webSocket is { State: WebSocketState.Open or WebSocketState.CloseReceived }) {
            try {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close connection on destroy", CancellationToken.None);
            } finally {
                webSocket.Dispose();
                webSocket = null;
            }
        }

        heartbeatTimer?.Dispose();
    }
    
    //----------------------------------------------------------------
    // DISCORD ENTITY'S
    //----------------------------------------------------------------
    [JsonConverter(typeof(GatewayMessageConverter))]
    public class DiscordGatewayMessage {
        [JsonProperty("op")] public int Op { get; set; }
        [JsonProperty("t")] public string T { get; set; }
        [JsonProperty("d")] public string D { get; set; }
    }
    
    public class DiscordGatewayData {
        [JsonProperty("heartbeat_interval")] public int heartbeatInterval { get; set; }
    }
    
    public class DiscordChannel {
        [JsonProperty("id")] public string id { get; set; }
        [JsonProperty("name")] public string name { get; set; }
        [JsonProperty("type")] public int type { get; set; }
        [JsonProperty("parent_id")] public string parentId { get; set; }
        
        [JsonIgnore] public string typeName => type switch {
            0  => "TEXT",
            2  => "VOICE",
            5  => "ANNOUNCE",
            10 => "THREAD PUBLIC",
            11 => "THREAD PRIVATE",
            12 => "STAGE CHANNEL",
            13 => "STAGE INSTANCE",
            15 => "FORUM",
            _ => "TEXT",
        };
        
        public Dictionary<string, object> toCollection() {
            var collection = new Dictionary<string, object> {
                { "discord.channel.json", includeJsons ? JsonConvert.SerializeObject(this) : "not_enabled" },
                { "discord.channel.id", id },
                { "discord.channel.name", name },
                { "discord.channel.type", typeName },
                { "discord.channel.parent", parentId ?? "NO_CATEGORY"}
            };
            
            return collection;
        }
    }
    
    public class DiscordMessage {
        [JsonProperty("guild_id")] public string guildId { get; set; }
        [JsonProperty("channel_id")] public string channelId { get; set; }
        
        [JsonProperty("id")] public string id { get; set; }
        [JsonProperty("content")] public string content { get; set; }
        [JsonProperty("timestamp")] public string timestamp { get; set; }
        
        [JsonProperty("author")] public DiscordUser Author { get; set; }
        [JsonProperty("member")] public DiscordMember Member { get; set; }
        
        [JsonIgnore] public CPHInline cda { get; set; }
        public Dictionary<string, object> toCollection() {
            var serverName = serverNames.getValueOrDefault(guildId, "UNKNOWN");
            var serverCollection = new Dictionary<string, object> {
                { "discord.server.id"  , guildId },
                { "discord.server.name", serverName }
            };
            
            var channel = serverChannels.getValueOrDefault($"{guildId}_{channelId}");
            if (channel == null) {
                channel = cda.getDiscordChannelById(channelId);
                serverChannels[$"{guildId}_{channelId}"] = channel;
            }
            
            var messageCollection = new Dictionary<string, object> {
                { "discord.message.json", includeJsons ? JsonConvert.SerializeObject(this) : "not_enabled" },
                { "discord.message.id", id },
                { "discord.message.content", content },
                { "discord.message.timestamp", timestamp }
            };
            
            var collection = new Dictionary<string, object> ();
            collection.addAll(serverCollection);
            collection.addAll(channel?.toCollection());
            collection.addAll(messageCollection);
            collection.addAll(Author?.toCollection("author"));
            collection.addAll(Member?.toCollection());
            
            return collection;
        }
    }
    
    public class DiscordVoiceState {
        [JsonProperty("guild_id")] public string guildId { get; set; }
        [JsonProperty("channel_id")] public string channelId { get; set; }
        
        [JsonProperty("user_id")] public string userId { get; set; }
        [JsonProperty("self_mute")] public bool selfMute { get; set; }
        [JsonProperty("self_deaf")] public bool selfDeaf { get; set; }
        [JsonProperty("self_video")] public bool selfVideo { get; set; }
        [JsonProperty("self_stream")] public bool selfStream { get; set; }
        [JsonProperty("mute")] public bool mute { get; set; }
        [JsonProperty("deaf")] public bool deaf { get; set; }
        
        [JsonProperty("member")] public DiscordMember Member { get; set; }
        
        [JsonIgnore] public string voiceStateEvent => getEventName();
        [JsonIgnore] private string voiceStateCache;
        [JsonIgnore] public CPHInline cda { get; set; }
        public Dictionary<string, object> toCollection() {
            var serverName = serverNames.getValueOrDefault(guildId, "UNKNOWN");
            var serverCollection = new Dictionary<string, object> {
                { "discord.server.id"  , guildId },
                { "discord.server.name", serverName }
            };
        
            var voiceCollection = new Dictionary<string, object> {
                { "discord.voice.state"      , voiceStateEvent },
                { "discord.voice.self_mute"  , selfMute },
                { "discord.voice.self_deaf"  , selfDeaf },
                { "discord.voice.self_video" , selfVideo },
                { "discord.voice.self_stream", selfStream },
                { "discord.voice.mute"       , mute },
                { "discord.voice.deaf"       , deaf }
            };
            
            DiscordChannel channel = null;
            if (channelId != null) {
                channel = serverChannels.getValueOrDefault($"{guildId}_{channelId}");
                if (channel == null) {
                    channel = cda.getDiscordChannelById(channelId);
                    serverChannels[$"{guildId}_{channelId}"] = channel;
                }
            }
            
            var collection = new Dictionary<string, object> ();
            collection.addAll(serverCollection);
            collection.addAll(voiceCollection);
            collection.addAll(channel?.toCollection());
            collection.addAll(Member?.toCollection());
            
            return collection;
        }
        
        private string getEventName() {
            if (voiceStateCache != null) {
                return voiceStateCache;
            }
            
            if (channelId == null) {
                if (userToVChannel.TryGetValue(userId, out var value)) {
                    channelId = value;
                    voiceStateCache = VOICE_LEFT_EVENT;
                } else { // user left channel but he was joined before SBot started (channel is empty)
                    voiceStateCache = VOICE_LEFT_EVENT + "UNKNOWN";
                }
                userToVChannel.Remove(userId);
            } else {
                if (userToVChannel.TryGetValue(userId, out var savedChannelId)) {
                    voiceStateCache = channelId.Equals(savedChannelId) ? VOICE_UPDATE_EVENT : VOICE_MOVE_EVENT;
                } else {
                    voiceStateCache = VOICE_JOIN_EVENT;
                }
                userToVChannel[userId] = channelId;
            }
            
            return voiceStateCache;
        }
    }
    
    public class DiscordReaction {
        [JsonProperty("guild_id")] public string guildId { get; set; }
        [JsonProperty("channel_id")] public string channelId { get; set; }
            
        [JsonProperty("user_id")] public string userId { get; set; }
        [JsonProperty("message_id")] public string messageId { get; set; }
            
        [JsonProperty("emoji")] public DiscordEmoji Emoji { get; set; }
        [JsonProperty("member")] public DiscordMember Member { get; set; }
            
        [JsonIgnore] public CPHInline cda { get; set; }
        public Dictionary<string, object> toCollection() {
            var serverName = serverNames.getValueOrDefault(guildId, "UNKNOWN");
            var serverCollection = new Dictionary<string, object> {
                { "discord.server.id"  , guildId },
                { "discord.server.name", serverName }
            };
                
            var channel = serverChannels.getValueOrDefault($"{guildId}_{channelId}");
            if (channel == null) {
                channel = cda.getDiscordChannelById(channelId);
                serverChannels[$"{guildId}_{channelId}"] = channel;
            }
                
            var reactionCollection = new Dictionary<string, object> {
                { "discord.message.id", messageId }
            };
                
            var collection = new Dictionary<string, object> ();
            collection.addAll(serverCollection);
            collection.addAll(channel?.toCollection());
            collection.addAll(reactionCollection);
            collection.addAll(Emoji?.toCollection());
            collection.addAll(Member?.toCollection());
                
            return collection;
        }
    }

    public class DiscordMember {
        [JsonProperty("guild_id")] public string guildId { get; set; }
            
        [JsonProperty("nick")] public string nickName { get; set; }
        [JsonProperty("roles")] public List<string> roles { get; set; }
        [JsonProperty("joined_at")] public string joinedAt { get; set; }
        [JsonProperty("premium_since")] public string premiumSince { get; set; }
            
        [JsonProperty("user")] public DiscordUser User { get; set; }
            
        [JsonIgnore] private bool isPremium => !string.IsNullOrEmpty(premiumSince);
        public Dictionary<string, object> toCollection() {
            var serverCollection = new Dictionary<string, object> ();
            if (guildId != null) {
                var serverName = serverNames.getValueOrDefault(guildId, "UNKNOWN");
                serverCollection.Add("discord.server.id", guildId);
                serverCollection.Add("discord.server.name", serverName);
            }
            
            var memberCollection = new Dictionary<string, object> {
                { "discord.member.json", includeJsons ? JsonConvert.SerializeObject(this) : "not_enabled" },
                { "discord.member.nickName", nickName },
                { "discord.member.roles", String.Join(", ", roles) },
                { "discord.member.joined", joinedAt },
                { "discord.member.isBoosting", isPremium },
                { "discord.member.boostingSince", premiumSince }
            };
            
            var collection = new Dictionary<string, object> ();
            collection.addAll(serverCollection);
            collection.addAll(memberCollection);
            collection.addAll(User?.toCollection("user"));
            
            return collection;
        }
    }

    public class DiscordEmoji {
        [JsonProperty("id")] public string id { get; set; }
        [JsonProperty("name")] public string name { get; set; }
        [JsonProperty("managed")] public bool managed { get; set; }
        [JsonProperty("animated")] public bool animated { get; set; }
        [JsonProperty("available")] public bool available { get; set; }
        [JsonProperty("require_colons")] public bool requireColons { get; set; }
            
        [JsonProperty("roles")] public List<string> roles { get; set; }
        [JsonProperty("user")] public DiscordUser User { get; set; }
            
        public Dictionary<string, object> toCollection() {
            var emojiCollection = new Dictionary<string, object> {
                { "discord.emoji.json", includeJsons ? JsonConvert.SerializeObject(this) : "not_enabled" },
                { "discord.emoji.id", id },
                { "discord.emoji.name", name },
                { "discord.emoji.managed", managed },
                { "discord.emoji.animated", animated },
                { "discord.emoji.available", available }
            };
                
            var collection = new Dictionary<string, object> ();
            collection.addAll(emojiCollection);
            collection.addAll(User?.toCollection("author"));
                    
            return collection;
        }
    }

    public class DiscordUser {
        [JsonProperty("id")] public string id { get; set; }
        [JsonProperty("username")] public string userName { get; set; }
        [JsonProperty("discriminator")] public string discriminator { get; set; }
        [JsonProperty("avatar")] public string avatar { get; set; }
        [JsonProperty("premium_type")] public int nitroType { get; set; }
        [JsonProperty("bot")] public bool isBot { get; set; }
            
        [JsonIgnore] private string avatarUrl => !string.IsNullOrEmpty(avatar) ? 
            $"https://cdn.discordapp.com/avatars/{id}/{avatar}.png" : 
            "https://cdn.discordapp.com/embed/avatars/0.png";
                 
        [JsonIgnore] private string nitroName => nitroType switch {
            0 => "NONE",
            1 => "Nitro Classic",
            2 => "Nitro",
            3 => "Nitro Basic",
            _ => "UNKNOWN"
        };
            
        public Dictionary<string, object> toCollection(string attributeName) {
            var collection = new Dictionary<string, object> {
                { $"discord.{attributeName}.json", includeJsons ? JsonConvert.SerializeObject(this) : "not_enabled" },
                { $"discord.{attributeName}.id", id },
                { $"discord.{attributeName}.userName", userName },
                { $"discord.{attributeName}.discriminator", discriminator },
                { $"discord.{attributeName}.avatar", avatar },
                { $"discord.{attributeName}.avatarUrl", avatarUrl },
                { $"discord.{attributeName}.nitro", nitroName },
                { $"discord.{attributeName}.isBot", isBot }
            };
                
            return collection;
        }
    }
    
    private class GatewayMessageConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(DiscordGatewayMessage);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var jsonObject = JObject.Load(reader);
            var message = new DiscordGatewayMessage();
    
            foreach (var property in jsonObject.Properties()) {
                switch (property.Name) {
                    case "op":
                        int.TryParse(property.Value.ToString(), out var result);
                        message.Op = result;
                        break;
                    case "t":
                        message.T = property.Value.ToString();
                        break;
                    case "d":
                        message.D = property.Value.ToString();
                        break;
                }
            }

            return message;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var message = (DiscordGatewayMessage)value;
            var jsonObject = new JObject {
                ["op"] = message?.Op,
                ["t"] = message?.T,
                ["d"] = message?.D
            };

            jsonObject.WriteTo(writer);
        }
    }
    
    //----------------------------------------------------------------
    // DEFAULT METHODS AND SETUP
    //----------------------------------------------------------------
    private const bool isDebugEnabled = false;
    private bool isInitialized;
    private string widgetActionName = "TEMPLATE";
    
    // ReSharper disable once UnusedMember.Global
    public bool Execute() {
        setUp();
        return true;
    }
    
    // ReSharper disable once UnusedMember.Global
    public void Dispose() {
        destroy();
    }
    
    // ReSharper disable once UnusedMember.Local
    private void sendMessage(string message, string replyId = null) {
        if (replyId is null or "NONE" or "UNKNOWN" or "") {
            CPH.SendMessage(message, true);
            return;
        }
        
        CPH.TwitchReplyToMessage(message, replyId, true);
    }
    
    private void setUp() {
        if (isInitialized) {
            return;
        }
        widgetActionName = getProperty("actionName", "TEMPLATE");

        INFO(() => "INITIAL SETUP");
        init();

        isInitialized = true;
    }
    
    private T getProperty<T>(string key, T defaultValue) {
        var result = CPH.TryGetArg(key, out T value);
        DEBUG(() => "{key: " + key + ", value: " + value + ", default: " + defaultValue + "}");

        return result ? 
            !value.Equals("") ? 
                value 
                : defaultValue 
            : defaultValue;
    }

    private void DEBUG(Func<string> getMessage) {
        if (!isDebugEnabled) {
            return;
        }

        CPH.LogInfo("DEBUG: " + widgetActionName + " :: " + getMessage());
    }
    
    private void INFO(Func<string> getMessage) {
        CPH.LogInfo("INFO : " + widgetActionName + " :: " + getMessage());
    }

    private void WARN(Func<string> getMessage) {
        CPH.LogWarn("WARN : " + widgetActionName + " :: " + getMessage());
    }
    
    private void ERROR(Func<string> getMessage) {
        CPH.LogError("ERROR: " + widgetActionName + " :: " + getMessage());
    }
}

// EXTENSIONS
public static class DictionaryExtensions {
    public static TValue getValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default) {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
    
    public static Dictionary<TKey, TValue> addAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> source) {
        if (source == null) {
            return dictionary;
        }
        
        foreach (var pair in source) { //KeyValuePair<TKey, TValue>
            dictionary[pair.Key] = pair.Value;
        }
        source.Clear();
        
        return dictionary;
    }
}