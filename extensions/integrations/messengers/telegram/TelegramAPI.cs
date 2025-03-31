// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

/**
 * [API] TELEGRAM
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_TelegramAPI : CPHInlineBase {

    private string apiUrl;
    private string botToken;
    private string parseMode;
    private long pollInterval;

    private const string TG_MESSAGE_RECEIVED_PRIVATE = "tg.message.received.private";
    private const string TG_MESSAGE_RECEIVED_GROUP   = "tg.message.received.group";
    private const string TG_MESSAGE_RECEIVED_CHANNEL = "tg.message.received.channel";

    private void init() {
		botToken = getProperty("tg.token", "");
		parseMode = getProperty("tg.parse.mode", "MarkdownV2");
		pollInterval = getProperty("poll.interval", 5000); //5000ms = 5s
		apiUrl = $"https://api.telegram.org/bot{botToken}/";
		
		CPH.RegisterCustomTrigger("Message: PRIVATE", TG_MESSAGE_RECEIVED_PRIVATE, ["Telegram", "onMessageReceived"]);
		CPH.RegisterCustomTrigger("Message: GROUP", TG_MESSAGE_RECEIVED_GROUP, ["Telegram", "onMessageReceived"]);
		CPH.RegisterCustomTrigger("Message: CHANNEL", TG_MESSAGE_RECEIVED_CHANNEL, ["Telegram", "onMessageReceived"]);
    }
    
    public bool sendMessage() {
    	var chatId = getProperty("message.chat.id", 0L);
    	var threadId = getProperty("message.thread.id", 0L);
        //string[] lines = Regex.Unescape(getProperty("message.text", "HELLO WORLD\\nNEW LINE!")).Split('\n');
        var text = getProperty("message.text", "HELLO WORLD\\nNEW LINE!").Replace("\\n", "\n");
        var filePath = getProperty("message.file.path", "C:/dummy.png");
        
        var buttons = new List<InlineKeyboardButton>();
        for (var i = 1; i < 10; i++) {
        	var buttonText = getProperty("message.button." + i + ".text", "DUMMY");
        	var buttonURL = getProperty("message.button." + i + ".url", "DUMMY");
        	
        	if (buttonText.Equals("DUMMY") || buttonURL.Equals("DUMMY")) {
        		break;
        	}
        	
        	buttons.Add(new InlineKeyboardButton {
        		text = buttonText,
        		url = buttonURL
        	});
        }
    	
    	_ = sendMessage(chatId, threadId, text, filePath, createInlineKeyboard(buttons));
    	return true;
    }
    
    //----------------------------------------------------------------
    // TELEGRAM POLLING UPDATES 
    //----------------------------------------------------------------
    private Timer updateTimer;
    private long lastUpdateId;
    
    private void fetchUpdatesTask() { // Set up the timer to call GetUpdates periodically
        updateTimer = new Timer(pollInterval);
        updateTimer.Elapsed += async (_, _) => await receiveUpdates();
        updateTimer.AutoReset = true;
        updateTimer.Enabled = true;
    }

    private async Task receiveUpdates() {
    	DEBUG(() => "LISTENING FOR UPDATES");
    	try {
		    using var client = new HttpClient();
		    var response = await client.GetAsync($"{apiUrl}getUpdates?offset={lastUpdateId + 1}");
		    response.EnsureSuccessStatusCode();
		    
		    var responseBody = await response.Content.ReadAsStringAsync();
		    var telegramUpdates = JsonConvert.DeserializeObject<TelegramResponse>(responseBody);
		    handleUpdates(telegramUpdates.Result);
	    } catch (Exception e) {
        	INFO(() => $"CANNOT GET UPDATE BECAUSE: {e.Message}");
        }
    }

    private void handleUpdates(List<TelegramUpdate> updates) {
        foreach (var update in updates) {
	        if (update.Message != null) {
		        //if UserID who send message and ChatID is same, it's private message
		        if ((update.Message.From?.id ?? -1) == (update.Message.Chat?.id ?? -2)) {
			        handlePrivateMessageReceived(
				        update.Message.From, 
				        update.Message
			        );
			        lastUpdateId = update.UpdateId;
		        }
		        
		        // if Message is not empty then it's a Message from some GroupID
		        handleGroupMessageReceived(
			        update.Message.Chat,
			        update.Message.SenderChat,
			        update.Message.From,
			        update.Message
		        );
		        lastUpdateId = update.UpdateId;
	        }
        	
        	// if ChannelPost is not empty then this Message from ChannelID
        	if (update.ChannelPost != null) {
        		handleChannelPostReceived(
        		    update.ChannelPost.Chat,
        		    update.ChannelPost.SenderChat,
        		    update.ChannelPost.From,
        		    update.ChannelPost
        		);
        		lastUpdateId = update.UpdateId;
        		
        		continue;
        	}
        	
        	DEBUG(() => $"UNKNOWN: {update}");
            lastUpdateId = update.UpdateId;
        }
    }
    
    private void handleChannelPostReceived(
        Chat chat,
        Chat senderChat,
        User user,
        Message post
    ) {
    	var properties = new Dictionary<string, object> {
		    { "tg.chat.id", chat.id },
		    { "tg.chat.type", chat.type },
		    { "tg.chat.title", chat.title },
		    { "tg.chat.isForum", chat.isForum }
	    };

	    if (user != null) {
    		properties.Add("tg.author.id", user.id);
    		properties.Add("tg.author.type", "user");
    		properties.Add("tg.author.title", "user");
    		properties.Add("tg.author.isForum", "false");
    		
    		properties.Add("tg.author.isBot", user.isBot);
    	    properties.Add("tg.author.firstName", user.firstName);
    	    properties.Add("tg.author.lastName", user.lastName);
    	    properties.Add("tg.author.userName", user.userName);
    	} else {
    		properties.Add("tg.author.id", senderChat.id);
    		properties.Add("tg.author.type", senderChat.type);
    		properties.Add("tg.author.title", senderChat.title);
    		properties.Add("tg.author.isForum", senderChat.isForum);
    	}    	
    	
    	properties.Add("tg.post.isTopicMessage", post.isTopicMessage);
    	properties.Add("tg.post.topicId", post.topicId);
    	properties.Add("tg.post.id", post.id);
    	properties.Add("tg.post.date", post.date);
    	properties.Add("tg.post.text", post.text);
    	
    	CPH.TriggerCodeEvent(TG_MESSAGE_RECEIVED_CHANNEL, properties);
    	INFO(() => $"CHANNEL: {post.id} - {post.text}");
    }
    
    private void handleGroupMessageReceived(
        Chat chat,
        Chat senderChat,
        User user,
        Message message
    ) {
    	var properties = new Dictionary<string, object> {
		    { "tg.chat.id", chat.id },
		    { "tg.chat.type", chat.type },
		    { "tg.chat.title", chat.title },
		    { "tg.chat.isForum", chat.isForum }
	    };

	    if (senderChat != null) {
    		properties.Add("tg.author.id", senderChat.id);
    		properties.Add("tg.author.type", senderChat.type);
    		properties.Add("tg.author.title", senderChat.title);
    		properties.Add("tg.author.isForum", senderChat.isForum);
    	} else {
    		properties.Add("tg.author.id", user.id);
    		properties.Add("tg.author.type", "user");
    		properties.Add("tg.author.title", "user");
    		properties.Add("tg.author.isForum", "false");
    	}
    	properties.Add("tg.author.isBot", user.isBot);
    	properties.Add("tg.author.firstName", user.firstName);
    	properties.Add("tg.author.lastName", user.lastName);
    	properties.Add("tg.author.userName", user.userName);
    	
    	properties.Add("tg.message.isTopicMessage", message.isTopicMessage);
    	properties.Add("tg.message.topicId", message.topicId);
    	properties.Add("tg.message.id", message.id);
    	properties.Add("tg.message.date", message.date);
    	properties.Add("tg.message.text", message.text);
    	
    	CPH.TriggerCodeEvent(TG_MESSAGE_RECEIVED_GROUP, properties);
    	INFO(() => $"GROUP: {message.id} - {message.text}");
    }
    
    private void handlePrivateMessageReceived(
        User user,
        Message message
    ) {
    	var properties = new Dictionary<string, object> {
		    { "tg.author.id", user.id },
		    { "tg.author.isBot", user.isBot },
		    { "tg.author.firstName", user.firstName },
		    { "tg.author.lastName", user.lastName },
		    { "tg.author.userName", user.userName },
		    { "tg.message.id", message.id },
		    { "tg.message.date", message.date },
		    { "tg.message.text", message.text }
	    };

	    CPH.TriggerCodeEvent(TG_MESSAGE_RECEIVED_PRIVATE, properties);
    	INFO(() => $"PRIVATE: {message.id} - {message.text}");
    }
    
    //----------------------------------------------------------------
    // TELEGRAM API CALLS
    //----------------------------------------------------------------
    private static readonly string[] imageExtensions = [".jpg", ".jpeg", ".png",  ".bmp", ".tiff", ".webp", ".svg"];
    private static readonly string[] videoExtensions = [".mp4", ".gif",  ".mov",  ".avi",  ".mkv", ".webm", ".wmv"];
    private static readonly string[] audioExtensions = [".mp3", ".wav",  ".flac", ".aac", ".ogg",  ".aiff"];
    
    private async Task sendMessage(
        long chatId,
        long threadId,
        string text,
        string filePath,
        InlineKeyboardMarkup keyboard
    ) {
    	try {
		    using var client = new HttpClient();
		    var form = new MultipartFormDataContent();
		    form.Add(new StringContent(chatId.ToString()), "chat_id");
		    form.Add(new StringContent(parseMode), "parse_mode");
    		    
		    if (threadId > 0) {
			    form.Add(new StringContent(threadId.ToString()), "message_thread_id");
		    }
    		
		    var url = getUrlForContent(form, text, filePath, chatId);
            
		    if (keyboard != null) {
			    form.Add(new StringContent(JsonConvert.SerializeObject(keyboard)), "reply_markup");
			    DEBUG(() => $"Keyboard been attached to message for Chat: {chatId}");
		    }
            
		    var response = await client.PostAsync(url, form);
		    response.EnsureSuccessStatusCode();
		    INFO(() => $"Message been sent to Telegram Chat: {chatId}");
	    } catch (Exception e) {
    		INFO(() => $"Cannot send message to Telegram Chat: {chatId}, because: {e.Message}");
    	}
    }

    //----------------------------------------------------------------
    // TELEGRAM UTILITY METHODS
    //----------------------------------------------------------------
	private string getUrlForContent(
	    MultipartFormDataContent form, 
	    string text, 
	    string filePath, 
	    long chatId
	) {
		string url;
		if (File.Exists(filePath)) {
			form.Add(new StringContent(text), "caption");
	
			var fileExtension = Path.GetExtension(filePath).ToLower();
			if (Array.Exists(imageExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendPhoto";
				form.Add(new StreamContent(File.OpenRead(filePath)), "photo", Path.GetFileName(filePath));
				DEBUG(() => $"Sending message with Photo attached to: {chatId}");
			} else if (Array.Exists(videoExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendVideo";
				form.Add(new StreamContent(File.OpenRead(filePath)), "video", Path.GetFileName(filePath));
				DEBUG(() => $"Sending message with Video attached to: {chatId}");
			} else if (Array.Exists(audioExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendAudio";
				form.Add(new StreamContent(File.OpenRead(filePath)), "audio", Path.GetFileName(filePath));
				DEBUG(() => $"Sending message with Audio attached to: {chatId}");
			} else {
				url = $"{apiUrl}sendDocument";
				form.Add(new StreamContent(File.OpenRead(filePath)), "document", Path.GetFileName(filePath));
				DEBUG(() => $"Sending message with Document attached to: {chatId}");
			}
		} else if (Uri.IsWellFormedUriString(filePath, UriKind.Absolute)) {
			form.Add(new StringContent(text), "caption");
	
			var fileExtension = Path.GetExtension(new Uri(filePath).AbsolutePath).ToLower();
			if (Array.Exists(imageExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendPhoto";
				form.Add(new StringContent(filePath), "photo");
				DEBUG(() => $"Sending message with Photo{fileExtension} URL to: {chatId}");
			} else if (Array.Exists(videoExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendVideo";
				form.Add(new StringContent(filePath), "video");
				DEBUG(() => $"Sending message with Video{fileExtension} URL to: {chatId}");
			} else if (Array.Exists(audioExtensions, ext => ext == fileExtension)) {
				url = $"{apiUrl}sendAudio";
				form.Add(new StringContent(filePath), "audio");
				DEBUG(() => $"Sending message with Audio{fileExtension} URL to: {chatId}");
			} else {
				url = $"{apiUrl}sendDocument";
				form.Add(new StringContent(filePath), "document");
				DEBUG(() => $"Sending message with Document{fileExtension} URL to: {chatId}");
			}
		} else {
			url = $"{apiUrl}sendMessage";
			form.Add(new StringContent(text), "text");
			DEBUG(() => $"Sending simple message to Telegram Chat: {chatId}");
		}
		return url;
	}
    
    private static InlineKeyboardMarkup createInlineKeyboard(List<InlineKeyboardButton> buttons) {
        var rows = buttons
            .Select(button => new[] { button })
            .ToArray();

        var inlineKeyboard = new InlineKeyboardMarkup {
            InlineKeyboard = rows
        };

        return inlineKeyboard;
    }
    
    //----------------------------------------------------------------
    // TELEGRAM ENTITY'S
    //----------------------------------------------------------------
    public class TelegramResponse {
    	[JsonProperty("result")] public List<TelegramUpdate> Result { get; set; }
    }
    
    public class TelegramUpdate {
    	[JsonProperty("update_id")] public long UpdateId { get; set; }
    	[JsonProperty("message")] public Message Message { get; set; }
    	[JsonProperty("channel_post")] public Message ChannelPost { get; set; }
    }
    
    public class Message {
        [JsonProperty("message_id")] public long id { get; set; }
        [JsonProperty("message_thread_id")] public long topicId { get; set; }
        [JsonProperty("from")] public User From { get; set; }
        [JsonProperty("sender_chat")] public Chat SenderChat { get; set; }
        [JsonProperty("date")] public long date { get; set; }
        [JsonProperty("chat")] public Chat Chat { get; set; }
        [JsonProperty("text")] public string text { get; set; }
        [JsonProperty("is_topic_message")] public bool isTopicMessage { get; set; }
    }
    
    public class User {
    	[JsonProperty("id")] public long id { get; set; }
    	[JsonProperty("is_bot")] public bool isBot { get; set; }
    	[JsonProperty("first_name")] public string firstName { get; set; }
    	[JsonProperty("last_name")] public string lastName { get; set; }
    	[JsonProperty("username")] public string userName { get; set; }
    }
    
    public class Chat {
    	[JsonProperty("id")] public long id { get; set; }
    	[JsonProperty("type")] public string type { get; set; }
    	[JsonProperty("title")] public string title { get; set; }
    	[JsonProperty("is_forum")] public bool isForum { get; set; }
    }
    
    public class InlineKeyboardMarkup {
    	[JsonProperty("inline_keyboard")] public InlineKeyboardButton[][] InlineKeyboard { get; set; }
    }

    public class InlineKeyboardButton {
    	[JsonProperty("text")] public string text { get; set; }
    	[JsonProperty("url")] public string url { get; set; }
    	[JsonProperty("callback_data")] public string callbackData { get; set; }
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
        fetchUpdatesTask();
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