// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using Q42.HueApi;

using System;
using System.Threading.Tasks;

/**
 * [API] HUE
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_HUE : CPHInlineBase {
	private const string HUE_BRIDGE_APP_KEY_PROPERTY = "integration.phillipsHue.bridge.appKey";

	private string ip;
    private string appKey;
    
    private LocalHueClient client;
    
	private void init() {
		ip = getProperty("hue.bridge.ip", "127.0.0.1");
		appKey = CPH.GetGlobalVar<string>(HUE_BRIDGE_APP_KEY_PROPERTY, true);
		if (appKey is null or "") {
			DEBUG(() => "KEY NOT FOUND, REGISTER CLIENT");
			_ = registerNewApplication();
		} else {
			DEBUG(() => "KEY FOUND, CREATE CLIENT");
			client = new LocalHueClient(ip);
			client.Initialize(appKey);
		}
		_ = printAllLights();
    }
    
    public bool turnOnHueID() {
    	var id = getProperty("hue.id", "UNKNOWN");
    	
    	_ = updateHueState(id, true);
    	return true;
    }
    
    public bool turnOffHueID() {
    	var id = getProperty("hue.id", "UNKNOWN");
    	
    	_ = updateHueState(id, false);
    	return true;
    }
    
    
    public bool changeLightColor() {
    	var id = getProperty("hue.id", "UNKNOWN");
    	var color = getProperty("hue.hexColor" , "#FF0000");
    	var saturation = getProperty("hue.saturation", -1);
    	var brightness = getProperty("hue.brightness", -1);
    	
    	_ = updateLightColor(id, color, saturation, brightness);
    	return true;
    }
    
    //----------------------------------------------------------------
    // HUE API METHODS
    //----------------------------------------------------------------
    private async Task updateLightColor(string hueId, string hexColor, int saturation, int brightness) {
    	try {
    		var light = await client.GetLightAsync(hueId);
    	    if (light != null) {
    		    DEBUG(() => "Updating color for: " + light.Name);
    		
    		    var command = new LightCommand();
    		    command.TurnOn();
    		    command.Hue = HexToHue(hexColor);
    		    if (saturation != -1) {
        			command.Saturation = (byte) saturation;
    		    }
    		    if (brightness != -1) {
        			command.Brightness = (byte) brightness;
    		    }
    		
    		    await client.SendCommandAsync(command, new[] { light.Id });
    		    INFO(() => "Color updated to hex:" + hexColor + ", for: " + light.Name);
    	    }
    	} catch (Exception e) {
    		INFO(() => "Cannot change color for: {hue.id: " + hueId + "}: " + e.Message);
    	}
    }
    
    private async Task updateHueState(string hueId, bool isOn) {
    	try {
    		var device = await client.GetLightAsync(hueId);
    		if (device != null) {
    			DEBUG(() => "Updating state for: " + device.Name);
    			
    			var command = new LightCommand();
    			if (isOn) {
    				command.TurnOn();
    			} else {
    				command.TurnOff();
    			}
    			
    			
    			await client.SendCommandAsync(command, new[] { device.Id });
    			INFO(() => "State updated for: " + device.Name);
    		}
    	} catch (Exception e) {
    		INFO(() => "Cannot change state for: {hue.id: " + hueId + "}: " + e.Message);
    	}
    }
    
    private async Task printAllLights() {
    	var lights = await client.GetLightsAsync();
    	INFO(() => "PRINT ALL THE AVAILABLE HUE LIGHTS");
    	INFO(() => "=====================");
    	foreach(var light in lights) {
    		INFO(() => "HUE ID    : " + light.Id);
    		INFO(() => "HUE NAME  : " + light.Name);
    		INFO(() => "HUE TYPE  : " + light.Type);
    		INFO(() => "HUE MODEL : " + light.ModelId);
    		INFO(() => "HUE STATE : " + (light.State.On ? "ON" : "OFF"));
    		INFO(() => "=====================");
    	}
    }
    
    private async Task registerNewApplication() {
    	try {
    		DEBUG(() => "REGISTERING NEW APPLICATION");
    	
    	    client = new LocalHueClient(ip);
    	    var registrationResult = await client.RegisterAsync(ip, "SBotHueApplication", false);
	        if (registrationResult == null) {
		        DEBUG(() => "CANNOT CONNECT TO Hue BRIDGE");
		        return;
	        }
	        
    	    appKey = registrationResult.Username;
    	    CPH.SetGlobalVar(HUE_BRIDGE_APP_KEY_PROPERTY, appKey, true);
    	
    	    DEBUG(() => "REGISTERING OF APPLICATION COMPLETED");
    	} catch (Exception e) {
    		INFO(() => "Cannot register SBot: " + e.Message);
    	}
    }
    
    //----------------------------------------------------------------
    // HUE UTILITY METHODS
    //----------------------------------------------------------------
    private static int HexToHue(string hexColor) {
    	hexColor = hexColor.Replace("#", string.Empty).Trim();
    	
        // Convert hex to RGB
        var r = int.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = int.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = int.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        // Normalize RGB values to [0, 1]
        var rNorm = r / 255.0;
        var gNorm = g / 255.0;
        var bNorm = b / 255.0;

        // Find min and max RGB values
        var max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        var min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        var delta = max - min;

        double hue = 0;

        if (delta == 0) {
            hue = 0;
        }
        else if (max == rNorm) {
            hue = ((gNorm - bNorm) / delta) % 6;
        }
        else if (max == gNorm) {
            hue = ((bNorm - rNorm) / delta) + 2;
        }
        else if (max == bNorm) {
            hue = ((rNorm - gNorm) / delta) + 4;
        }

        hue *= 60;
        if (hue < 0) {
            hue += 360;
        }

        // Convert to Philips Hue scale (0-65535)
        return (int)((hue / 360) * 65535);
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

	    CPH.LogInfo("DEBUG: " + widgetActionName + " :: " + getMessage);
    }
    
    private void INFO(Func<string> getMessage) {
	    CPH.LogInfo("INFO : " + widgetActionName + " :: " + getMessage);
    }

    private void WARN(Func<string> getMessage) {
	    CPH.LogWarn("WARN : " + widgetActionName + " :: " + getMessage);
    }
    
    private void ERROR(Func<string> getMessage) {
	    CPH.LogError("ERROR: " + widgetActionName + " :: " + getMessage);
    }
}