// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;

/**
 * PUT_NAME
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_TEMPLATE : CPHInlineBase {

    private string text;
    
    private void init() {
        text = getProperty("test.ket", "default.value");
    }

    private bool process() {
        return true;
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
        return process();
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