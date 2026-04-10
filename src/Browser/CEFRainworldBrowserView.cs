


using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Windows.Input;
using Xilium.CefGlue;
using System.Collections.Generic;
using System.Linq;
using RWCustom;


namespace CEFRainworld
{
    class CEFRainworldBrowserView : FSprite, IDisposable
    {
        private readonly CefBrowserSettings browserSettings;
        private readonly CefWindowInfo window_info;
        
        
        private CefBrowser? _browser;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CefBrowser? TryGetBrowser() => _browser;

        public CefBrowser Browser => _browser ?? InitializeBrowser();
        public bool BrowserInitialized => _browser != null;

        private string _url;
        public string URL
        {
            get
            {
                if (_browser != null)
                {
                    _url = _browser.GetMainFrame().Url;
                }
                return _url;
            }

            set
            {
                _url = value;
                if (_browser != null)
                {
                    _browser.GetMainFrame().LoadUrl(_url);
                }
            }
        }

        public bool Loading => _browser?.IsLoading ?? false;
        private bool _disposedValue;


        private int _windowWidth;
        public int WindowWidth 
        { 
            get => _windowWidth;
            set
            {
                if (_windowWidth != value)
                {
                    _windowWidth = value;
                    _areLocalVerticesDirty = true;
                }
            }
        }

        private int _windowHeight;
        public int WindowHeight 
        { 
            get => _windowHeight;
            set
            {
                if (_windowHeight != value)
                {
                    _windowHeight = value;
                    _areLocalVerticesDirty = true;
                }
            }
        }

        private CEFRainworldInputHandler _InputHandler;
        public CEFRainworldBrowserView(float X, float Y, int windowWidth, int windowHeight, string URL) : base("Futile_White", true)
        {
            this.x = X;
            this.y = Y;
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            _url = URL;
            UpdateLocalVertices();

            // Create instance-specific window_info and browserSettings
            window_info = CefWindowInfo.Create();
            window_info.SetAsWindowless(IntPtr.Zero, true);
            window_info.Width = WindowWidth;
            window_info.Height = WindowHeight;
            window_info.SharedTextureEnabled = false;

            browserSettings = new CefBrowserSettings();
            browserSettings.WindowlessFrameRate = 60;

            _InputHandler = CEFRainworldProcess.Instance.InputHandler;
            _InputHandler.KeyEvent += KeyEvent;
            // Subscribe to centralized mouse events from the input handler
            _InputHandler.MouseMove += OnMouseMove;
            _InputHandler.MouseDown += OnMouseDown;
            _InputHandler.MouseUp += OnMouseUp;
            _InputHandler.MouseWheel += OnMouseWheel;
            ListenForUpdate(Update);
            // CEFRainworldPlugin.GUIEvent += OnGUI;
        }

        bool wasVisible = true;
        public void Update()
        {
            if (_isOnStage && _browser != null)
            {
                bool visible = IsAncestryVisible() && _isOnStage;
                if (visible != wasVisible)
                {
                    wasVisible = visible;
                    _browser.GetHost().WasHidden(!visible);
                }
            }
        }

        private void OnMouseMove(Vector2 stagePos)
        {
            if (_browser == null || !_isOnStage) return;
            var mousePos = GetBrowserPosition(stagePos);
            var cefMouseEvent = new CefMouseEvent(mousePos.x, mousePos.y, default);
            _browser.GetHost().SendMouseMoveEvent(cefMouseEvent, false);
        }

        private void OnMouseDown(Vector2 stagePos, int button, int clickCount)
        {
            if (_browser == null || !_isOnStage) return;

            var mousePos = GetBrowserPosition(stagePos);
            var cefMouseEvent = new CefMouseEvent(mousePos.x, mousePos.y, default);
            _browser.GetHost().SendMouseClickEvent(cefMouseEvent, ConvertButton(button), false, clickCount);
        }

        public Vector2Int GetBrowserPosition(Vector2 stagePos)
        {
            Vector2 localPos = StageToLocal(stagePos);
            float elementLocalX = localPos.x - _localRect.x;
            float elementLocalYFromBottom = localPos.y - _localRect.y;
            float elemW = _localRect.width;
            float elemH = _localRect.height;
            if (elemW <= 0f || elemH <= 0f)
            {
                return new Vector2Int(-1, -1);
            }
            else
            {
                return Vector2Int.RoundToInt(new (Mathf.Clamp01(elementLocalX / elemW) * (float)WindowWidth, 
                    (1.0f - Mathf.Clamp01(elementLocalYFromBottom / elemH)) * (float)WindowHeight));
            }
        }

        private void OnMouseUp(Vector2 stagePos, int button, int clickCount)
        {
            if (_browser == null || !_isOnStage) return;
            var mousePos = GetBrowserPosition(stagePos);
            var cefMouseEvent = new CefMouseEvent(mousePos.x, mousePos.y, default);
            _browser.GetHost().SendMouseClickEvent(cefMouseEvent, ConvertButton(button), true, clickCount);
        }

        private void OnMouseWheel(Vector2 stagePos, Vector2 scroll)
        {
            if (_browser == null || !_isOnStage) return;
            var mousePos = GetBrowserPosition(stagePos);
            var cefMouseEvent = new CefMouseEvent(mousePos.x, mousePos.y, _InputHandler.GetKeyModifiers());
            _browser.GetHost().SendMouseWheelEvent(cefMouseEvent, (int)(scroll.x*120f), -(int)(scroll.y*120f));
        }

        public void KeyEvent(ref CefKeyEvent keyEvent)
        {
            if (_browser != null && _isOnStage)
            {
                _browser.GetHost().SendKeyEvent(keyEvent);
            }   
        }

        // public void OnGUI()
        // {
        //     Event currentEvent = Event.current;
        //     if (currentEvent is not null && _browser != null)
        //     {
                
        //         // Convert Unity mouse position (origin bottom-left) to CEF coordinates (origin top-left),
        //         // taking this view's position, anchor and scale into account.
        //         // We map the stage mouse position into this node's local coordinates (which are in texture/source pixels),
        //         // then convert to normalized coordinates within the element and finally to browser pixel coords.
                
        //         int cefX, cefY;
        //         Vector2 localPos = StageToLocal(Futile.mousePosition);

        //         // localPos is in the same coordinate space as _localRect (pixels relative to anchor/offest)
        //         float elementLocalX = localPos.x - _localRect.x;
        //         float elementLocalYFromBottom = localPos.y - _localRect.y;

        //         float elemW = _localRect.width;
        //         float elemH = _localRect.height;
                
        //         if (elemW <= 0f || elemH <= 0f)
        //         {
        //             cefX = -1;
        //             cefY = -1;
        //         }
        //         else
        //         {
        //             // Normalize within element (0..1). Clamp to avoid sending coords outside the view.
        //             float nx = Mathf.Clamp01(elementLocalX / elemW);
        //             float ny = Mathf.Clamp01(elementLocalYFromBottom / elemH);

        //             // Map to browser pixel dimensions
        //             cefX = Mathf.RoundToInt(nx * (float)WindowWidth);
        //             cefY = Mathf.RoundToInt(ny * (float)WindowHeight);

        //             // Clamp final integer coords within reasonable bounds
        //             cefX = Mathf.Clamp(cefX, 0, Math.Max(0, WindowWidth - 1));
        //             cefY = Mathf.Clamp(cefY, 0, Math.Max(0, WindowHeight - 1));
        //         }

        //         var cefMouseEvent = new CefMouseEvent(cefX, cefY, default);
        //         switch (currentEvent.type)
        //         {
        //             case EventType.MouseMove:
        //             case EventType.MouseDrag:
        //                 _browser.GetHost().SendMouseMoveEvent(cefMouseEvent, false);
        //                 break;
                
        //             case EventType.MouseDown:
        //                 _browser.GetHost().SendMouseClickEvent(cefMouseEvent, ConvertButton(currentEvent.button), false, currentEvent.clickCount);
        //                 break;
    
        //             case EventType.MouseUp:
        //                 _browser.GetHost().SendMouseClickEvent(cefMouseEvent, ConvertButton(currentEvent.button), true, currentEvent.clickCount);
        //                 break;

        //             case EventType.ScrollWheel:
        //                 // Unity's scroll delta is in lines; translate to wheel delta (120 per notch typical)
        //                 int deltaY = (int)(currentEvent.delta.y * 120f);
        //                 int deltaX = (int)(currentEvent.delta.x * 120f);
        //                 _browser.GetHost().SendMouseWheelEvent(cefMouseEvent, deltaX, -deltaY);
        //                 break;

        //             case EventType.KeyDown:
        //                 int rawkeycode = Util.KeyCodes.mapBack[currentEvent.keyCode];
        //                 var keyDown = new CefKeyEvent
        //                 {
        //                     EventType = CefKeyEventType.RawKeyDown,
        //                     WindowsKeyCode = rawkeycode,
        //                     NativeKeyCode = rawkeycode,
        //                     // Character = currentEvent.character,
        //                     // UnmodifiedCharacter = currentEvent.character,
        //                     Modifiers = Util.KeyCodes.ModifiersFromUnityEvent(Event.current.modifiers)
        //                 };

        //                 _browser.GetHost().SendKeyEvent(keyDown);
        //                 if (Util.KeyCodes.keycodetoChar.ContainsKey(currentEvent.keyCode))
        //                 {
        //                     var keyEventChar = new CefKeyEvent
        //                     {
        //                         EventType = CefKeyEventType.Char,
        //                         WindowsKeyCode = rawkeycode,
        //                         NativeKeyCode = rawkeycode,
        //                         Character = currentEvent.character,
        //                         UnmodifiedCharacter = Util.KeyCodes.keycodetoChar[currentEvent.keyCode],
        //                         Modifiers = Util.KeyCodes.ModifiersFromUnityEvent(Event.current.modifiers)
        //                     };
        //                     _browser.GetHost().SendKeyEvent(keyEventChar);
        //                 }
                        
        //                 break;

        //             case EventType.KeyUp:
        //                 var keyUp = new CefKeyEvent
        //                 {
        //                     EventType = CefKeyEventType.KeyUp,
        //                     WindowsKeyCode = (int)Util.KeyCodes.mapBack[currentEvent.keyCode],
        //                     NativeKeyCode = (int)Util.KeyCodes.mapBack[currentEvent.keyCode],
        //                     // Character = currentEvent.character,
        //                     // UnmodifiedCharacter = currentEvent.character
        //                 };
        //                 _browser.GetHost().SendKeyEvent(keyUp);
        //                 break;
        //         }
        //     }
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CefMouseButtonType ConvertButton(int unityButton)
        {
            return unityButton switch
            {
                1 => CefMouseButtonType.Right,
                2 => CefMouseButtonType.Middle,
                _ => CefMouseButtonType.Left,
            };
        }

        FAtlas? _atlas2;
        private CefBrowser InitializeBrowser()
        {
            if (_disposedValue) throw new ObjectDisposedException(GetType().Name);
            var client = CEFRainworldProcess.Instance.Client;

            lock (this)
            {
                if (_browser != null) return _browser;
                _browser = CefBrowserHost.CreateBrowserSync(window_info, client, browserSettings, URL);
            }
            
            lock (_browser)
            {
                _atlas2 = Futile.atlasManager.LoadAtlasFromTexture(
                    $"CEFRainworldBrowserView_{_browser.Identifier}", 
                    client.RenderHandler.SetupViewport(_browser, new RectInt((int)x, (int)y, WindowWidth, WindowHeight)), false);
                
            }

            FAtlasElement element = _atlas2.elements[0];
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                element.uvTopLeft.Set(element.uvRect.xMin, element.uvRect.yMin);
                element.uvTopRight.Set(element.uvRect.xMax, element.uvRect.yMin);
                element.uvBottomRight.Set(element.uvRect.xMax, element.uvRect.yMax);
                element.uvBottomLeft.Set(element.uvRect.xMin, element.uvRect.yMax);
            }

            Init(FFacetType.Quad, element, 1);
            _areLocalVerticesDirty = true;
            client.LifeSpanHandler.OnBrowserClose += BrowserClosed;
            return _browser;
        }

        public void BrowserClosed(CEFRainworldLifeSpanHandler handler, int browser)
        {
            lock (this)
            {
                if (_browser != null)
                {
                    lock (_browser)
                    {
                        if (browser != _browser.Identifier) return;
                        CEFRainworldPlugin.Log.LogDebug($"Browser View Closed: {browser}");
                        _browser.Dispose();
                        _browser = null;
                    }
                } 
            }
            
            handler.OnBrowserClose -= BrowserClosed;
            return;
        }

        public override void HandleAddedToStage()
        {
            base.HandleAddedToStage();

            lock (this)
            {
                if (_browser == null) 
                {
                    wasVisible = IsAncestryVisible() && _isOnStage;
                    InitializeBrowser();
                }
            }
            
        }

        public override void UpdateLocalVertices()
        {
            lock (this)
            {
                if (_browser != null && _areLocalVerticesDirty)
                {
                
                    lock (_browser)
                    {
                        var client = CEFRainworldProcess.Instance.Client;
                        client.RenderHandler.SetupViewport(_browser, new RectInt((int)x, (int)y, WindowWidth, WindowHeight));
                    }
                }
            }
            
            base.UpdateLocalVertices();
        }

        ~CEFRainworldBrowserView() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    var client = CEFRainworldProcess.Instance.Client;

                    lock (this)
                    {
                        if (_browser is not null) 
                        {
                            lock (_browser)
                            {
                                client.RenderHandler.CloseViewport(_browser);
                                _browser.GetHost().CloseBrowser(true);
                            }
                        }
                    }
                }
                
                if (_atlas2 != null)
                {
                    Futile.atlasManager.UnloadAtlas(_atlas2.name);
                    _atlas2 = null;
                }
                
                // CEFRainworldPlugin.GUIEvent -= OnGUI;
                _InputHandler.KeyEvent -= KeyEvent;
                _InputHandler.MouseMove -= OnMouseMove;
                _InputHandler.MouseDown -= OnMouseDown;
                _InputHandler.MouseUp -= OnMouseUp;
                _InputHandler.MouseWheel -= OnMouseWheel;
                RemoveListenForUpdate();
                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
            RemoveListenForUpdate();
        }
    }
}
