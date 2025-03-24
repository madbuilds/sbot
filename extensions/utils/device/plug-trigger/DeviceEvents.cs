// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS0114

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

/**
 * [DEVICE] EVENT
 */
// ReSharper disable once UnusedType.Global
public class CPHInline_DeviceEvents : CPHInlineBase {

    private void init() {
        registerTriggers("CONNECTED");
        registerTriggers("DISCONNECTED");
        
        var watcherThread = new Thread(() => {
            var deviceWatcher = new DeviceWatcher(CPH);
            Application.Run(deviceWatcher);
        });
        watcherThread.SetApartmentState(ApartmentState.STA);
        watcherThread.Start();
    }

    private void registerTriggers(string action) {
        CPH.RegisterCustomTrigger($"ANY: {action}", DeviceWatcher.ANY_EVENT + action,    new [] { "System", "onDeviceEvent" });

        CPH.RegisterCustomTrigger($"USB: {action}",          DeviceWatcher.USB_EVENT,       new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"STORAGE: {action}",      DeviceWatcher.STORAGE_EVENT,   new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"CAMERA: {action}",       DeviceWatcher.CAMERA_EVENT,    new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"AUDIO: {action}",        DeviceWatcher.AUDIO_EVENT,     new [] { "System", "onDeviceEvent" });
        
        CPH.RegisterCustomTrigger($"HID: {action}",          DeviceWatcher.HID_EVENT,       new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"MOUSE: {action}",        DeviceWatcher.MOUSE_EVENT,     new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"CONTROLLER: {action}",   DeviceWatcher.CONTROLLER_EVENT,new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"KEYBOARD: {action}",     DeviceWatcher.KEYBOARD_EVENT,  new [] { "System", "onDeviceEvent" });
        
        CPH.RegisterCustomTrigger($"BLUETOOTH: {action}",    DeviceWatcher.BLUETOOTH_EVENT, new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"PRINTER: {action}",      DeviceWatcher.PRINTER_EVENT,   new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"SCANNER: {action}",      DeviceWatcher.SCANNER_EVENT,   new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"NETWORK: {action}",      DeviceWatcher.NETWORK_EVENT,   new [] { "System", "onDeviceEvent" });
        CPH.RegisterCustomTrigger($"MODEM: {action}",        DeviceWatcher.MODEM_EVENT,     new [] { "System", "onDeviceEvent" });
    }

    private bool process() {
        DEBUG(() => "START DEVICE EVENT WATCHER");
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

    private void setUp() {
        if (isInitialized) {
            return;
        }
        widgetActionName = getProperty("actionName", "TEMPLATE");

        INFO(() => "INITIAL SETUP");
        init();

        isInitialized = true;
    }

    // ReSharper disable once UnusedMember.Local
    private T getProperty<T>(string key, T defaultValue) {
        var result = CPH.TryGetArg(key, out T value);
        DEBUG(() => "{key: " + key + ", value: " + value + ", default: " + defaultValue + "}");

        return result ? 
            !value.Equals("") ? 
                value 
                : defaultValue 
            : defaultValue;
    }
    
    // ReSharper disable once UnusedMember.Local
    private void DEBUG(Func<string> getMessage) {
        if (!isDebugEnabled) {
            return;
        }

        CPH.LogInfo("DEBUG: " + widgetActionName + " :: " + getMessage);
    }
    
    // ReSharper disable once UnusedMember.Local
    private void INFO(Func<string> getMessage) {
        CPH.LogInfo("INFO : " + widgetActionName + " :: " + getMessage);
    }

    // ReSharper disable once UnusedMember.Local
    private void WARN(Func<string> getMessage) {
        CPH.LogWarn("WARN : " + widgetActionName + " :: " + getMessage);
    }
    
    // ReSharper disable once UnusedMember.Local
    private void ERROR(Func<string> getMessage) { 
        CPH.LogError("ERROR: " + widgetActionName + " :: " + getMessage);
    }
}

public class DeviceWatcher : Form {
    //Windows event messages
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

    // Device Interface GUIDs
    private static readonly Guid GUID_USB_DEVICE = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");
    private static readonly Guid GUID_STORAGE = new("53F56307-B6BF-11D0-94F2-00A0C91EFB8B");
    private static readonly Guid GUID_CAMERA = new("E5323777-F976-4F5B-9B55-B94699C46E44");
    private static readonly Guid GUID_AUDIO = new("E6327CAD-DCEC-4949-AE8A-991E976A79D2");

    private static readonly Guid GUID_HID = new("4D1E55B2-F16F-11CF-88CB-001111000030");
    private static readonly Guid GUID_MOUSE = new("6F1D2B60-D5A0-11CF-BFC7-444553540000");
    private static readonly Guid GUID_GAMEPAD = new("6F1D2B70-D5A0-11CF-BFC7-444553540000");
    private static readonly Guid GUID_KEYBOARD = new("6F1D2B61-D5A0-11CF-BFC7-444553540000");

    private static readonly Guid GUID_BLUETOOTH = new("E0CBF06C-CD8B-4647-BB8A-263B43F0F974");
    private static readonly Guid GUID_PRINTER = new("28D78FAD-5A12-11D1-AE5B-0000F803A8C2");
    private static readonly Guid GUID_SCANNER = new("6BDD1FC6-810F-11D0-BEC7-08002BE2092F");
    private static readonly Guid GUID_NETWORK = new("CAC88484-7515-4C03-82E6-71A87ABAC361");
    private static readonly Guid GUID_MODEM = new("2C7089AA-2E0E-11D1-B114-00C04FC2AAE4");

    // Events
    public const string USB_EVENT           = "system.event.usb.";
    public const string STORAGE_EVENT       = "system.event.storage.";
    public const string CAMERA_EVENT        = "system.event.camera.";
    public const string AUDIO_EVENT         = "system.event.audio.";

    public const string HID_EVENT           = "system.event.hid.";
    public const string MOUSE_EVENT         = "system.event.mouse.";
    public const string CONTROLLER_EVENT    = "system.event.controller.";
    public const string KEYBOARD_EVENT      = "system.event.keyboard.";

    public const string BLUETOOTH_EVENT     = "system.event.bluetooth.";
    public const string PRINTER_EVENT       = "system.event.printer.";
    public const string SCANNER_EVENT       = "system.event.scanner.";
    public const string NETWORK_EVENT       = "system.event.network.";
    public const string MODEM_EVENT         = "system.event.modem.";
    public const string ANY_EVENT           = "system.event.any.";
    
    private readonly IInlineInvokeProxy CPH;
        
    [StructLayout(LayoutKind.Sequential)]
    private struct DEV_BROADCAST_DEVICEINTERFACE {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public short dbcc_name;
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterDeviceNotification(IntPtr Handle);
    
    private IntPtr notificationHandle;
    
    public DeviceWatcher(IInlineInvokeProxy CPH) {
        this.CPH = CPH;
        RegisterForDeviceNotifications();
    }

    private void RegisterForDeviceNotifications() {
        DEV_BROADCAST_DEVICEINTERFACE dbdi = new() {
            dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
            dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
        };

        var buffer = Marshal.AllocHGlobal(dbdi.dbcc_size);
        Marshal.StructureToPtr(dbdi, buffer, true);
        notificationHandle = RegisterDeviceNotification(this.Handle, buffer, 0);
        Marshal.FreeHGlobal(buffer);
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        if (notificationHandle != IntPtr.Zero) {
            UnregisterDeviceNotification(notificationHandle);
            notificationHandle = IntPtr.Zero;
        }
        base.OnFormClosed(e);
    }
    
    protected override void WndProc(ref Message m) {
        if (m.Msg != WM_DEVICECHANGE) {
            base.WndProc(ref m);
            return;
        }

        var eventType = m.WParam.ToInt32();
        if (eventType is not (DBT_DEVICEARRIVAL or DBT_DEVICEREMOVECOMPLETE)) {
            base.WndProc(ref m);
            return;
        }

        var action = eventType switch {
            DBT_DEVICEARRIVAL => "CONNECTED",
            DBT_DEVICEREMOVECOMPLETE => "DISCONNECTED",
            _ => throw new ArgumentOutOfRangeException()
        };
        var dbdi = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
        var deviceType = dbdi.dbcc_classguid switch {
            _ when dbdi.dbcc_classguid == GUID_USB_DEVICE => "USB",
            _ when dbdi.dbcc_classguid == GUID_STORAGE    => "STORAGE",
            _ when dbdi.dbcc_classguid == GUID_CAMERA     => "CAMERA",
            _ when dbdi.dbcc_classguid == GUID_AUDIO      => "AUDIO",
            
            _ when dbdi.dbcc_classguid == GUID_HID        => "HID",
            _ when dbdi.dbcc_classguid == GUID_MOUSE      => "MOUSE",
            _ when dbdi.dbcc_classguid == GUID_GAMEPAD    => "GAMEPAD",
            _ when dbdi.dbcc_classguid == GUID_KEYBOARD   => "KEYBOARD",
            
            _ when dbdi.dbcc_classguid == GUID_BLUETOOTH  => "BLUETOOTH",
            _ when dbdi.dbcc_classguid == GUID_PRINTER    => "PRINTER",
            _ when dbdi.dbcc_classguid == GUID_SCANNER    => "SCANNER",
            _ when dbdi.dbcc_classguid == GUID_NETWORK    => "NETWORK",
            _ when dbdi.dbcc_classguid == GUID_MODEM      => "MODEM",
            _ => "UNKNOWN"
        };
        
        var deviceName = GetDeviceName(m.LParam);
        
        CPH.SetArgument("device.class", dbdi.dbcc_classguid);
        CPH.SetArgument("device.type", deviceType);
        CPH.SetArgument("device.name", deviceName);
        CPH.SetArgument("device.action", action);
        
        if        (dbdi.dbcc_classguid == GUID_USB_DEVICE) {  CPH.TriggerCodeEvent(USB_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_STORAGE) {     CPH.TriggerCodeEvent(STORAGE_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_CAMERA) {      CPH.TriggerCodeEvent(CAMERA_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_AUDIO) {       CPH.TriggerCodeEvent(AUDIO_EVENT + action);
             
        } else if (dbdi.dbcc_classguid == GUID_HID) {         CPH.TriggerCodeEvent(HID_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_MOUSE) {       CPH.TriggerCodeEvent(MOUSE_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_GAMEPAD) {     CPH.TriggerCodeEvent(CONTROLLER_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_KEYBOARD) {    CPH.TriggerCodeEvent(KEYBOARD_EVENT + action);
            
        } else if (dbdi.dbcc_classguid == GUID_BLUETOOTH) {   CPH.TriggerCodeEvent(BLUETOOTH_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_PRINTER) {     CPH.TriggerCodeEvent(PRINTER_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_SCANNER) {     CPH.TriggerCodeEvent(SCANNER_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_NETWORK) {     CPH.TriggerCodeEvent(NETWORK_EVENT + action);
        } else if (dbdi.dbcc_classguid == GUID_MODEM) {       CPH.TriggerCodeEvent(MODEM_EVENT + action);
        }                                                     CPH.TriggerCodeEvent(ANY_EVENT + action);

        base.WndProc(ref m);
    }
    
    private string GetDeviceName(IntPtr lParam) {
        try {
            var namePtr = new IntPtr(lParam.ToInt64() + Marshal.OffsetOf(typeof(DEV_BROADCAST_DEVICEINTERFACE), "dbcc_name").ToInt64());
            var deviceName = Marshal.PtrToStringAuto(namePtr);

            return string.IsNullOrEmpty(deviceName) ? "UNKNOWN" : deviceName;
        }
        catch {
            return "UNKNOWN";
        }
    }
}