using System;

// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
public static class CPH { //Mock CPH to ignore UNKNOWN reference for extensions
    public static void LogInfo(string message) {
        Console.WriteLine($"[GlobalCPH LogInfo]: {message}");
    }

    public static void RegisterCustomTrigger(string anyConnected, string anyConnectedEvent, string[] p2) {
        throw new NotImplementedException();
    }

    public static void SetArgument(string deviceName, object p1) {
        throw new NotImplementedException();
    }

    public static void TriggerCodeEvent(string eventName) {
        throw new NotImplementedException();
    }
}