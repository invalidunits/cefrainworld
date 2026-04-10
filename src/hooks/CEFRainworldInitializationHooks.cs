using System;
using System.IO;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;
using Xilium.CefGlue;

namespace CEFRainworld 
{
    public partial class CEFRainworldPlugin
    {
        public static CEFRainworldApp App => _app ?? throw new InvalidOperationException("App not initialized.");
        private static CEFRainworldApp? _app;

        public void InitializationHooks()
        {
            Logger.LogDebug("InitializationHooks!");
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        }        

        void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            Logger.LogDebug("PostModsInit!");
            orig(self);
            
            try
            {
                if (!Init)
                {
                    Init = true;
                    On.Menu.MainMenu.ctor += MainMenu_ctor;
                    On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;


                    // Add Unmanaged DLLs to Search Path
                    string cef = Path.GetFullPath(Path.Combine(ModManager.GetModById("invalidunits.cefdep").basePath, "plugins"));
                    CEFRainworldNative.SetDllDirectoryW(cef);
                    CefRuntime.Load();

                    _app = new CEFRainworldApp();
                    try
                    {
                        var cefArgs = new CefMainArgs(new string[]{});
                        CefRuntime.Initialize(cefArgs, CEFRainworldApp.BuildCEFSettings(),
                            _app, IntPtr.Zero);
                        self.processManager.sideProcesses.Add(new CEFRainworldProcess(self.processManager, _app));
                    }
                    catch
                    {
                        _app = null;
                        CefRuntime.Shutdown();
                        throw;
                    }

                }
            }
            catch (Exception except)
            {
                failedInitialization = except;
                Logger.LogError(except);
            }

        }

        Exception? failedInitialization = null;
        bool showedFailedInitialization = false;

        void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            if (failedInitialization is not null)
            {
                if (!showedFailedInitialization)
                {
                    showedFailedInitialization = true;
                    manager.ShowDialog(new Menu.DialogNotify(
                        $"CEF Rainworld: \n CEF Rainworld has failed to start up. {failedInitialization.Message} {Environment.NewLine} Please restart your game.  {Environment.NewLine} If this message continues to show, please disable the mod before playing.",
                        manager, () => { }));
                }
            }
            else
            {
                float buttonWidth = MainMenu.GetButtonWidth(self.CurrLang);
                Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
                Vector2 size = new Vector2(buttonWidth, 30f);
                self.AddMainMenuButton(new SimpleButton(self, self.pages[0], self.Translate("BROWSE"), "Browse", pos, size), () =>
                {
                    Custom.rainWorld.processManager.RequestMainProcessSwitch(Ext_ProcessID.CEFBrowseMenu);
                }, 0);
            }
        }
        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == Ext_ProcessID.CEFBrowseMenu) self.currentMainLoop = new CEFBrowseMenu(self);
            orig(self, ID);
        }
    }
}
    