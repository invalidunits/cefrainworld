using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldRenderHandler : CefRenderHandler
    {
        public event Action Painted = delegate { }; 

        class Viewport : IDisposable {
            public readonly Texture2D texture;
            public RectInt viewportBounds;

            public Viewport(Texture2D texture, RectInt viewport)
            {
                this.texture = texture;
                this.viewportBounds = viewport;
            }

            ~Viewport() => Dispose();
            public void Dispose()
            {
                UnityEngine.Object.Destroy(texture);
                GC.SuppressFinalize(this);
            }
        }
        
        private ConcurrentDictionary<int, Viewport> renderViewports;
        public CEFRainworldRenderHandler()
        {
            renderViewports = new();
        }
    
        public Texture2D SetupViewport(CefBrowser browser, RectInt viewportBounds)
        {

            if (viewportBounds.width <= 0) 
            {
                CEFRainworldPlugin.Log.LogError(viewportBounds.width.ToString());
                throw new ArgumentOutOfRangeException("width");
            }
            if (viewportBounds.height <= 0) 
            {
                CEFRainworldPlugin.Log.LogError(viewportBounds.height.ToString());
                throw new ArgumentOutOfRangeException("height");
            }
            
            Viewport viewport;
            renderViewports.TryGetValue(browser.Identifier, out viewport);
            if (viewport == default)
            {
                viewport = new Viewport(new Texture2D(viewportBounds.width, viewportBounds.height, TextureFormat.BGRA32, false), viewportBounds);
                renderViewports.TryAdd(browser.Identifier, viewport);
                return viewport.texture;   
            }

            lock (viewport)
            {
                if (viewport.viewportBounds.width != viewportBounds.width || viewport.viewportBounds.height != viewportBounds.height)
                {
                    // Resize to the new requested size (was incorrectly using the old bounds)
                    viewport.texture.Resize(viewportBounds.width, viewportBounds.height);
                    browser.GetHost().WasResized();
                }

                viewport.viewportBounds = viewportBounds;
                return viewport.texture;
            }
        }

        public void CloseViewport(CefBrowser browser)
        {
            Viewport viewport;
            lock (renderViewports) renderViewports.Remove(browser.Identifier, out viewport);

            if (viewport != default)
            {
                lock (viewport)
                {
                    viewport.Dispose();
                }
            }
        }

        protected override CefAccessibilityHandler GetAccessibilityHandler() => null!;
        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            screenInfo.Depth = 32*4;
            screenInfo.DepthPerComponent = 32;
            screenInfo.IsMonochrome = false;
            return true;
        }

        protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
        {
            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            if (hWnd == IntPtr.Zero) return false;

            CEFRainworldNative.RECT native_rect = new();
            if (!CEFRainworldNative.GetWindowRect(hWnd, out native_rect)) return false;

            rect = new CefRectangle(native_rect.Left, native_rect.Top, Futile.screen.pixelWidth, Futile.screen.pixelHeight);
            return true;
        }

        protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
        {
            Viewport viewport;
            renderViewports.TryGetValue(browser.Identifier, out viewport);
            if (viewport != default)
            {
                rect = new CefRectangle(viewport.viewportBounds.x, viewport.viewportBounds.y, viewport.viewportBounds.width, viewport.viewportBounds.height);
            }
            else
            {
                rect = new CefRectangle(0, 0, 64, 64);
                CEFRainworldPlugin.Log.LogWarning($"GetViewRect: Texture not initialized for browser {browser.Identifier}");
            }
        }

        protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
        {
            CEFRainworldPlugin.Log.LogWarning($"OnAcceleratedPaint called but not handled; browser {browser.Identifier}");
            return;
        }
        
        protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
        {
            //TODO: grab focus from whatever native element has it.
            return;
        }

        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            CEFRainworldPlugin.Log.LogWarning("OnPaint");
            Viewport viewport;
            renderViewports.TryGetValue(browser.Identifier, out viewport);

            if (viewport != default)
            {
                lock (viewport)
                {
                    // If either dimension differs, warn and skip
                    if (width != viewport.viewportBounds.width || height != viewport.viewportBounds.height)
                    {
                        CEFRainworldPlugin.Log.LogWarning($"{viewport.viewportBounds} vs {width}x{height}");
                        CEFRainworldPlugin.Log.LogWarning($"OnPaint: Texture initialized to wrong size {browser.Identifier}");
                        return;
                    }

                    viewport.texture.LoadRawTextureData(buffer, width * height * 4);
                    viewport.texture.Apply();
                }
            }
        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
        {
        }

        protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
        {
        }
    }
}