using UnityEngine;
using System.Runtime.InteropServices;

public static class JavaScriptBridge
{
    [DllImport("__Internal")]
    public static extern void UnityIsReady();
}