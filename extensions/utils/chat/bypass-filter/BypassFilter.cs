// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;
using System.Linq;
using System.Text.RegularExpressions;

/**
 * [FILTER] SPAM
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_BypassFilter : CPHInlineBase {
    private const string defaultCharacters = "[`~!@#%&*\\(\\)_=+|{};:'\",<.>?№\\$\\^\\-\\\\\\/\\[\\]\\s\\d]";
    private string patternCyrillic;
    private string patternLatin;
    
    private void init() {
        patternLatin    = getProperty("spam.pattern.latin"   , "[a-zA-Z]+");
        patternCyrillic = getProperty("spam.pattern.cyrillic", "[а-яА-ЯëёËЁА́а́Е́е́И́и́О́о́У́у́Ы́ы́Э́э́Ю́ю́Я́я]+");
    }
    
    private bool process() {
        var isAdmin = getProperty("isModerator", false);
        if (isAdmin) { //no need to validate admin messages
            return false;
        }
        
        return !isMessageValid();
    }
    
    private bool isMessageValid() {
        var isSubscriber = getProperty("isSubscribed", false);
        var isVip        = getProperty("isVip", false);
        
        var message = getProperty("rawInput", "");
        var noCharactersMessage = Regex.Replace(message, defaultCharacters, " ");
        var words = noCharactersMessage.Split(' ');
        
        var bypassWordsCount = words.Count(word => 
            Regex.IsMatch(word, patternCyrillic) && 
            Regex.IsMatch(word, patternLatin)
        ); //number of words contains symbols from both Cyrillic and Latin letters
        
        if (isSubscriber || isVip) {
            return bypassWordsCount < 4; //4 or more words found for sub/vip - timeout
        }
        
        //less 2 message is ok
        //more 2 or more words found for regular - timeout
        return bypassWordsCount < 2;
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