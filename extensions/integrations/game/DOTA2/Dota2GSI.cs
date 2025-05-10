// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;

using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/**
 * DOTA 2 GAME STATE INTEGRATION
 */
// ReSharper disable once UnusedType.Global
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
        
        CPH.RegisterCustomTrigger("dota2_trigger", "dota2_trigger", [
            "dota2", 
            "trigger"
        ]);
        _ = startDota2EventListener();
    }

    private void handleGameEvent(GameEvent gameEvent, GameEvent lastEvent) {
        if (gameEvent == null) {
            return;
        }
        DEBUG(() => "GAME EVENT RECEIVED: " + gameEvent.provider.name);
        CPH.TriggerCodeEvent("dota2_trigger", gameEvent.getProperties());

        var differences = gameEvent.GetDifferencesTo(lastEvent);
        if (differences.Count == 0) {
            INFO((() => "NO DIFFERENCES"));
            return;
        }

        foreach (var diff in differences) {
            INFO(() => $"{diff.Key} : {diff.Value.OldValue} -> {diff.Value.NewValue}");
        }
        
        if (gameEvent.player?.kills > lastEvent?.player?.kills) {
            INFO(() => $"player kills: {gameEvent.player.kills}");
        }

        differences.TryGetValue("hero.level", out var pair);
        if (pair.NewValue != null) {
            //sendMessage($"[DOTA2 STAT] - ПУДЖ ТОЛЬКО ЧТО СТАЛ {pair.NewValue}го УРОВНЯ");
        }

        differences.TryGetValue("player.kills", out var killsCount);
        if (killsCount.NewValue != null) {
            //var keys = gameEvent.player!.killList.Keys.Aggregate("", (current, key) => current + ", " + key);

            //sendMessage($"[DOTA2 STAT] - ПУДЖ УЖЕ ПОБЕДИЛ - {killsCount.NewValue}х ДРУГИХ ПУДЖЕЙ (это на {(int) killsCount.NewValue - (int) killsCount.OldValue} больше чем было): {keys}");
        }

        INFO(() => "");
        INFO(() => "");
        INFO(() => "");
    }
    
    private static readonly byte[] responseMessage = Encoding.UTF8.GetBytes("{\"message\": \"ok\"}");
    private async Task startDota2EventListener() {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://{host}:{port}/{uri}");
        listener.Start();
        INFO(() => $"listener started on: http://{host}:{port}/{uri}");
        
        try {
            var gameEvent = new GameEvent();
            var lastEvent = new GameEvent();
            while (isRunning) {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                DEBUG(() => $"request info: {request.HttpMethod} -> {request.Url}");
                
                if (request.HttpMethod == "POST") {
                    try {
                        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                        var jsonData = await reader.ReadToEndAsync();
                        gameEvent = deserializeEntity<GameEvent>(jsonData);
                        handleGameEvent(gameEvent, lastEvent);
                        lastEvent = gameEvent;
                    } catch (Exception e) {
                        INFO(() => $"cannot handle json event: {e.Message}, " +
                                      $"reason: {e.InnerException?.Message}, " +
                                      $"stack: {e.StackTrace}"
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
            var dataObject = JsonConvert.DeserializeObject<T>(jsonData, GameExtension.deserializeSettings);
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

//----------------------------------------------------------------
// DOTA 2 EVENT EXTENSIONS CLASS
//----------------------------------------------------------------
public static class GameExtension {
    private static readonly Dictionary<Type, PropertyInfo[]> propertyCache = new();

    public static readonly JsonSerializerSettings deserializeSettings = new() {
        MissingMemberHandling = MissingMemberHandling.Ignore
    };
    public static readonly JsonSerializerSettings serializeSettings = new() {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dictionary<string, (object NewValue, object OldValue)> GetDifferencesTo(
        this object newObject,
        object oldObject,
        string parentPath = ""
    ) {
        if (newObject == oldObject) return new Dictionary<string, (object, object)>();
        if (newObject == null || oldObject == null)
            return new Dictionary<string, (object, object)> { [parentPath] = (newObject, oldObject) };

        var differences = new Dictionary<string, (object, object)>();
        var type = newObject.GetType();

        if (!propertyCache.TryGetValue(type, out PropertyInfo[] properties)) {
            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            propertyCache[type] = properties;
        }

        foreach (var prop in properties) {
            var newValue = prop.GetValue(newObject);
            var oldValue = prop.GetValue(oldObject);
            var fullPath = string.IsNullOrEmpty(parentPath) ? prop.Name : $"{parentPath}.{prop.Name}";
            
            if (newValue == oldValue) continue;
            if (newValue == null || oldValue == null) {
                differences[fullPath] = (newValue, oldValue);
                continue;
            }
            
            var propType = prop.PropertyType;
            if (propType.IsPrimitive || propType == typeof(string) || propType == typeof(decimal))
            {
                if (!newValue.Equals(oldValue)) differences[fullPath] = (newValue, oldValue);
                continue;
            }

            if (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string)) {
                var enum1 = ((IEnumerable)newValue).Cast<object>().ToArray();
                var enum2 = ((IEnumerable)oldValue).Cast<object>().ToArray();

                if (enum1.Length != enum2.Length) {
                    differences[fullPath] = ($"Size: {enum1.Length}", $"Size: {enum2.Length}");
                }
                var minCount = Math.Min(enum1.Length, enum2.Length);
                
                for (var i = 0; i < minCount; i++) {
                    foreach (var diff in enum1[i].GetDifferencesTo(enum2[i], $"{fullPath}[{i}]")) {
                        differences[diff.Key] = diff.Value;
                    }
                }
                continue;
            }
            
            foreach (var diff in newValue.GetDifferencesTo(oldValue, fullPath)) {
                differences[diff.Key] = diff.Value;
            }
        }

        return differences;
    }
    
    public static void AddIfNotEmpty(this Dictionary<string, object> dictionary, string key, object value) {
        switch (value) {
            case null:
                return;
            case Enum:
                dictionary[key] = value.ToString();
                return;
            default:
                dictionary[key] = value;
                break;
        }
    }
    
    public static Dictionary<TKey, TValue> addAllFrom<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> source) {
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

//----------------------------------------------------------------
// DOTA 2 GAME EVENT ENTITIES
//----------------------------------------------------------------
public class GameEvent {
    [JsonProperty("provider")] public Provider provider { get; set; }
    [JsonProperty("map")] public Map map { get; set; }
    [JsonProperty("player")] public Player player { get; set; }
    [JsonProperty("hero")] public Hero hero { get; set; }
    [JsonProperty("abilities")] public Dictionary<string, Ability> abilities { get; set; }
    [JsonProperty("items")] public Items items { get; set; }
    [JsonProperty("buildings")] public Buildings buildings { get; set; }
    [JsonProperty("draft")] public Draft draft { get; set; }

    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties() {
        var properties = new Dictionary<string, object>();
        properties.addAllFrom(getProviderProperties());
        properties.addAllFrom(getMapProperties());
        // properties.addAllFrom(getPlayerProperties());
        // properties.addAllFrom(getHeroProperties());
        // properties.addAllFrom(getItemsProperties());
        // properties.addAllFrom(getBuildingsProperties());
        // properties.addAllFrom(getDraftProperties());
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

public class Provider {
    [JsonProperty("name")] public string name { get; set; }
    [JsonProperty("appid")] public int appId { get; set; }
    [JsonProperty("version")] public int appVersion { get; set; }
    [JsonProperty("timestamp")] public long timestamp { get; set; }

    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
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

public class Map {
    [JsonProperty("name")] public string name { get; set; }
    [JsonProperty("matchid")] public string matchId { get; set; }
    
    [JsonProperty("game_time")] public int gameTime { get; set; }
    [JsonProperty("clock_time")] public int clockTime { get; set; }
    [JsonProperty("daytime")] public bool isDayTime { get; set; }
    [JsonProperty("nightstalker_night")] public bool isNightStalkerNight { get; set; }
    
    [JsonProperty("radiant_score")] public int radiantScore { get; set; }
    [JsonProperty("dire_score")] public int direScore { get; set; }
    
    [JsonProperty("game_state")] public GameState gameState { get; set; }
    
    [JsonProperty("paused")] public bool isPaused { get; set; }
    
    [JsonProperty("win_team")] public Team winningTeam { get; set; }
    
    [JsonProperty("customgamename")] public string customGameName { get; set; }
    [JsonProperty("ward_purchase_cooldown")] public int wardPurchaseCooldown { get; set; }
    [JsonProperty("radiant_ward_purchase_cooldown")] public int radiantWardPurchaseCooldown { get; set; }
    [JsonProperty("dire_ward_purchase_cooldown")] public int direWardPurchaseCooldown { get; set; }
    
    [JsonProperty("roshan_state")] public int roshanState { get; set; }
    [JsonProperty("roshan_state_end_time")] public int roshanStateEndTime { get; set; }

    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
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

public class Player {
    [JsonProperty("steamid")] public string steamId { get; set; }
    [JsonProperty("accountid")] public string accountId { get; set; }
    [JsonProperty("name")] public string name { get; set; }
    [JsonProperty("activity")] public string activity { get; set; }
    [JsonProperty("kills")] public int kills { get; set; }
    [JsonProperty("deaths")] public int deaths { get; set; }
    [JsonProperty("assists")] public int assists { get; set; }
    [JsonProperty("last_hits")] public int lastHits { get; set; }
    [JsonProperty("denies")] public int denies { get; set; }
    [JsonProperty("kill_streak")] public int killStreak { get; set; }
    [JsonProperty("commands_issued")] public int commandsIssued { get; set; }
    [JsonProperty("kill_list")] public Dictionary<string, int> killList { get; set; }
    [JsonProperty("team_name")] public string teamName { get; set; }
    [JsonProperty("player_slot")] public int slotPlayer { get; set; }
    [JsonProperty("team_slot")] public int slotTeam { get; set; }
    [JsonProperty("gold")] public int gold { get; set; }
    [JsonProperty("gold_reliable")] public int goldReliable { get; set; }
    [JsonProperty("gold_unreliable")] public int goldUnreliable { get; set; }
    [JsonProperty("gold_from_hero_kills")] public int goldKills { get; set; }
    [JsonProperty("gold_from_creep_kills")] public int goldCreeps { get; set; }
    [JsonProperty("gold_from_income")] public int goldIncome { get; set; }
    [JsonProperty("gold_from_shared")] public int goldShared { get; set; }
    [JsonProperty("gpm")] public int gpm { get; set; }
    [JsonProperty("xpm")] public int xpm { get; set; }
    [JsonProperty("onstage_seat")] public int onStageSeat { get; set; }
    [JsonProperty("net_worth")] public int netWorth { get; set; }
    [JsonProperty("hero_damage")] public int heroDamage { get; set; }
    [JsonProperty("hero_healing")] public int heroHealing { get; set; }
    [JsonProperty("tower_damage")] public int towerDamage { get; set; }
    [JsonProperty("wards_purchased")] public int wardsPurchased { get; set; }
    [JsonProperty("wards_placed")] public int wardsPlaced { get; set; }
    [JsonProperty("wards_destroyed")] public int wardsDestroyed { get; set; }
    [JsonProperty("runes_activated")] public int runesActivated { get; set; }
    [JsonProperty("camps_stacked")] public int campsStacked { get; set; }
    [JsonProperty("support_gold_spent")] public int supportGoldSpent { get; set; }
    [JsonProperty("consumable_gold_spent")] public int consumableGoldSpent { get; set; }
    [JsonProperty("item_gold_spent")] public int itemGoldSpent { get; set; }
    [JsonProperty("gold_lost_to_death")] public int goldLostToDeath { get; set; }
    [JsonProperty("gold_spent_on_buybacks")] public int goldSpentOnBuybacks { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.player.{field.Name}", field.GetValue(this));
        }
        
        return properties;
    }
}

public class Hero {
    [JsonProperty("facet")] public int facet { get; set; }
    [JsonProperty("xpos")] public int xpos { get; set; }
    [JsonProperty("ypos")] public int ypos { get; set; }
    [JsonProperty("id")] public int id { get; set; }
    [JsonProperty("name")] public string name { get; set; }
    [JsonProperty("level")] public int level { get; set; }
    [JsonProperty("xp")] public int xp { get; set; }
    [JsonProperty("alive")] public bool isAlive { get; set; }
    [JsonProperty("respawn_seconds")] public int respawnSeconds { get; set; }
    [JsonProperty("buyback_cost")] public int buybackCost { get; set; }
    [JsonProperty("buyback_cooldown")] public int buybackCooldown { get; set; }
    [JsonProperty("health")] public int health { get; set; }
    [JsonProperty("max_health")] public int maxHealth { get; set; }
    [JsonProperty("health_percent")] public int healthPercent { get; set; }
    [JsonProperty("mana")] public int mana { get; set; }
    [JsonProperty("max_mana")] public int maxMana { get; set; }
    [JsonProperty("mana_percent")] public int manaPercent { get; set; }
    [JsonProperty("silenced")] public bool isSilenced { get; set; }
    [JsonProperty("stunned")] public bool isStunned { get; set; }
    [JsonProperty("disarmed")] public bool isDisarmed { get; set; }
    [JsonProperty("magicimmune")] public bool isMagicImmune { get; set; }
    [JsonProperty("hexed")] public bool isHexed { get; set; }
    [JsonProperty("muted")] public bool isMuted { get; set; }
    [JsonProperty("break")] public bool isBroken { get; set; }
    [JsonProperty("aghanims_scepter")] public bool isAghanim { get; set; }
    [JsonProperty("aghanims_shard")] public bool isShard { get; set; }
    [JsonProperty("smoked")] public bool isSmoked { get; set; }
    [JsonProperty("has_debuff")] public bool hasDebuff { get; set; }
    [JsonProperty("attributes_level")] public int attributesLevel { get; set; }
    
    [JsonProperty("talent_1")] public bool talent1 { get; set; }
    [JsonProperty("talent_2")] public bool talent2 { get; set; }
    [JsonProperty("talent_3")] public bool talent3 { get; set; }
    [JsonProperty("talent_4")] public bool talent4 { get; set; }
    [JsonProperty("talent_5")] public bool talent5 { get; set; }
    [JsonProperty("talent_6")] public bool talent6 { get; set; }
    [JsonProperty("talent_7")] public bool talent7 { get; set; }
    [JsonProperty("talent_8")] public bool talent8 { get; set; }

    [JsonIgnore]
    public TalentTree talentTree => new() {
        level25 = new TalentTreeChoice { hasLeft = talent8, hasRight = talent7 },
        level20 = new TalentTreeChoice { hasLeft = talent6, hasRight = talent5 },
        level15 = new TalentTreeChoice { hasLeft = talent4, hasRight = talent3 },
        level10 = new TalentTreeChoice { hasLeft = talent2, hasRight = talent1 }
    };
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.hero.{field.Name}", field.GetValue(this));
        }
        
        return properties;
    }
}

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
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.ability.{field.Name}", field.GetValue(this));
        }
        
        return properties;
    }
}

public class Items {
    [JsonProperty("slot0")] public Item slot0 { get; set; }
    [JsonProperty("slot1")] public Item slot1 { get; set; }
    [JsonProperty("slot2")] public Item slot2 { get; set; }
    [JsonProperty("slot3")] public Item slot3 { get; set; }
    [JsonProperty("slot4")] public Item slot4 { get; set; }
    [JsonProperty("slot5")] public Item slot5 { get; set; }
    [JsonProperty("slot6")] public Item slot6 { get; set; }
    [JsonProperty("slot7")] public Item slot7 { get; set; }
    [JsonProperty("slot8")] public Item slot8 { get; set; }

    [JsonProperty("stash0")] public Item stash0 { get; set; }
    [JsonProperty("stash1")] public Item stash1 { get; set; }
    [JsonProperty("stash2")] public Item stash2 { get; set; }
    [JsonProperty("stash3")] public Item stash3 { get; set; }
    [JsonProperty("stash4")] public Item stash4 { get; set; }
    [JsonProperty("stash5")] public Item stash5 { get; set; }

    [JsonProperty("teleport0")] public Item teleport0 { get; set; }

    [JsonProperty("neutral0")] public Item neutral0 { get; set; }
    [JsonProperty("neutral1")] public Item neutral1 { get; set; }

    [JsonProperty("preserved_neutral6")] public Item preservedNeutral6 { get; set; }
    [JsonProperty("preserved_neutral7")] public Item preservedNeutral7 { get; set; }
    [JsonProperty("preserved_neutral8")] public Item preservedNeutral8 { get; set; }
    [JsonProperty("preserved_neutral9")] public Item preservedNeutral9 { get; set; }
    [JsonProperty("preserved_neutral10")] public Item preservedNeutral10 { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.items.{field.Name}", field.GetValue(this));
            
            if (field.GetValue(this) is not Item item) continue;
            properties.addAllFrom(item.getProperties($"{prefix}.items.{field.Name}"));
        }
        
        return properties;
    }
}

public class Item {
    [JsonProperty("name")] public string name { get; set; }
    [JsonProperty("purchaser")] public int purchaser { get; set; }
    [JsonProperty("item_level")] public int level { get; set; }
    [JsonProperty("contains_rune")] public BottledRune BottledRune { get; set; }
    [JsonProperty("can_cast")] public bool canCast { get; set; }
    [JsonProperty("cooldown")] public int cooldown { get; set; }
    [JsonProperty("passive")] public bool isPassive { get; set; }
    [JsonProperty("item_charges")] public int itemCharges { get; set; }
    [JsonProperty("ability_charges")] public int abilityCharges { get; set; }
    [JsonProperty("max_charges")] public int maxCharges { get; set; }
    [JsonProperty("charge_cooldown")] public int chargeCooldown { get; set; }
    [JsonProperty("charges")] public int charges { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.item.{field.Name}", field.GetValue(this));
        }
        
        return properties;
    }
}

public class Buildings {
    [JsonProperty("radiant")] public Dictionary<string, Tower> radiant { get; set; }
    [JsonProperty("dire")] public Dictionary<string, Tower> dire { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.buildings.{field.Name}", field.GetValue(this));
            
            if (field.GetValue(this) is not Dictionary<string, Tower> dictionary) continue;
            properties.Add($"{prefix}.buildings.{field.Name}.count", dictionary.Count.ToString());
            foreach (var tower in dictionary) {
                properties.addAllFrom(tower.Value.getProperties($"{prefix}.buildings.{field.Name}.{tower.Key}"));
            }
        }
        
        return properties;
    }
}

public class Tower {
    [JsonProperty("health")] public int health { get; set; }
    [JsonProperty("max_health")] public int maxHealth { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.tower.{field.Name}", field.GetValue(this));
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
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.draft.{field.Name}", field.GetValue(this));
            
            if (field.GetValue(this) is not TeamDraft teamDraft) continue;
            properties.addAllFrom(teamDraft.getProperties($"{prefix}.draft.{field.Name}"));
        }
        
        return properties;
    }
}

public class TeamDraft {
    [JsonProperty("home_team")] public bool isHomeTeam { get; set; }
    [JsonProperty("picks")] public List<DraftHero> picks { get; set; }
    [JsonProperty("bans")] public List<DraftHero> bans { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.draft.{field.Name}", field.GetValue(this));
            
            if (field.GetValue(this) is not List<DraftHero> list) continue;
            properties.Add($"{prefix}.team.{field.Name}.count", list.Count.ToString());
            foreach (var draftHero in list) {
                properties.addAllFrom(draftHero.getProperties($"{prefix}.team.{field.Name}"));
            }
        }
        
        return properties;
    }
}

public class DraftHero {
    [JsonProperty("id")] public int id { get; set; }
    [JsonProperty("name")] public string name { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
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
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.talent.{field.Name}", field.GetValue(this));
            properties.addAllFrom((field.GetValue(this) as TalentTreeChoice)?.getProperties($"{prefix}.talent.{field.Name}"));
        }
        
        return properties;
    }
}

public class TalentTreeChoice {
    public bool hasLeft { get; set; }
    public bool hasRight { get; set; }
    
    public override string ToString() {
        return JsonConvert.SerializeObject(this, GameExtension.serializeSettings);
    }
    
    public Dictionary<string, object> getProperties(string prefix) {
        var properties = new Dictionary<string, object>();
        foreach (var field in GetType().GetFields()) {
            properties.AddIfNotEmpty($"{prefix}.choice.{field.Name}", field.GetValue(this));
        }
        
        return properties;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum BottledRune {
    DOUBLE_DAMAGE,
    ILLUSION,
    SHIELD,
    HASTE,
    REGEN,
    INVIS,

    ARCANE,
    BOUNTY,
    WATER,
    
    EMPTY
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