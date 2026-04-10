using System;
using BepInEx.Logging;
using UnityEngine;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldDisplayHandler : CefDisplayHandler
    {
        public CEFRainworldDisplayHandler()
        {
            
        }

        protected override bool OnConsoleMessage(CefBrowser browser, CefLogSeverity level, string message, string source, int line)
        {
            CEFRainworldPlugin.Log.Log(level switch
            {
                CefLogSeverity.Debug => LogLevel.Debug,
                CefLogSeverity.Info => LogLevel.Info,
                CefLogSeverity.Warning => LogLevel.Warning,
                CefLogSeverity.Error => LogLevel.Error,
                CefLogSeverity.Fatal => LogLevel.Fatal,
                _ => LogLevel.None,
            }, $"{source}:{line}: {message}");
            return true;
        }
    }
}