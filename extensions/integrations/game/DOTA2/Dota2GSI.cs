// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

//#nullable enable
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/**
 * DOTA 2 GAME STATE INTEGRATION
 */
// ReSharper disable once UnusedType.Global
// public class CPHInline {
public class CPHInline_DOTA2GSI : CPHInlineBase {
    private static bool isRunning = true;
    private static HttpListener listener;
    
    private string host;
    private string port;
    private string uri;
    
    private async Task init() {
        host = getProperty("dota2.gsi.host", "+"); // "+" means connections from all hosts /localhost/127.0.0.1/ip
        port = getProperty("dota2.gsi.port", "3000"); //gsi = Game State Integration
        uri = getProperty("dota2.gsi.uri", "");
        
        foreach (var eventDetails in dota.GameEvent.GAME_EVENTS) {
            CPH.RegisterCustomTrigger(eventDetails.name, eventDetails.id, eventDetails.path);
        }
        _ = startDota2EventListener();
    }

    private void handleGameEvent(dota.GameEvent gameEvent) {
        DEBUG(() => "GAME EVENT RECEIVED: " + gameEvent.provider.name);
        
        gameEvent.provider?.handleChanges((eventDetails, changes) => {
            DEBUG(() => $"PROVIDER CHANGES: {eventDetails.name} {JsonConvert.SerializeObject(changes, Extensions.serializeSettings)}");
            
            var properties = new Dictionary<string, object>();
            foreach (var pair in changes) {
                properties[$"provider.{pair.Key}.new"] = pair.Value.newValue?.ToString() ?? "";
                properties[$"provider.{pair.Key}.old"] = pair.Value.oldValue?.ToString() ?? "";
            }
            properties.addAllFrom(gameEvent.getProviderProperties());
            CPH.TriggerCodeEvent(eventDetails.id, properties);
        });
        gameEvent.map?.handleChanges((eventDetails, changes) => {
            DEBUG(() => $"MAP CHANGES: {eventDetails.name} {JsonConvert.SerializeObject(changes, Extensions.serializeSettings)}");
            
            var properties = new Dictionary<string, object>();
            foreach (var pair in changes) {
                properties[$"map.{pair.Key}.new"] = pair.Value.newValue?.ToString() ?? "";
                properties[$"map.{pair.Key}.old"] = pair.Value.oldValue?.ToString() ?? "";
            }
            properties.addAllFrom(gameEvent.getMapProperties());
            CPH.TriggerCodeEvent(eventDetails.id, properties);
        });
        gameEvent.player?.handleChanges((eventDetails, changes) => {
            DEBUG(() => $"PLAYER CHANGES: {eventDetails.name} {JsonConvert.SerializeObject(changes, Extensions.serializeSettings)}");
            
            var properties = new Dictionary<string, object>();
            foreach (var pair in changes) {
                properties[$"player.{pair.Key}.new"] = pair.Value.newValue?.ToString() ?? "";
                properties[$"player.{pair.Key}.old"] = pair.Value.oldValue?.ToString() ?? "";
            }
            properties.addAllFrom(gameEvent.getPlayerProperties());
            CPH.TriggerCodeEvent(eventDetails.id, properties);
        });
        gameEvent.hero?.handleChanges((eventDetails, changes) => {
            DEBUG(() => $"HERO CHANGES: {eventDetails.name} {JsonConvert.SerializeObject(changes, Extensions.serializeSettings)}");
            
            var properties = new Dictionary<string, object>();
            foreach (var pair in changes) {
                properties[$"hero.{pair.Key}.new"] = pair.Value.newValue?.ToString() ?? "";
                properties[$"hero.{pair.Key}.old"] = pair.Value.oldValue?.ToString() ?? "";
            }
            properties.addAllFrom(gameEvent.getHeroProperties());
            CPH.TriggerCodeEvent(eventDetails.id, properties);
        });
    }
    
    private static readonly byte[] responseMessage = Encoding.UTF8.GetBytes("{\"message\": \"ok\"}");
    private async Task startDota2EventListener() {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://{host}:{port}/{uri}");
        listener.Start();
        INFO(() => $"listener started on: http://{host}:{port}/{uri}");
        
        try {
            while (isRunning) {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                DEBUG(() => $"request info: {request.HttpMethod} -> {request.Url}");
                
                if (request.HttpMethod == "POST") {
                    var jsonData = string.Empty;
                    try {
                        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                        jsonData = await reader.ReadToEndAsync();
                        handleGameEvent(
                            deserializeEntity<dota.GameEvent>(jsonData)
                                .initializePrevious()
                        );
                    } catch (Exception e) {
                        ERROR(() => $"cannot handle json event: {e.Message}, " +
                                      $"reason: {e.InnerException?.Message}, " +
                                      $"stack: {e.StackTrace}, " +
                                      $"originalJson: {jsonData}"
                        );
                    }
                }
                
                response.ContentLength64 = responseMessage.Length;
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(responseMessage, 0, responseMessage.Length);
                response.OutputStream.Close();
            }
        } catch(HttpListenerException e) {
            DEBUG(() => "Listener been failed: " + e.ErrorCode switch {
                995 => "gracefull shutdown",
                _ => $"unexpected error with code {e.ErrorCode}, {e.Message}"
            });
        } catch(Exception e) {
            DEBUG(() => "Listener throw exception: " + e.Message);
        } finally {
            INFO(() => $"listener is stopped");
            listener.Close();
        }
    }
    
    //----------------------------------------------------------------
    // DOTA 2 UTILITY METHODS
    //----------------------------------------------------------------
    
    private T deserializeEntity<T>(string jsonData) where T: class {
        DEBUG(() => $"JSON DATA OBJECT {jsonData}");
        try {
            var dataObject = JsonConvert.DeserializeObject<T>(jsonData, Extensions.deserializeSettings);
            if (dataObject == null) {
                throw new Exception($"{typeof(T).Name.ToUpper()} IS NULL");
            }
            
            return dataObject;
        } catch (Exception e) {
            throw new Exception($"Cannot read: {typeof(T).Name.ToUpper()} because {e.Message}");
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
        _ = init();
        
        isInitialized = true;
    }
    
    // ReSharper disable once UnusedMember.Global
    public void Dispose() {
    }
    
    private T getProperty<T>(string key, T defaultValue) {
        var result = CPH.TryGetArg(key, out T value);
        DEBUG(() => "{key: " + key + ", value: " + value + ", default: " + defaultValue + "}");

        return result ? 
            value != null && !value.Equals("") ? 
                value : defaultValue 
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

internal static class Extensions {
    public static readonly JsonSerializerSettings deserializeSettings = new() {
        MissingMemberHandling = MissingMemberHandling.Ignore
    };
    public static readonly JsonSerializerSettings serializeSettings = new() {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };
    
    public static void AddIfNotEmpty(this Dictionary<string, object> dictionary, string key, object value) {
        switch (value) {
            case null:
                return;
            case var _ when value.GetType().IsPrimitive:
                dictionary[key] = value;
                return;
            default:
                dictionary[key] = value.ToString();
                break;
        }
    }
    
    public static void addAllFrom<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        Dictionary<TKey, TValue> source,
        bool clearSource = true
    ) {
        if (source == null || source.Count == 0) {
            return;
        }
        
        foreach (var pair in source) { //KeyValuePair<TKey, TValue>
            dictionary[pair.Key] = pair.Value;
        }

        if (clearSource) {
            source.Clear();
        }
    }
}

namespace dota {
    public class EventDetails(string name, string id, string[] path) {
        public string name { get; } = name;
        public string id { get; } = id;
        public string[] path { get; } = path;
    }
    
    public class GameEvent {
        public static readonly EventDetails[] GAME_EVENTS = [
            ..Provider.PROVIDER_EVENT_LIST,
            ..Map.MAP_EVENT_LIST,
            ..Player.PLAYER_EVENT_LIST,
            ..Hero.HERO_EVENT_LIST
        ];
        
        [JsonProperty("provider")]  [JsonConverter(typeof(DotaEntityConverter<Provider>))]  public Provider provider { get; set; }
        [JsonProperty("map")]       [JsonConverter(typeof(DotaEntityConverter<Map>))]       public Map map { get; set; }
        [JsonProperty("player")]    [JsonConverter(typeof(DotaEntityConverter<Player>))]    public Player player { get; set; }
        [JsonProperty("hero")]      [JsonConverter(typeof(DotaEntityConverter<Hero>))]      public Hero hero { get; set; }
        [JsonProperty("abilities")] [JsonConverter(typeof(DotaEntityConverter<Abilities>))] public Abilities abilities { get; set; }
        [JsonProperty("items")]     [JsonConverter(typeof(DotaEntityConverter<Items>))]     public Items items { get; set; }
        [JsonProperty("buildings")] [JsonConverter(typeof(DotaEntityConverter<Buildings>))] public Buildings buildings { get; set; }
        [JsonProperty("draft")]     [JsonConverter(typeof(DotaEntityConverter<Draft>))]     public Draft draft { get; set; }
        [JsonProperty("previously")] public GameEvent previousEvent { get; set; }
    
        public GameEvent initializePrevious() {
            provider?.initializePrevious(previousEvent?.provider);
            map?.initializePrevious(previousEvent?.map);
            player?.initializePrevious(previousEvent?.player);
            hero?.initializePrevious(previousEvent?.hero);
            return this;
        }
        
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
        
        public Dictionary<string, object> getProperties() {
            var properties = new Dictionary<string, object> {
                { "dota.event", ToString() }
            };
            properties.addAllFrom(getProviderProperties());
            properties.addAllFrom(getMapProperties());
            properties.addAllFrom(getPlayerProperties());
            properties.addAllFrom(getHeroProperties());
            properties.addAllFrom(getAbilitiesProperties());
            properties.addAllFrom(getItemsProperties());
            properties.addAllFrom(getBuildingsProperties());
            properties.addAllFrom(getDraftProperties());
            return properties;
        }
    
        public Dictionary<string, object> getProviderProperties() {
            return provider?.getProperties("game") ?? new Dictionary<string, object>();
        }
    
        public Dictionary<string, object> getMapProperties() {
            return map?.getProperties("dota") ?? new Dictionary<string, object>();
        }
        
        public Dictionary<string, object> getPlayerProperties() {
            return player?.getProperties("dota") ?? new Dictionary<string, object>();
        }
    
        public Dictionary<string, object> getHeroProperties() {
            return hero?.getProperties("dota") ?? new Dictionary<string, object>();
        }
    
        public Dictionary<string, object> getAbilitiesProperties() {
            return abilities?.getProperties("dota") ?? new Dictionary<string, object>();
        }
        
        public Dictionary<string, object> getItemsProperties() {
            return items?.getProperties("dota") ?? new Dictionary<string, object>();
        }
        
        public Dictionary<string, object> getBuildingsProperties() {
            return buildings?.getProperties("dota") ?? new Dictionary<string, object>();
        }
        
        public Dictionary<string, object> getDraftProperties() {
            return draft?.getProperties("dota") ?? new Dictionary<string, object>();
        }
    }
    
    public class Provider : DotaEntity {
        [JsonIgnore] private static readonly EventDetails DOTA_PROVIDER_CHANGED_EVENT      = new("PROVIDER CHANGED",     "dota_provider_details_changed",              ["dota", "provider"]);
        [JsonIgnore] public static readonly List<EventDetails> PROVIDER_EVENT_LIST = [
            DOTA_PROVIDER_CHANGED_EVENT
        ];
        
        [JsonProperty("name")] public string? name { get; set; }
        [JsonProperty("appid")] public int? appId { get; set; }
        [JsonProperty("version")] public int? appVersion { get; set; }
        [JsonProperty("timestamp")] public long? timestamp { get; set; }

        public override void handleChanges(Action<EventDetails, Dictionary<string, (object newValue, object oldValue)>> onEvent) {
            var providerChanges = getChanges(nameof(name), nameof(appId), nameof(appVersion), nameof(timestamp));
            
            if (providerChanges.Any()) {
                onEvent(DOTA_PROVIDER_CHANGED_EVENT, providerChanges);
            }
        }

        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.provider", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.provider.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class Map : DotaEntity {
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_MATCH_CHANGED_EVENT      = new("MATCH CHANGED",     "dota_map_match_changed",              ["dota", "map", "match"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_TIME_CHANGED_EVENT       = new("TIME CHANGED",      "dota_map_time_changed",               ["dota", "map", "time"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_DAY_EVENT                = new("DAY STARTED",       "dota_map_day_changed",                ["dota", "map", "time"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_NIGHT_EVENT              = new("NIGHT STARTED",     "dota_map_night_changed",              ["dota", "map", "time"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_NIGHTSTALKER_NIGHT_EVENT = new("NIGHTSTALKER NIGHT","dota_map_nightstalker_night_changed", ["dota", "map", "time"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_RADIANT_SCORE_EVENT      = new("RADIANT SCORE CHANGE", "dota_map_radiant_score_changed", ["dota", "map", "score"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_DIRE_SCORE_EVENT         = new("DIRE SCORE CHANGED",   "dota_map_dire_score_changed",    ["dota", "map", "score"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_GAME_STATE_EVENT         = new("GAME STATE CHANGED",   "dota_map_game_state_changed", ["dota", "map", "state"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_GAME_PAUSED_EVENT        = new("GAME PAUSED", "dota_map_game_pause_changed",          ["dota", "map", "state"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_GAME_UNPAUSE_EVENT       = new("GAME UNPAUSED", "dota_map_game_unpause_changed",      ["dota", "map", "state"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_GAME_WIN_TEAM_EVENT      = new("GAME WIN TEAM", "dota_map_game_win_team_changed", ["dota", "map", "winning"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_CUSTOM_GAME_NAME_EVENT   = new("CUSTOM GAME NAME", "dota_map_custom_game_name_changed", ["dota", "map", "custom"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_WARD_COOLDOWN_EVENT         = new("WARD COOLDOWN",         "dota_map_ward_cooldown_changed",         ["dota", "map", "ward"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_WARD_COOLDOWN_RADIANT_EVENT = new("WARD COOLDOWN RADIANT", "dota_map_ward_cooldown_radiant_changed", ["dota", "map", "ward"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_WARD_COOLDOWN_DIRE_EVENT    = new("WARD COOLDOWN DIRE", "dota_map_ward_cooldown_dire_changed", ["dota", "map", "ward"]);
        [JsonIgnore] private static readonly EventDetails DOTA_MAP_ROSHAN_STATE_CHANGED     = new("ROSHAN STATE CHANGE", "dota_map_roshan_state_changed", ["dota", "map", "roshan"]);
        [JsonIgnore] public static readonly List<EventDetails> MAP_EVENT_LIST = [
            DOTA_MAP_MATCH_CHANGED_EVENT,
            DOTA_MAP_TIME_CHANGED_EVENT,
            DOTA_MAP_DAY_EVENT,
            DOTA_MAP_NIGHT_EVENT,
            DOTA_MAP_NIGHTSTALKER_NIGHT_EVENT,
            DOTA_MAP_RADIANT_SCORE_EVENT,
            DOTA_MAP_DIRE_SCORE_EVENT,
            DOTA_MAP_GAME_STATE_EVENT,
            DOTA_MAP_GAME_PAUSED_EVENT,
            DOTA_MAP_GAME_UNPAUSE_EVENT,
            DOTA_MAP_GAME_WIN_TEAM_EVENT,
            DOTA_MAP_CUSTOM_GAME_NAME_EVENT,
            DOTA_MAP_WARD_COOLDOWN_EVENT,
            DOTA_MAP_WARD_COOLDOWN_RADIANT_EVENT,
            DOTA_MAP_WARD_COOLDOWN_DIRE_EVENT,
            DOTA_MAP_ROSHAN_STATE_CHANGED
        ];
        
        [JsonProperty("name")] public string? name { get; set; }
        [JsonProperty("matchid")] public string? matchId { get; set; }
        [JsonProperty("game_time")] public int? gameTime { get; set; }
        [JsonProperty("clock_time")] public int? clockTime { get; set; }
        [JsonProperty("daytime")] public bool? isDayTime { get; set; }
        [JsonProperty("nightstalker_night")] public bool? isNightStalkerNight { get; set; }
        [JsonProperty("radiant_score")] public int? radiantScore { get; set; }
        [JsonProperty("dire_score")] public int? direScore { get; set; }
        [JsonProperty("game_state")] public GameState? gameState { get; set; }
        [JsonProperty("paused")] public bool? isPaused { get; set; }
        [JsonProperty("win_team")] public Team? winningTeam { get; set; }
        [JsonProperty("customgamename")] public string? customGameName { get; set; }
        [JsonProperty("ward_purchase_cooldown")] public int? wardPurchaseCooldown { get; set; }
        [JsonProperty("radiant_ward_purchase_cooldown")] public int? radiantWardPurchaseCooldown { get; set; }
        [JsonProperty("dire_ward_purchase_cooldown")] public int? direWardPurchaseCooldown { get; set; }
        [JsonProperty("roshan_state")] public RoshanState? roshanState { get; set; }
        [JsonProperty("roshan_state_end_time")] public int? roshanStateEndTime { get; set; }
        
        public override void handleChanges(Action<EventDetails, Dictionary<string, (object newValue, object oldValue)>> onEvent) {
            var matchChanges = getChanges(nameof(name), nameof(matchId));
            var timeChanges = getChanges(nameof(gameTime), nameof(clockTime));
            var dayChanges = getChanges(nameof(isDayTime), nameof(isNightStalkerNight));
            var radiantScoreChanges = getChanges(nameof(radiantScore));
            var direScoreChanges = getChanges(nameof(direScore));
            var stateChanges = getChanges(nameof(gameState));
            var pausedChanges = getChanges(nameof(isPaused));
            var winningChanges = getChanges(nameof(winningTeam));
            var customGameNameChanges = getChanges(nameof(customGameName));
            var wardCooldownChanges = getChanges(nameof(wardPurchaseCooldown), nameof(radiantWardPurchaseCooldown), nameof(direWardPurchaseCooldown));
            var roshanStateChanges = getChanges(nameof(roshanState), nameof(roshanStateEndTime));
    
            if (matchChanges.Any()) {
                onEvent(DOTA_MAP_MATCH_CHANGED_EVENT, matchChanges);
            }
            if (timeChanges.Any()) {
                onEvent(DOTA_MAP_TIME_CHANGED_EVENT, timeChanges);
            }
            if (dayChanges.Any()) {
                if (isNightStalkerNight != null && isNightStalkerNight.Value) {
                    onEvent(DOTA_MAP_NIGHTSTALKER_NIGHT_EVENT, dayChanges);
                } else {
                    if (isDayTime != null) {
                        onEvent(isDayTime.Value ? DOTA_MAP_DAY_EVENT : DOTA_MAP_NIGHT_EVENT, dayChanges);
                    }
                }
            }
            if (radiantScoreChanges.Any()) {
                onEvent(DOTA_MAP_RADIANT_SCORE_EVENT, radiantScoreChanges);
            }
            if (direScoreChanges.Any()) {
                onEvent(DOTA_MAP_DIRE_SCORE_EVENT, direScoreChanges);
            }
            if (stateChanges.Any()) {
                onEvent(DOTA_MAP_GAME_STATE_EVENT, stateChanges);
            }
            if (pausedChanges.Any()) {
                if (isPaused != null) {
                    onEvent(isPaused.Value ? DOTA_MAP_GAME_PAUSED_EVENT : DOTA_MAP_GAME_UNPAUSE_EVENT, pausedChanges);
                }
            }
            if (winningChanges.Any()) {
                onEvent(DOTA_MAP_GAME_WIN_TEAM_EVENT, winningChanges);
            }
            if (customGameNameChanges.Any()) {
                onEvent(DOTA_MAP_CUSTOM_GAME_NAME_EVENT, customGameNameChanges);
            }
            if (wardCooldownChanges.Any()) {
                onEvent(DOTA_MAP_WARD_COOLDOWN_EVENT, wardCooldownChanges);
                if (wardCooldownChanges.ContainsKey(nameof(radiantWardPurchaseCooldown))) {
                    onEvent(DOTA_MAP_WARD_COOLDOWN_RADIANT_EVENT, wardCooldownChanges);
                }
                if (wardCooldownChanges.ContainsKey(nameof(direWardPurchaseCooldown))) {
                    onEvent(DOTA_MAP_WARD_COOLDOWN_DIRE_EVENT, wardCooldownChanges);
                }
            }
            if (roshanStateChanges.Any()) {
                onEvent(DOTA_MAP_ROSHAN_STATE_CHANGED, roshanStateChanges);
            }
        }
        
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.map", ToString() }
            };
            
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.map.{field.Name}", field.GetValue(this));
            }
            
            return properties;
        }
    }
    
    public class Player : DotaEntity {
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_DETAILS_CHANGED_EVENT    = new("PLAYER CHANGED",     "dota_player_details_changed",                ["dota", "player", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_ACTIVITY_CHANGED_EVENT   = new("ACTIVITY CHANGED",   "dota_player_activity_changed",               ["dota", "player", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_KILLS_CHANGED_EVENT      = new("KILLS CHANGED",      "dota_player_kills_changed",                  ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_DEATHS_CHANGED_EVENT     = new("DEATHS CHANGED",     "dota_player_deaths_changed",                 ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_ASSISTS_CHANGED_EVENT    = new("ASSISTS CHANGED",    "dota_player_assists_changed",                ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_LAST_HIT_CHANGED_EVENT   = new("LAST HIT CHANGED",   "dota_player_last_hit_changed",               ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_DENIES_CHANGED_EVENT     = new("DENIES CHANGED",     "dota_player_denies_changed",                 ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_COMMANDS_EVENT           = new("COMMANDS CHANGED",   "dota_player_commands_changed",               ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_SLOT_EVENT               = new("SLOT CHANGED",       "dota_player_slot_changed",                   ["dota", "player", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_EVENT               = new("GOLD CHANGED",             "dota_player_gold_changed",             ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_RELIABLE_EVENT      = new("GOLD(RELIABLE) CHANGED",   "dota_player_gold_reliable_changed",    ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_UNRELIABLE_EVENT    = new("GOLD(UNRELIABLE) CHANGED", "dota_player_gold_unreliable_changed",  ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_KILLS_EVENT         = new("GOLD(KILLs) CHANGED",      "dota_player_gold_kills_changed",       ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_CREEPS_EVENT        = new("GOLD(CREEPs) CHANGED",     "dota_player_gold_creeps_changed",      ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_INCOME_EVENT        = new("GOLD(INCOME) CHANGED",     "dota_player_gold_income_changed",      ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_SHARED_EVENT        = new("GOLD(SHARED) CHANGED",     "dota_player_gold_shared_changed",      ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GPM_EVENT                = new("GPM CHANGED",              "dota_player_gpm_changed",              ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_XPM_EVENT                = new("XPM CHANGED",              "dota_player_xpm_changed",              ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_SEAT_EVENT               = new("SEAT CHANGED",             "dota_player_seat_changed",             ["dota", "player", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_NET_WORTH_EVENT          = new("NET WORTH CHANGED",        "dota_player_net_worth_changed",        ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_HERO_DAMAGE_EVENT        = new("HERO DAMAGE CHANGED",      "dota_player_hero_damage_changed",      ["dota", "player", "damage"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_HERO_HEALING_EVENT       = new("HERO HEALING CHANGED",     "dota_player_hero_healing_changed",     ["dota", "player", "damage"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_TOWER_DAMAGE_EVENT       = new("TOWER DAMAGE CHANGED",     "dota_player_tower_damage_changed",     ["dota", "player", "damage"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_WARDS_PURCHASED_EVENT    = new("WARDS PURCHASED",     "dota_player_wards_purchased_changed",       ["dota", "player", "wards"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_WARDS_PLACED_EVENT       = new("WARDS PLACED",        "dota_player_wards_placed_changed",          ["dota", "player", "wards"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_WARDS_DESTROYED_EVENT    = new("WARDS DESTROYED",     "dota_player_wards_destroyed_changed",       ["dota", "player", "wards"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_RUNES_ACTIVATED_EVENT    = new("RUNES ACTIVATED",     "dota_player_runes_activated_changed",       ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_CAMPS_STACKED_EVENT      = new("CAMPS STACKED",       "dota_player_camps_stacked_changed",         ["dota", "player", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_SPENT_SUPPORT_EVENT    = new("GOLD SPENT(SUPPORT)",     "dota_player_gold_spent_support_changed",    ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_SPENT_CONSUMABLE_EVENT = new("GOLD SPENT(CONSUMABLE)",  "dota_player_gold_spent_consumable_changed", ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_SPENT_ITEMS_EVENT      = new("GOLD SPENT(ITEMs)",      "dota_player_gold_spent_items_changed",       ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_LOST_DEATH_EVENT       = new("GOLD LOST(DEATHs)",      "dota_player_gold_lost_deaths_changed",       ["dota", "player", "gold"]);
        [JsonIgnore] private static readonly EventDetails DOTA_PLAYER_GOLD_LOST_BUYBACK_EVENT     = new("GOLD LOST(BUYBACKs)",    "dota_player_gold_lost_buybacks_changed",     ["dota", "player", "gold"]);
        [JsonIgnore] public static readonly List<EventDetails> PLAYER_EVENT_LIST = [
            DOTA_PLAYER_DETAILS_CHANGED_EVENT,
            DOTA_PLAYER_ACTIVITY_CHANGED_EVENT,
            DOTA_PLAYER_KILLS_CHANGED_EVENT,
            DOTA_PLAYER_DEATHS_CHANGED_EVENT,
            DOTA_PLAYER_ASSISTS_CHANGED_EVENT,
            DOTA_PLAYER_LAST_HIT_CHANGED_EVENT,
            DOTA_PLAYER_DENIES_CHANGED_EVENT,    
            DOTA_PLAYER_COMMANDS_EVENT,          
            DOTA_PLAYER_SLOT_EVENT,              
            DOTA_PLAYER_GOLD_EVENT,              
            DOTA_PLAYER_GOLD_RELIABLE_EVENT,     
            DOTA_PLAYER_GOLD_UNRELIABLE_EVENT,   
            DOTA_PLAYER_GOLD_KILLS_EVENT,        
            DOTA_PLAYER_GOLD_CREEPS_EVENT,
            DOTA_PLAYER_GOLD_INCOME_EVENT,       
            DOTA_PLAYER_GOLD_SHARED_EVENT,       
            DOTA_PLAYER_GPM_EVENT,               
            DOTA_PLAYER_XPM_EVENT,               
            DOTA_PLAYER_SEAT_EVENT,              
            DOTA_PLAYER_NET_WORTH_EVENT,         
            DOTA_PLAYER_HERO_DAMAGE_EVENT,       
            DOTA_PLAYER_HERO_HEALING_EVENT,      
            DOTA_PLAYER_TOWER_DAMAGE_EVENT,      
            DOTA_PLAYER_WARDS_PURCHASED_EVENT,   
            DOTA_PLAYER_WARDS_PLACED_EVENT,
            DOTA_PLAYER_WARDS_DESTROYED_EVENT,   
            DOTA_PLAYER_RUNES_ACTIVATED_EVENT,
            DOTA_PLAYER_CAMPS_STACKED_EVENT,
            DOTA_PLAYER_GOLD_SPENT_SUPPORT_EVENT,
            DOTA_PLAYER_GOLD_SPENT_CONSUMABLE_EVENT,
            DOTA_PLAYER_GOLD_SPENT_ITEMS_EVENT,
            DOTA_PLAYER_GOLD_LOST_DEATH_EVENT,
            DOTA_PLAYER_GOLD_LOST_BUYBACK_EVENT
        ];
        
        [JsonProperty("steamid")] public string? steamId { get; set; }
        [JsonProperty("accountid")] public string? accountId { get; set; }
        [JsonProperty("name")] public string? name { get; set; }
        [JsonProperty("activity")] public string? activity { get; set; }
        [JsonProperty("kills")] public int? kills { get; set; }
        [JsonProperty("deaths")] public int? deaths { get; set; }
        [JsonProperty("assists")] public int? assists { get; set; }
        [JsonProperty("last_hits")] public int? lastHits { get; set; }
        [JsonProperty("denies")] public int? denies { get; set; }
        [JsonProperty("kill_streak")] public int? killStreak { get; set; }
        [JsonProperty("commands_issued")] public int? commandsIssued { get; set; }
        [JsonProperty("kill_list")] public Dictionary<string, int>? killList { get; set; }
        [JsonProperty("team_name")] public string? teamName { get; set; }
        [JsonProperty("player_slot")] public int? slotPlayer { get; set; }
        [JsonProperty("team_slot")] public int? slotTeam { get; set; }
        [JsonProperty("gold")] public int? gold { get; set; }
        [JsonProperty("gold_reliable")] public int? goldReliable { get; set; }
        [JsonProperty("gold_unreliable")] public int? goldUnreliable { get; set; }
        [JsonProperty("gold_from_hero_kills")] public int? goldFromKills { get; set; }
        [JsonProperty("gold_from_creep_kills")] public int? goldFromCreeps { get; set; }
        [JsonProperty("gold_from_income")] public int? goldFromIncome { get; set; }
        [JsonProperty("gold_from_shared")] public int? goldFromShared { get; set; }
        [JsonProperty("gpm")] public int? gpm { get; set; }
        [JsonProperty("xpm")] public int? xpm { get; set; }
        [JsonProperty("onstage_seat")] public int? onStageSeat { get; set; }
        [JsonProperty("net_worth")] public int? netWorth { get; set; }
        [JsonProperty("hero_damage")] public int? heroDamage { get; set; }
        [JsonProperty("hero_healing")] public int? heroHealing { get; set; }
        [JsonProperty("tower_damage")] public int? towerDamage { get; set; }
        [JsonProperty("wards_purchased")] public int? wardsPurchased { get; set; }
        [JsonProperty("wards_placed")] public int? wardsPlaced { get; set; }
        [JsonProperty("wards_destroyed")] public int? wardsDestroyed { get; set; }
        [JsonProperty("runes_activated")] public int? runesActivated { get; set; }
        [JsonProperty("camps_stacked")] public int? campsStacked { get; set; }
        [JsonProperty("support_gold_spent")] public int? supportGoldSpent { get; set; }
        [JsonProperty("consumable_gold_spent")] public int? consumableGoldSpent { get; set; }
        [JsonProperty("item_gold_spent")] public int? itemGoldSpent { get; set; }
        [JsonProperty("gold_lost_to_death")] public int? goldLostToDeath { get; set; }
        [JsonProperty("gold_spent_on_buybacks")] public int? goldSpentOnBuybacks { get; set; }

        public override void handleChanges(Action<EventDetails, Dictionary<string, (object newValue, object oldValue)>> onEvent) {
            var playerDetailsChanges = getChanges(nameof(steamId), nameof(accountId), nameof(name));
            var activityChanges = getChanges(nameof(activity));
            var killsChanges = getChanges(nameof(kills), nameof(killStreak));
            var deathsChanges = getChanges(nameof(deaths));
            var assistsChanges = getChanges(nameof(assists));
            var lastHitsChanges = getChanges(nameof(lastHits));
            var deniesChanges = getChanges(nameof(denies));
            var commandsIssuedChanges = getChanges(nameof(commandsIssued));
            var playerSlotChanges = getChanges(nameof(teamName), nameof(slotPlayer), nameof(slotTeam));
            var goldChanges = getChanges(nameof(gold));
            var goldReliableChanges = getChanges(nameof(goldReliable));
            var goldUnreliableChanges = getChanges(nameof(goldUnreliable));
            var goldFromKillsChanges = getChanges(nameof(goldFromKills));
            var goldFromCreepsChanges = getChanges(nameof(goldFromCreeps));
            var goldFromIncomeChanges = getChanges(nameof(goldFromIncome));
            var goldFromSharedChanges = getChanges(nameof(goldFromShared));
            var gpmChanges = getChanges(nameof(gpm));
            var xpmChanges = getChanges(nameof(xpm));
            var onStageSeatChanges = getChanges(nameof(onStageSeat));
            var netWorthChanges = getChanges(nameof(netWorth));
            var heroDamageChanges = getChanges(nameof(heroDamage));
            var heroHealingChanges = getChanges(nameof(heroHealing));
            var towerDamageChanges = getChanges(nameof(towerDamage));
            var wardsPurchasedChanges = getChanges(nameof(wardsPurchased));
            var wardsPlacedChanges = getChanges(nameof(wardsPlaced));
            var wardsDestroyedChanges = getChanges(nameof(wardsDestroyed));
            var runesActivatedChanges = getChanges(nameof(runesActivated));
            var campsStackedChanges = getChanges(nameof(campsStacked));
            var supportGoldSpentChanges = getChanges(nameof(supportGoldSpent));
            var consumableGoldSpentChanges = getChanges(nameof(consumableGoldSpent));
            var itemGoldSpentChanges = getChanges(nameof(itemGoldSpent));
            var goldLostToDeathChanges = getChanges(nameof(goldLostToDeath));
            var goldSpentOnBuybacksChanges = getChanges(nameof(goldSpentOnBuybacks));

            if (playerDetailsChanges.Any()) {
                onEvent(DOTA_PLAYER_DETAILS_CHANGED_EVENT, playerDetailsChanges);
            }
            if (activityChanges.Any()) {
                onEvent(DOTA_PLAYER_ACTIVITY_CHANGED_EVENT, activityChanges);
            }
            if (killsChanges.Any()) {
                killsChanges.addAllFrom(goldFromKillsChanges, false);
                killsChanges.addAllFrom(gpmChanges, false);
                killsChanges.addAllFrom(xpmChanges, false);
                killsChanges.addAllFrom(netWorthChanges, false);
                killsChanges.addAllFrom(heroDamageChanges, false);
                onEvent(DOTA_PLAYER_KILLS_CHANGED_EVENT, killsChanges);
            }
            if (deathsChanges.Any()) {
                deathsChanges.addAllFrom(goldLostToDeathChanges, false);
                deathsChanges.addAllFrom(gpmChanges, false);
                deathsChanges.addAllFrom(xpmChanges, false);
                deathsChanges.addAllFrom(netWorthChanges, false);
                onEvent(DOTA_PLAYER_DEATHS_CHANGED_EVENT, deathsChanges);
            }
            if (assistsChanges.Any()) {
                assistsChanges.addAllFrom(goldFromSharedChanges, false);
                assistsChanges.addAllFrom(gpmChanges, false);
                assistsChanges.addAllFrom(xpmChanges, false);
                assistsChanges.addAllFrom(netWorthChanges, false);
                onEvent(DOTA_PLAYER_ASSISTS_CHANGED_EVENT, assistsChanges);
            }
            if (lastHitsChanges.Any()) {
                lastHitsChanges.addAllFrom(goldFromCreepsChanges, false);
                lastHitsChanges.addAllFrom(gpmChanges, false);
                lastHitsChanges.addAllFrom(xpmChanges, false);
                lastHitsChanges.addAllFrom(netWorthChanges, false);
                onEvent(DOTA_PLAYER_LAST_HIT_CHANGED_EVENT, lastHitsChanges);
            }
            if (deniesChanges.Any()) {   
                onEvent(DOTA_PLAYER_DENIES_CHANGED_EVENT, deniesChanges);
            }
            if (commandsIssuedChanges.Any()) {
                onEvent(DOTA_PLAYER_COMMANDS_EVENT, commandsIssuedChanges);
            }
            if (playerSlotChanges.Any()) {
                onEvent(DOTA_PLAYER_SLOT_EVENT, playerSlotChanges);
            }
            if (goldChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_EVENT, goldChanges);
            }
            if (goldReliableChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_RELIABLE_EVENT, goldReliableChanges);
            }
            if (goldUnreliableChanges.Any()) {   
                onEvent(DOTA_PLAYER_GOLD_UNRELIABLE_EVENT, goldUnreliableChanges);
            }
            if (goldFromKillsChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_KILLS_EVENT, goldFromKillsChanges);
            }
            if (goldFromCreepsChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_CREEPS_EVENT, goldFromCreepsChanges);
            }
            if (goldFromIncomeChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_INCOME_EVENT, goldFromIncomeChanges);
            }
            if (goldFromSharedChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_SHARED_EVENT, goldFromSharedChanges);
            }
            if (gpmChanges.Any()) {
                onEvent(DOTA_PLAYER_GPM_EVENT, gpmChanges);
            }
            if (xpmChanges.Any()) {
                onEvent(DOTA_PLAYER_XPM_EVENT, xpmChanges);
            }
            if (onStageSeatChanges.Any()) {
                onEvent(DOTA_PLAYER_SEAT_EVENT, onStageSeatChanges);
            }
            if (netWorthChanges.Any()) {
                onEvent(DOTA_PLAYER_NET_WORTH_EVENT, netWorthChanges);
            }
            if (heroDamageChanges.Any()) {
                onEvent(DOTA_PLAYER_HERO_DAMAGE_EVENT, heroDamageChanges);
            }
            if (heroHealingChanges.Any()) {
                onEvent(DOTA_PLAYER_HERO_HEALING_EVENT, heroHealingChanges);
            }
            if (towerDamageChanges.Any()) {
                onEvent(DOTA_PLAYER_TOWER_DAMAGE_EVENT, towerDamageChanges);
            }
            if (wardsPurchasedChanges.Any()) {
                onEvent(DOTA_PLAYER_WARDS_PURCHASED_EVENT, wardsPurchasedChanges);
            }
            if (wardsPlacedChanges.Any()) {
                onEvent(DOTA_PLAYER_WARDS_PLACED_EVENT, wardsPlacedChanges);
            }
            if (wardsDestroyedChanges.Any()) {
                onEvent(DOTA_PLAYER_WARDS_DESTROYED_EVENT, wardsDestroyedChanges);
            }
            if (runesActivatedChanges.Any()) {
                onEvent(DOTA_PLAYER_RUNES_ACTIVATED_EVENT, runesActivatedChanges);
            }
            if (campsStackedChanges.Any()) {
                onEvent(DOTA_PLAYER_CAMPS_STACKED_EVENT, campsStackedChanges);
            }
            if (supportGoldSpentChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_SPENT_SUPPORT_EVENT, supportGoldSpentChanges);
            }
            if (consumableGoldSpentChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_SPENT_CONSUMABLE_EVENT, consumableGoldSpentChanges);
            }
            if (itemGoldSpentChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_SPENT_ITEMS_EVENT, itemGoldSpentChanges);
            }
            if (goldLostToDeathChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_LOST_DEATH_EVENT, goldLostToDeathChanges);   
            }
            if (goldSpentOnBuybacksChanges.Any()) {
                onEvent(DOTA_PLAYER_GOLD_LOST_BUYBACK_EVENT, goldSpentOnBuybacksChanges);  
            }
        }

        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.player", ToString() }
            };
            
            foreach (var field in GetType().GetProperties()) {
                if (field.Name == nameof(killList)) {
                    properties.AddIfNotEmpty($"{prefix}.player.{nameof(killList)}", JsonConvert.SerializeObject(field.GetValue(this), Extensions.serializeSettings));
                    continue;
                }
                properties.AddIfNotEmpty($"{prefix}.player.{field.Name}", field.GetValue(this));
            }
            
            return properties;
        }
    }
    
    public class Hero : DotaEntity {
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_CHANGED_EVENT           = new("HERO CHANGED",      "dota_hero_changed",                ["dota", "hero", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_POSITION_CHANGED_EVENT  = new("POSITION CHANGED",  "dota_hero_position_changed",       ["dota", "hero", "activity"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_LEVEL_CHANGED_EVENT     = new("LEVEL CHANGED",  "dota_hero_level_changed",       ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_XP_CHANGED_EVENT        = new("XP CHANGED",  "dota_hero_xp_changed",       ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DIED_CHANGED_EVENT      = new("HERO DIED",  "dota_hero_died_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_RESPAWNED_CHANGED_EVENT = new("HERO RESPAWNED",  "dota_hero_respawned_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_BUYSBACK_CHANGED_EVENT  = new("HERO RESPAWNED(BUYBACK)",  "dota_hero_respawned_buyback_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_RESPAWN_SECONDS_CHANGED_EVENT  = new("RESPAWN COOLDOWN CHANGED",  "dota_hero_respawn_cooldown_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_BUYBACK_COST_CHANGED_EVENT     = new("BUYBACK COST CHANGED",  "dota_hero_buyback_cost_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_BUYBACK_COOLDOWN_CHANGED_EVENT = new("BUYBACK COOLDOWN CHANGED",  "dota_hero_buyback_cooldown_changed",       ["dota", "hero", "respawn"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_HEALTH_CHANGED_EVENT  = new("HEALTH CHANGED",  "dota_hero_health_changed",       ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_MANA_CHANGED_EVENT    = new("MANA CHANGED",  "dota_hero_mana_changed",       ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_CHANGED_EVENT      = new("ANY DEBUFFs",   "dota_hero_debuff_changed",            ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_SILENCED_EVENT     = new("HERO SILENCED", "dota_hero_debuff_silenced_changed",   ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_STUNNED_EVENT      = new("HERO STUNNED",  "dota_hero_debuff_stunned_changed",    ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_DISARMED_EVENT     = new("HERO DISARMED", "dota_hero_debuff_disarmed_changed",   ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_MAGIC_IMMUNE_EVENT = new("HERO MAGIC IMMUNE",  "dota_hero_magic_immune_changed", ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_HEXED_EVENT        = new("HERO HEXED",   "dota_hero_debuff_hexed_changed",       ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_MUTED_EVENT        = new("HERO MUTED",   "dota_hero_debuff_muted_changed",       ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_DEBUFF_BROKEN_EVENT       = new("HERO BROKEN",  "dota_hero_debuff_broken_changed",      ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_AGHANIM_EVENT       = new("HERO AGHAMIN",  "dota_hero_aghanim_changed",                 ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_SHARD_EVENT         = new("HERO SHARD",  "dota_hero_shard_changed",                     ["dota", "hero", "stats"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_SMOKED_EVENT        = new("HERO SMOKED",  "dota_hero_smoked_changed",                   ["dota", "hero", "debuff"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_ATTRIBUTE_LEVEL_EVENT     = new("ATTR LEVEL CHANGED",  "dota_hero_attribute_level_changed",      ["dota", "hero", "stats"]);        
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_TALENT_LEVEL_ANY_EVENT     = new("TALENT CHANGES",       "dota_hero_talent_any_changed",        ["dota", "hero", "stats", "talent"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_TALENT_LEVEL_10_EVENT     = new("TALENT LVL10 CHANGES",  "dota_hero_talent_lvl10_changed",      ["dota", "hero", "stats", "talent"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_TALENT_LEVEL_15_EVENT     = new("TALENT LVL15 CHANGES",  "dota_hero_talent_lvl15_changed",      ["dota", "hero", "stats", "talent"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_TALENT_LEVEL_20_EVENT     = new("TALENT LVL20 CHANGES",  "dota_hero_talent_lvl20_changed",      ["dota", "hero", "stats", "talent"]);
        [JsonIgnore] private static readonly EventDetails DOTA_HERO_TALENT_LEVEL_25_EVENT     = new("TALENT LVL25 CHANGES",  "dota_hero_talent_lvl25_changed",      ["dota", "hero", "stats", "talent"]);
        [JsonIgnore] public static readonly List<EventDetails> HERO_EVENT_LIST = [
            DOTA_HERO_CHANGED_EVENT,
            DOTA_HERO_POSITION_CHANGED_EVENT,
            DOTA_HERO_LEVEL_CHANGED_EVENT,     
            DOTA_HERO_XP_CHANGED_EVENT,        
            DOTA_HERO_DIED_CHANGED_EVENT,      
            DOTA_HERO_RESPAWNED_CHANGED_EVENT, 
            DOTA_HERO_BUYSBACK_CHANGED_EVENT,  
            DOTA_HERO_RESPAWN_SECONDS_CHANGED_EVENT, 
            DOTA_HERO_BUYBACK_COST_CHANGED_EVENT,    
            DOTA_HERO_BUYBACK_COOLDOWN_CHANGED_EVENT,
            DOTA_HERO_HEALTH_CHANGED_EVENT,
            DOTA_HERO_MANA_CHANGED_EVENT,
            DOTA_HERO_DEBUFF_CHANGED_EVENT,     
            DOTA_HERO_DEBUFF_SILENCED_EVENT,    
            DOTA_HERO_DEBUFF_STUNNED_EVENT,     
            DOTA_HERO_DEBUFF_DISARMED_EVENT,    
            DOTA_HERO_DEBUFF_MAGIC_IMMUNE_EVENT,
            DOTA_HERO_DEBUFF_HEXED_EVENT,       
            DOTA_HERO_DEBUFF_MUTED_EVENT,       
            DOTA_HERO_DEBUFF_BROKEN_EVENT,      
            DOTA_HERO_AGHANIM_EVENT,
            DOTA_HERO_SHARD_EVENT,
            DOTA_HERO_SMOKED_EVENT,
            DOTA_HERO_ATTRIBUTE_LEVEL_EVENT,
            DOTA_HERO_TALENT_LEVEL_ANY_EVENT,
            DOTA_HERO_TALENT_LEVEL_10_EVENT, 
            DOTA_HERO_TALENT_LEVEL_15_EVENT, 
            DOTA_HERO_TALENT_LEVEL_20_EVENT, 
            DOTA_HERO_TALENT_LEVEL_25_EVENT, 
        ];
        
        [JsonProperty("facet")] public int? facet { get; set; }
        [JsonProperty("xpos")] public int? xpos { get; set; }
        [JsonProperty("ypos")] public int? ypos { get; set; }
        [JsonProperty("id")] public int? id { get; set; }
        [JsonProperty("name")] public string? name { get; set; }
        [JsonProperty("level")] public int? level { get; set; }
        [JsonProperty("xp")] public int? xp { get; set; }
        [JsonProperty("alive")] public bool? isAlive { get; set; }
        [JsonProperty("respawn_seconds")] public int? respawnSeconds { get; set; }
        [JsonProperty("buyback_cost")] public int? buybackCost { get; set; }
        [JsonProperty("buyback_cooldown")] public int? buybackCooldown { get; set; }
        [JsonProperty("health")] public int? health { get; set; }
        [JsonProperty("max_health")] public int? maxHealth { get; set; }
        [JsonProperty("health_percent")] public int? healthPercent { get; set; }
        [JsonProperty("mana")] public int? mana { get; set; }
        [JsonProperty("max_mana")] public int? maxMana { get; set; }
        [JsonProperty("mana_percent")] public int? manaPercent { get; set; }
        [JsonProperty("silenced")] public bool? isSilenced { get; set; }
        [JsonProperty("stunned")] public bool? isStunned { get; set; }
        [JsonProperty("disarmed")] public bool? isDisarmed { get; set; }
        [JsonProperty("magicimmune")] public bool? isMagicImmune { get; set; }
        [JsonProperty("hexed")] public bool? isHexed { get; set; }
        [JsonProperty("muted")] public bool? isMuted { get; set; }
        [JsonProperty("break")] public bool? isBroken { get; set; }
        [JsonProperty("aghanims_scepter")] public bool? isAghanim { get; set; }
        [JsonProperty("aghanims_shard")] public bool? isShard { get; set; }
        [JsonProperty("smoked")] public bool? isSmoked { get; set; }
        [JsonProperty("has_debuff")] public bool? hasDebuff { get; set; }
        [JsonProperty("attributes_level")] public int? attributesLevel { get; set; }
        [JsonProperty("talent_1")] public bool? talent10_right { get; set; }
        [JsonProperty("talent_2")] public bool? talent10_left { get; set; }
        [JsonProperty("talent_3")] public bool? talent15_right { get; set; }
        [JsonProperty("talent_4")] public bool? talent15_left { get; set; }
        [JsonProperty("talent_5")] public bool? talent20_right { get; set; }
        [JsonProperty("talent_6")] public bool? talent20_left { get; set; }
        [JsonProperty("talent_7")] public bool? talent25_right { get; set; }
        [JsonProperty("talent_8")] public bool? talent25_left { get; set; }
    
        [JsonIgnore]
        public TalentTree talentTree => new() {
            level25 = new TalentTreeChoice { hasLeft = talent25_left, hasRight = talent25_right },
            level20 = new TalentTreeChoice { hasLeft = talent20_left, hasRight = talent20_right },
            level15 = new TalentTreeChoice { hasLeft = talent15_left, hasRight = talent15_right },
            level10 = new TalentTreeChoice { hasLeft = talent10_left, hasRight = talent10_right }
        };

        public override void handleChanges(Action<EventDetails, Dictionary<string, (object newValue, object oldValue)>> onEvent) {
            var heroDetailsChanges = getChanges(nameof(facet), nameof(id), nameof(name));
            var heroPositionChanges = getChanges(nameof(xpos), nameof(ypos));
            var heroLevelChanges = getChanges(nameof(level));
            var heroXPChanges = getChanges(nameof(xp));
            var heroAliveChanges = getChanges(nameof(isAlive));
            var heroRespawnSecondsChanges = getChanges(nameof(respawnSeconds));
            var heroBuybackCostChanges = getChanges(nameof(buybackCost));
            var heroBuybackCooldownChanges = getChanges(nameof(buybackCooldown));
            var heroHealthChanges = getChanges(nameof(health), nameof(maxHealth), nameof(healthPercent));
            var heroManaChanges = getChanges(nameof(mana), nameof(maxMana), nameof(manaPercent));
            var heroSilencedChanges = getChanges(nameof(isSilenced));
            var heroStunnedChanges = getChanges(nameof(isStunned));
            var heroDisarmedChanges = getChanges(nameof(isDisarmed));
            var heroMagicImmuneChanges = getChanges(nameof(isMagicImmune));
            var heroHexedChanges = getChanges(nameof(isHexed));
            var heroMutedChanges = getChanges(nameof(isMuted));
            var heroBrokenChanges = getChanges(nameof(isBroken));
            var heroAghanimChanges = getChanges(nameof(isAghanim));
            var heroShardChanges = getChanges(nameof(isShard));
            var heroSmokedChanges = getChanges(nameof(isSmoked));
            var heroHasDebuffChanges = getChanges(nameof(hasDebuff));
            var heroAttributesLevelChanges = getChanges(nameof(attributesLevel));
            var talentLevel = getChanges(nameof(talentTree));
            var talentLevel10 = getChanges(nameof(talent10_left), nameof(talent10_right));
            var talentLevel15 = getChanges(nameof(talent15_left), nameof(talent15_right));
            var talentLevel20 = getChanges(nameof(talent20_left), nameof(talent20_right));
            var talentLevel25 = getChanges(nameof(talent25_left), nameof(talent25_right));

            if (heroDetailsChanges.Any()) {
                onEvent(DOTA_HERO_CHANGED_EVENT, heroDetailsChanges);
            }
            if (heroPositionChanges.Any()) {
                onEvent(DOTA_HERO_POSITION_CHANGED_EVENT, heroPositionChanges);
            }
            if (heroLevelChanges.Any()) {
                onEvent(DOTA_HERO_LEVEL_CHANGED_EVENT, heroLevelChanges);
            }
            if (heroXPChanges.Any()) {
                onEvent(DOTA_HERO_XP_CHANGED_EVENT, heroXPChanges);
            }
            if (heroAliveChanges.Any()) {
                onEvent(isAlive.Value ? DOTA_HERO_RESPAWNED_CHANGED_EVENT : DOTA_HERO_DIED_CHANGED_EVENT, heroAliveChanges);
                if (buybackCooldown is > 0) {
                    onEvent(DOTA_HERO_BUYSBACK_CHANGED_EVENT, heroAliveChanges);
                }
            }
            if (heroRespawnSecondsChanges.Any()) {
                onEvent(DOTA_HERO_RESPAWN_SECONDS_CHANGED_EVENT, heroRespawnSecondsChanges);
            }
            if (heroBuybackCostChanges.Any()) {
                onEvent(DOTA_HERO_BUYBACK_COST_CHANGED_EVENT, heroBuybackCostChanges);
            }
            if (heroBuybackCooldownChanges.Any()) {
                onEvent(DOTA_HERO_BUYBACK_COOLDOWN_CHANGED_EVENT, heroBuybackCooldownChanges);
            }
            if (heroHealthChanges.Any()) {
                onEvent(DOTA_HERO_HEALTH_CHANGED_EVENT, heroHealthChanges);
            }
            if (heroManaChanges.Any()) {
                onEvent(DOTA_HERO_MANA_CHANGED_EVENT, heroManaChanges);
            }
            if (heroHasDebuffChanges.Any() || heroSilencedChanges.Any() || heroStunnedChanges.Any() || heroDisarmedChanges.Any() || heroHexedChanges.Any() || heroMutedChanges.Any() || heroBrokenChanges.Any()) {
                heroHasDebuffChanges.addAllFrom(heroSilencedChanges, false);
                heroHasDebuffChanges.addAllFrom(heroStunnedChanges, false);
                heroHasDebuffChanges.addAllFrom(heroDisarmedChanges, false);
                heroHasDebuffChanges.addAllFrom(heroHexedChanges, false);
                heroHasDebuffChanges.addAllFrom(heroMutedChanges, false);
                heroHasDebuffChanges.addAllFrom(heroBrokenChanges, false);
                onEvent(DOTA_HERO_DEBUFF_CHANGED_EVENT, heroHasDebuffChanges);
            }
            if (heroSilencedChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_SILENCED_EVENT, heroSilencedChanges);
            }
            if (heroStunnedChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_STUNNED_EVENT, heroStunnedChanges);
            }
            if (heroDisarmedChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_DISARMED_EVENT, heroDisarmedChanges);
            }
            if (heroMagicImmuneChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_MAGIC_IMMUNE_EVENT, heroMagicImmuneChanges);
            }
            if (heroHexedChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_HEXED_EVENT, heroHexedChanges);
            }
            if (heroMutedChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_MUTED_EVENT, heroMutedChanges);
            }
            if (heroBrokenChanges.Any()) {
                onEvent(DOTA_HERO_DEBUFF_BROKEN_EVENT, heroBrokenChanges);
            }
            if (heroAghanimChanges.Any()) {
                onEvent(DOTA_HERO_AGHANIM_EVENT, heroAghanimChanges);
            }
            if (heroShardChanges.Any()) {
                onEvent(DOTA_HERO_SHARD_EVENT, heroShardChanges);
            }
            if (heroSmokedChanges.Any()) {
                onEvent(DOTA_HERO_SMOKED_EVENT, heroSmokedChanges);
            }
            if (heroAttributesLevelChanges.Any()) {
                onEvent(DOTA_HERO_ATTRIBUTE_LEVEL_EVENT, heroAttributesLevelChanges);
            }
            if (talentLevel10.Any() || talentLevel15.Any() || talentLevel20.Any() || talentLevel25.Any()) {
                onEvent(DOTA_HERO_TALENT_LEVEL_ANY_EVENT, talentLevel);
                if (talentLevel10.Any()) {
                    onEvent(DOTA_HERO_TALENT_LEVEL_10_EVENT, talentLevel10);
                }
                if (talentLevel15.Any()) {
                    onEvent(DOTA_HERO_TALENT_LEVEL_15_EVENT, talentLevel15);
                }
                if (talentLevel20.Any()) {
                    onEvent(DOTA_HERO_TALENT_LEVEL_20_EVENT, talentLevel20);
                }
                if (talentLevel25.Any()) {
                    onEvent(DOTA_HERO_TALENT_LEVEL_25_EVENT, talentLevel25);
                }
            }
        }
        
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.hero", ToString() }
            };
            
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.hero.{field.Name}", field.GetValue(this));
                if (field.Name == nameof(talentTree)) {
                    properties.addAllFrom(talentTree.getProperties($"{prefix}.hero.{field.Name}"));
                }
            }
            
            return properties;
        }
    }
    
    public class Abilities : Dictionary<string, Ability> {
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }

        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.abilities", ToString() }
            };

            foreach (var ability in this) {
                properties.addAllFrom(ability.Value.getProperties($"{prefix}.abilities.{ability.Key}"));
            }
        
            return properties;
        }
    }
    
    public class Items : Dictionary<string, Item> {
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.items", ToString() }
            };

            foreach (var item in this) {
                properties.addAllFrom(item.Value.getProperties($"{prefix}.items.{item.Key}"));
            }
        
            return properties;
        }
    }
    
    public class Buildings {
        [JsonProperty("radiant")] public Dictionary<string, Tower> radiant { get; set; }
        [JsonProperty("dire")] public Dictionary<string, Tower> dire { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.buildings", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
            
                if (field.GetValue(this) is Dictionary<string, Tower> buildings) {
                    properties.AddIfNotEmpty($"{prefix}.buildings.{field.Name}", JsonConvert.SerializeObject(buildings, Extensions.serializeSettings));
                    properties.AddIfNotEmpty($"{prefix}.buildings.{field.Name}.count", buildings.Count.ToString());

                    foreach (var tower in buildings) {
                        properties.addAllFrom(tower.Value.getProperties($"{prefix}.buildings.{field.Name}.{tower.Key}"));
                    }

                    continue;
                }
            
                properties.AddIfNotEmpty($"{prefix}.buildings.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class Draft {
        [JsonProperty("activeteam")] public int activeTeam { get; set; }
        [JsonProperty("pick")] public bool isPick { get; set; }
        [JsonProperty("activeteam_time_remaining")] public int activeTeamTimeRemaining { get; set; }
        [JsonProperty("radiant_bonus_time")] public int radiantBonusTime { get; set; }
        [JsonProperty("dire_bonus_time")] public int direBonusTime { get; set; }
        [JsonProperty("team2")] public TeamDraft radiantDraft { get; set; }
        [JsonProperty("team3")] public TeamDraft direDraft { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.draft", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                if (field.GetValue(this) is TeamDraft teamDraft) {
                    properties.addAllFrom(teamDraft.getProperties($"{prefix}.draft.{field.Name}"));
                    continue;
                }
            
                properties.AddIfNotEmpty($"{prefix}.draft.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    //-----------------------------------------------------------------------------------------------------
    public class Ability {
        [JsonProperty("name")] public string name { get; set; }
        [JsonProperty("level")] public int level { get; set; }
        [JsonProperty("can_cast")] public bool canCast { get; set; }
        [JsonProperty("passive")] public bool isPassive { get; set; }
        [JsonProperty("ability_active")] public bool isEnabled { get; set; }
        [JsonProperty("cooldown")] public int cooldown { get; set; }
        [JsonProperty("ultimate")] public bool isUltimate { get; set; }
        [JsonProperty("charges")] public int charges { get; set; }
        [JsonProperty("max_charges")] public int maxCharges { get; set; }
        [JsonProperty("charge_cooldown")] public int chargeCooldown { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.ability", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.ability.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class Item {
        [JsonProperty("name")] public string name { get; set; }
        [JsonProperty("purchaser")] public int purchaser { get; set; }
        [JsonProperty("item_level")] public int level { get; set; }
        [JsonProperty("contains_rune")] public BottledRune bottledRune { get; set; }
        [JsonProperty("can_cast")] public bool canCast { get; set; }
        [JsonProperty("cooldown")] public int cooldown { get; set; }
        [JsonProperty("passive")] public bool isPassive { get; set; }
        [JsonProperty("item_charges")] public int itemCharges { get; set; }
        [JsonProperty("ability_charges")] public int abilityCharges { get; set; }
        [JsonProperty("max_charges")] public int maxCharges { get; set; }
        [JsonProperty("charge_cooldown")] public int chargeCooldown { get; set; }
        [JsonProperty("charges")] public int charges { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.item", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.item.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class Tower {
        [JsonProperty("health")] public int health { get; set; }
        [JsonProperty("max_health")] public int maxHealth { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.tower", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.tower.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class TeamDraft {
        [JsonProperty("home_team")] public bool isHomeTeam { get; set; }
        [JsonProperty("picks")] public List<DraftHero> picks { get; set; }
        [JsonProperty("bans")] public List<DraftHero> bans { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.team", ToString() }
            };
        
            foreach (var field in GetType().GetProperties()) {
                if (field.GetValue(this) is List<DraftHero> list) {
                    properties.AddIfNotEmpty($"{prefix}.team.{field.Name}", JsonConvert.SerializeObject(list, Extensions.serializeSettings));
                    properties.AddIfNotEmpty($"{prefix}.team.{field.Name}.count", list.Count.ToString());

                    foreach (var draftHero in list) {
                        properties.addAllFrom(draftHero.getProperties($"{prefix}.team.{field.Name}"));
                    }
                
                    continue;
                }
            
                properties.AddIfNotEmpty($"{prefix}.team.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class DraftHero {
        [JsonProperty("id")] public int id { get; set; }
        [JsonProperty("name")] public string name { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object> {
                { $"{prefix}.hero", ToString() }
            };
        
            foreach (var field in GetType().GetFields()) {
                properties.AddIfNotEmpty($"{prefix}.hero.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    public class TalentTree {
        public TalentTreeChoice level25 { get; set; }
        public TalentTreeChoice level20 { get; set; }
        public TalentTreeChoice level15 { get; set; }
        public TalentTreeChoice level10 { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object>();
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.talent.{field.Name}", field.GetValue(this));
                properties.addAllFrom((field.GetValue(this) as TalentTreeChoice)?.getProperties($"{prefix}.talent.{field.Name}"));
            }
        
            return properties;
        }
    }
    
    public class TalentTreeChoice {
        public bool? hasLeft { get; set; }
        public bool? hasRight { get; set; }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    
        public Dictionary<string, object> getProperties(string prefix) {
            var properties = new Dictionary<string, object>();
            foreach (var field in GetType().GetProperties()) {
                properties.AddIfNotEmpty($"{prefix}.{field.Name}", field.GetValue(this));
            }
        
            return properties;
        }
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BottledRune {
        EMPTY,
    
        DOUBLE_DAMAGE,
        ILLUSION,
        SHIELD,
        HASTE,
        REGEN,
        INVIS,

        ARCANE,
        BOUNTY,
        WATER
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Team {
        NONE,
        SPECTATOR,
        NEUTRALS,
        RADIANT,
        DIRE
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameState {
        DOTA_GAMERULES_STATE_DISCONNECT,
        DOTA_GAMERULES_STATE_GAME_IN_PROGRESS,
        DOTA_GAMERULES_STATE_HERO_SELECTION,
        DOTA_GAMERULES_STATE_INIT,
        DOTA_GAMERULES_STATE_LAST,
        DOTA_GAMERULES_STATE_POST_GAME,
        DOTA_GAMERULES_STATE_PRE_GAME,
        DOTA_GAMERULES_STATE_STRATEGY_TIME,
        DOTA_GAMERULES_STATE_TEAM_SHOWCASE,
        DOTA_GAMERULES_STATE_WAIT_FOR_MAP_TO_LOAD,
        DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD,
        DOTA_GAMERULES_STATE_CUSTOM_GAME_SETUP
    }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoshanState {
        ALIVE,
        /// waiting base respawn rate
        RESPAWN_BASE,
        /// waiting variable respawn rate (additional time after base)
        RESPAWN_VARIABLE,
    }
    
    public abstract class DotaEntity {
        [JsonIgnore] private DotaEntity previously;

        public void initializePrevious(DotaEntity previous) {
            this.previously = previous;
        }

        public abstract void handleChanges(Action<EventDetails, Dictionary<string, (object newValue, object oldValue)>> onEvent);
        
        protected Dictionary<string, (object newValue, object oldValue)> getChanges(params string[] propertyPaths) {
            var changes = new Dictionary<string, (object NewValue, object OldValue)>();
            if (previously == null) return changes;

            foreach (var path in propertyPaths) {
                var newValue = getPropertyByPath(this, path);
                var oldValue = getPropertyByPath(previously, path);
                if (newValue != null && oldValue != null && !newValue.Equals(oldValue)) {
                    changes[path] = (newValue, oldValue);
                }
            }

            return changes;
        }
    
        private static object getPropertyByPath(object obj, string path) {
            foreach (var prop in path.Split('.')) {
                if (obj == null) return null;
                var type = obj.GetType();
                var property = type.GetProperty(prop);
                if (property == null) return null;
                obj = property.GetValue(obj);
            }
            return obj;
        }
    
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Extensions.serializeSettings);
        }
    }
    
    internal class DotaEntityConverter<T> : JsonConverter<T> where T : new() {
        public override T ReadJson(
            JsonReader reader, 
            Type objectType, 
            T existingValue, 
            bool hasExistingValue, 
            JsonSerializer serializer
        ) {
            return reader.TokenType == JsonToken.Boolean ? 
                new T() : 
                serializer.Deserialize<T>(reader);
        }
    
        public override void WriteJson(
            JsonWriter writer, 
            T value, 
            JsonSerializer serializer
        ) {
            serializer.Serialize(writer, value);
        }
    }
}