using System;
using System.Drawing;
using System.IO;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldProcess : MainLoopProcess
    {
        
        public static CEFRainworldProcess Instance => _instance ?? throw CEFRainworldExceptions.NotInitializedException();
        private static CEFRainworldProcess? _instance;


        private Lazy<CEFRainworldInputHandler> _InputHandler;
        public CEFRainworldInputHandler InputHandler => _InputHandler.Value;

        public CEFRainworldClient Client { get; }
        public CEFRainworldApp App { get; }
        public CEFRainworldProcess(ProcessManager manager, CEFRainworldApp App) : base(manager, CEFRainworldPlugin.Ext_ProcessID.CEFAppManager)
        {
            this.App = App;
            this.Client = new CEFRainworldClient();
            _InputHandler = new Lazy<CEFRainworldInputHandler>(System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            _instance = this;
        }

        public override void RawUpdate(float dt)
        {
            base.RawUpdate(dt);
            CefRuntime.DoMessageLoopWork();
            if (_InputHandler.IsValueCreated)
            {
                _InputHandler.Value.Update(dt);
            }
        }

        public override void ShutDownProcess()
        {
            CefRuntime.QuitMessageLoop();
            base.ShutDownProcess();
        }
    }

    sealed public class CEFRainworldApp : CefApp
    {
        public static CefSettings BuildCEFSettings()
        {
            string basePath = ModManager.GetModById("invalidunits.cefdep").basePath;
            string service = Path.GetFullPath(Path.Combine(basePath, "plugins/Xilium.CefGlue.BrowserProcess.exe"));
            var tmpDir = Path.GetFullPath(Path.Combine(basePath, "tmp"));
            if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);
            string log = Path.GetFullPath(Path.Combine(tmpDir, "tmp/cef.log"));
            string cache = Path.GetFullPath(Path.Combine(tmpDir, "tmp/cache"));
            var settings = new CefSettings()
            {
                // Allow command line args so we can disable GPU features that cause crashes on some platforms
                CommandLineArgsDisabled = false,
                MultiThreadedMessageLoop = false,
                BackgroundColor = new CefColor(0, 0, 0, 0),
                LogSeverity = CefLogSeverity.Verbose,
                LogFile = log,
                CachePath = cache,
                WindowlessRenderingEnabled = true,
                NoSandbox = true,                
                BrowserSubprocessPath=service,
            };

            
            return settings;
        }

        private CEFRainworldBrowserProcessHandler _browserProcessHandler;
        private CEFRainworldRenderProcessHandler _renderHandler;

        public CEFRainworldApp()
        {
            _browserProcessHandler = new CEFRainworldBrowserProcessHandler();
            _renderHandler = new CEFRainworldRenderProcessHandler();
        }

        protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return _browserProcessHandler;
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return _renderHandler;
        }
    }
}