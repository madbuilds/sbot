// ReSharper disable InconsistentNaming
using System;
using System.Management;

public class CPHInline {
    private ManagementEventWatcher watcher;

    private const string KEYBOARD_CONNECTED_EVENT      = "system.event.keyboard.connected";
    private const string KEYBOARD_DISCONNECTED_EVENT   = "system.event.keyboard.disconnected";
    private const string MOUSE_CONNECTED_EVENT         = "system.event.mouse.connected";
    private const string MOUSE_DISCONNECTED_EVENT      = "system.event.mouse.disconnected";
    private const string USB_CONNECTED_EVENT           = "system.event.usb.connected";
    private const string USB_DISCONNECTED_EVENT        = "system.event.usb.disconnected";
    private const string DRIVE_CONNECTED_EVENT         = "system.event.drive.connected";
    private const string DRIVE_DISCONNECTED_EVENT      = "system.event.drive.disconnected";
    private const string AUDIO_CONNECTED_EVENT         = "system.event.audio.connected";
    private const string AUDIO_DISCONNECTED_EVENT      = "system.event.audio.disconnected";
    private const string CONTROLLER_CONNECTED_EVENT    = "system.event.controller.connected";
    private const string CONTROLLER_DISCONNECTED_EVENT = "system.event.controller.disconnected";
    private const string PRINTER_CONNECTED_EVENT       = "system.event.printer.connected";
    private const string PRINTER_DISCONNECTED_EVENT    = "system.event.printer.disconnected";
    private const string MEDIA_CONNECTED_EVENT         = "system.event.media.connected";
    private const string MEDIA_DISCONNECTED_EVENT      = "system.event.media.disconnected";
    private const string HID_CONNECTED_EVENT           = "system.event.hid.connected";
    private const string HID_DISCONNECTED_EVENT        = "system.event.hid.disconnected";
    private const string ANY_CONNECTED_EVENT           = "system.event.hid.connected";
    private const string ANY_DISCONNECTED_EVENT        = "system.event.hid.disconnected";

    private void init() {
        WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
        watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += new EventArrivedEventHandler(deviceEventArrived);

        CPH.RegisterCustomTrigger("Any: CONNECTED",    ANY_CONNECTED_EVENT,    new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Any: DISCONNECTED", ANY_DISCONNECTED_EVENT, new [] { "System", "onDeviceEvent" });

        CPH.RegisterCustomTrigger("Keyboard: CONNECTED",      KEYBOARD_CONNECTED_EVENT,      new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Keyboard: DISCONNECTED",   KEYBOARD_DISCONNECTED_EVENT,   new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Mouse: CONNECTED",         MOUSE_CONNECTED_EVENT,         new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Mouse: DISCONNECTED",      MOUSE_DISCONNECTED_EVENT,      new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("USB: CONNECTED",           USB_CONNECTED_EVENT,           new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("USB: DISCONNECTED",        USB_DISCONNECTED_EVENT,        new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Drive: CONNECTED",         DRIVE_CONNECTED_EVENT,         new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Drive: DISCONNECTED",      DRIVE_DISCONNECTED_EVENT,      new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Audio: CONNECTED",         AUDIO_CONNECTED_EVENT,         new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Audio: DISCONNECTED",      AUDIO_DISCONNECTED_EVENT,      new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Controller: CONNECTED",    CONTROLLER_CONNECTED_EVENT,    new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Controller: DISCONNECTED", CONTROLLER_DISCONNECTED_EVENT, new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Printer: CONNECTED",       PRINTER_CONNECTED_EVENT,       new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Printer: DISCONNECTED",    PRINTER_DISCONNECTED_EVENT,    new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Media: CONNECTED",         MEDIA_CONNECTED_EVENT,         new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Media: DISCONNECTED",      MEDIA_DISCONNECTED_EVENT,      new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Device: CONNECTED",        HID_CONNECTED_EVENT,           new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger("Device: DISCONNECTED",     HID_DISCONNECTED_EVENT,        new [] { "System", "onDeviceEvent" });
    }

    private bool process() {
        watcher.Start();
        logDebug(() => "START DEVICE EVENT WATCHER");

        return true;
    }

    private void deviceEventArrived(object sender, EventArrivedEventArgs e) {
        var eventType = e.NewEvent.ClassPath.ClassName;
        var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
        var deviceName = instance["Name"];
        var deviceType = instance["PNPClass"];
        var deviceId = instance["PNPDeviceID"];

        if (isDebugEnabled) {
            logDebug(() => "=================================");
            foreach (var property in instance.Properties) {
                logDebug(() => $"{property.Name}: {property.Value}");
            }
        }

        if (deviceType != null) {
            handleDeviceConnection(eventType, ANY_CONNECTED_EVENT, ANY_DISCONNECTED_EVENT, deviceName, deviceId);
            switch (deviceType) {
                case "Keyboard":
                    handleDeviceConnection(eventType, KEYBOARD_CONNECTED_EVENT, KEYBOARD_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;
                case "Mouse":
                    handleDeviceConnection(eventType, MOUSE_CONNECTED_EVENT, MOUSE_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;
                case "GameController":
                    handleDeviceConnection(eventType, CONTROLLER_CONNECTED_EVENT, CONTROLLER_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;
                case "DiskDrive":
                    handleDeviceConnection(eventType, DRIVE_CONNECTED_EVENT, DRIVE_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;
                case "AudioEndpoint":
                    handleDeviceConnection(eventType, AUDIO_CONNECTED_EVENT, AUDIO_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;
                case "Printer":
                    handleDeviceConnection(eventType, PRINTER_CONNECTED_EVENT, PRINTER_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;

                case "USB":
                    handleDeviceConnection(eventType, USB_CONNECTED_EVENT, USB_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;

                case "Media":
                    handleDeviceConnection(eventType, MEDIA_CONNECTED_EVENT, MEDIA_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;

                case "HIDClass":
                    handleDeviceConnection(eventType, HID_CONNECTED_EVENT, HID_DISCONNECTED_EVENT, deviceName, deviceId);
                    return;
                break;

                default:
                    logDebug(() => "UNHANDLED DEVICE_TYPE: " + deviceType + "(" + deviceId + ")");
                break;
            };
        }
    }

    private void handleDeviceConnection(string eventType, string connectedEvent, string disconnectedEvent, object deviceName, object deviceId) {
        if (eventType == "__InstanceCreationEvent") {
            handleDeviceEvent(connectedEvent, deviceName, deviceId);
        } else if (eventType == "__InstanceDeletionEvent") {
            handleDeviceEvent(disconnectedEvent, deviceName, deviceId);
        }
    }

    private void handleDeviceEvent(String eventName, object deviceName, object deviceId) {
        logDebug(() => $"{eventName}: {deviceName}");
        CPH.SetArgument("device.name", deviceName);
        CPH.SetArgument("device.id", deviceId);

        CPH.TriggerCodeEvent(eventName);
    }

    //----------------------------------------------------------------
    // DEFAULT METHODS AND SETUP
    //----------------------------------------------------------------
    private bool isInitialized = false;
    private bool isDebugEnabled = false;
    private String widgetActionName = "TEMPLATE";

    public bool Execute() {
        setUp();
        return process();
    }

    private void setUp() {
        if (isInitialized) {
            return;
        }
        this.widgetActionName = getProperty("actionName", "TEMPLATE");

        logInfo(() => "INITIAL SETUP");
        init();

        isInitialized = true;
    }

    private T getProperty<T>(string key, T defaultValue) {
        bool result = false;
        T value;

        result = CPH.TryGetArg(key, out value);
        logDebug(() => "{key: " + key + ", value: " + value + ", default: " + defaultValue + "}");

        return result ? (!value.Equals("") ? value : defaultValue) : defaultValue;
    }

    private void logInfo(Func<String> getMessage) {
        CPH.LogInfo("INFO : " + widgetActionName + " :: " + getMessage());
    }

    private void logDebug(Func<String> getMessage) {
        if (!isDebugEnabled) {
            return;
        }

        CPH.LogInfo("DEBUG: " + widgetActionName + " :: " + getMessage());
    }
}