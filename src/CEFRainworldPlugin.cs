using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Xilium.CefGlue;

namespace CEFRainworld 
{

    [BepInPlugin("invalidunits.cefdep", "CEF Rainworld Dependency", "1.0.0")]
    public sealed partial class CEFRainworldPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log => Instance.Logger;
        public static bool Init = false;

    
        public static CEFRainworldPlugin? _instance;
        public static CEFRainworldPlugin Instance => _instance ?? throw CEFRainworldExceptions.NotInitializedException();

        public CEFRainworldPlugin() : base()
        {
        }

        public void OnEnable()
        {
            try
            {
                _instance = this;
                InitializationHooks();
            }
            catch (Exception except)
            {
                Logger.LogError(except);
            }
            
        }

        public static event Action GUIEvent = delegate { };
        public void OnGUI()
        {
            GUIEvent();
        }

        public static event Action OnClose = delegate { };
        public void OnApplicationQuit()
        {
            try
            {
                
                if (_app != null)
                {
                    OnClose();
                    _app = null;
                    CEFRainworldProcess.Instance.manager.StopSideProcess(CEFRainworldProcess.Instance);
                    CefRuntime.Shutdown();
                }
            }
            catch (Exception except)
            {
                Logger.LogError(except);
            }
        }
    }
}
