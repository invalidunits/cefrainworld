
using System;
using RWCustom;
using UnityEngine;
using Xilium.CefGlue;

namespace CEFRainworld
{
    public class CEFRainworldInputHandler
    {
        const int VK_SHIFT = 0x10;
        const int VK_CONTROL = 0x11;
        const int VK_MENU = 0x12;
        const int VK_CAPITAL = 0x14;
        const int VK_LSHIFT = 0xA0;
        const int VK_RSHIFT = 0xA1;
        const int VK_LCONTROL = 0xA2;
        const int VK_RCONTROL = 0xA3;
        const int VK_LMENU = 0xA4;
        const int VK_RMENU = 0xA5;
        const int VK_LWIN = 0x5B;
        const int VK_RWIN = 0x5C;
        const int VK_INSERT  = 0x2D;
        const int VK_DELETE  = 0x2E;
        const int VK_HOME  = 0x24;
        const int VK_END  = 0x23;
        const int VK_PRIOR  = 0x21; 
        const int VK_NEXT  = 0x22; 
        const int VK_LEFT  = 0x25;
        const int VK_UP  = 0x26;
        const int VK_RIGHT  = 0x27;
        const int VK_DOWN  = 0x28;
        const int VK_DIVIDE  = 0x6F; 
        
        private static bool IsExtendedKey(int vk)
        {
            switch (vk)
            {
                case VK_RCONTROL: 
                case VK_RMENU: 
                case VK_INSERT: 
                case VK_DELETE:
                case VK_HOME:
                case VK_END: 
                case VK_PRIOR: 
                case VK_NEXT: 
                case VK_LEFT: 
                case VK_UP: 
                case VK_RIGHT: 
                case VK_DOWN: 
                case VK_DIVIDE: 
                    return true;
                default:
                    return false;
            }
        }
        public const int KeyCount = 256;

        public delegate void KeyEventDelegate(ref CefKeyEvent keyEvent);
        public event KeyEventDelegate KeyEvent = delegate { };

        byte[] keyStates = new byte[KeyCount];
        byte[] prevkeyStates = new byte[KeyCount];
        private float[] keyRepeatTime = new float[KeyCount];
        private ushort[] keyRepeat = new ushort[KeyCount];


        private readonly float keyRepeatDelay = 0.5f;
        private readonly float keyRepeatInterval = 0.05f;

        // Mouse
        public event Action<Vector2> MouseMove = delegate { };
        public event Action<Vector2, int, int> MouseDown = delegate { };
        public event Action<Vector2, int, int> MouseUp = delegate { };
        public event Action<Vector2, Vector2> MouseWheel = delegate { };

        private bool[] prevMouseButtons = new bool[3];
        private Vector2 prevStagePos = new Vector2(float.MinValue, float.MinValue);
        private float[] ClickTime = new float[3];
        private Vector2[] ClickPos = new Vector2[3];
        private int[] ClickCount = new int[3];
        private readonly float doubleClickTime = 0.5f; // seconds
        private readonly float doubleClickMaxDist = 6f; // pixels

        public void Update(float delta)
        {
            PollKeyboard(delta);
            PollMouse(delta);
        }

        public void PollKeyboard(float delta)
        {
            (prevkeyStates, keyStates) = (keyStates, prevkeyStates);
            unsafe
            {
                fixed (byte *p_data = keyStates)
                {
                    // GetKeyboardState returns non-zero on success.
                    if (!CEFRainworldNative.GetKeyboardState(p_data))
                    {
                        CEFRainworldPlugin.Log.LogWarning("Failed to get keyboard state");
                        return;
                    }
                }
            }

        
            byte[] unmodStates = (byte[])keyStates.Clone(); 
            unmodStates[VK_SHIFT] &= 0x7F;
            unmodStates[VK_LSHIFT] &= 0x7F;
            unmodStates[VK_RSHIFT] &= 0x7F;

            CefEventFlags modifiers = GetKeyModifiers(keyStates);
            for (int vk = 0; vk < KeyCount; vk++)
            {
                bool prevDown = (prevkeyStates[vk] & 0x80) != 0;
                bool curDown = (keyStates[vk] & 0x80) != 0;

                // LParam WM_CHAR message
                // bits 0-15: repeat count (use 1)
                // bits 16-23: scan code
                // bit 24: extended-key flag
                // bit 29: context code (Alt down)
                // bit 30: previous key state (0 for keydown)
                // bit 31: transition state (0 for keydown)
                int scan = (int)CEFRainworldNative.MapVirtualKey((uint)vk, 0);
                int nativekeyinput = 0;
                nativekeyinput |= 1; // repeat count = 1
                nativekeyinput |= (scan & 0xFF) << 16;
                if (IsExtendedKey(vk)) nativekeyinput |= 1 << 24;
                if ((keyStates[VK_MENU] & 0x80) != 0) nativekeyinput |= 1 << 29; // context (ALT)
                nativekeyinput |= (prevDown? 1 : 0) << 30; // previous key state
                nativekeyinput |= (curDown? 0 : 1) << 31; // transition state 

                if (curDown)
                {
                    CefKeyEvent keyDown = new CefKeyEvent
                    {
                        EventType = CefKeyEventType.RawKeyDown,
                        WindowsKeyCode = vk,
                        NativeKeyCode = nativekeyinput,
                        Modifiers = modifiers
                    };

                    char chWithMods = GetCharFromKey((byte)vk, keyStates);
                    CefKeyEvent? keyChar = null;
                    if (chWithMods != '\0')
                    {
                        char chUnmodified = GetCharFromKey((byte)vk, unmodStates);
                        keyChar = new CefKeyEvent
                        {
                            EventType = CefKeyEventType.Char,
                            WindowsKeyCode = chWithMods,
                            NativeKeyCode = nativekeyinput,
                            Modifiers = modifiers,
                            Character = chWithMods,
                            UnmodifiedCharacter = chUnmodified == '\0' ? chWithMods : chUnmodified,
                        };
                    }

                    
                    if (!prevDown)
                    {
                        KeyEvent(ref keyDown);
                        if (keyChar is not null) KeyEvent(ref keyChar);
                        keyRepeatTime[vk] = Mathf.Lerp(0f, delta, 0.5f);
                        keyRepeat[vk] = 1;
                    }
                    else 
                    {
                        keyRepeatTime[vk] += delta;
                        while (keyRepeatTime[vk] >= (keyRepeatDelay + keyRepeatInterval))
                        {
                            keyRepeatTime[vk] -= keyRepeatInterval;
                            keyRepeat[vk] += 1;

                            int repeatnative = nativekeyinput | (keyRepeat[vk]) & 0xff;
                            keyDown.NativeKeyCode = repeatnative;

                            KeyEvent(ref keyDown);
                            if (keyChar is not null) 
                            {
                                keyChar.NativeKeyCode = repeatnative;
                                KeyEvent(ref keyChar);
                            }
                        }
                    }
                }

                if (prevDown && !curDown)
                {
                    var keyUp = new CefKeyEvent
                    {
                        EventType = CefKeyEventType.KeyUp,
                        WindowsKeyCode = vk,
                        NativeKeyCode = nativekeyinput,
                        Modifiers = modifiers
                    };

                    KeyEvent(ref keyUp);
                    keyRepeatTime[vk] = 0f;
                    keyRepeat[vk] = 0;
                }
            }
        }

        public void PollMouse(float delta)
        {
            Vector2 stagePos = Futile.mousePosition;
            if (!Custom.DistLess(prevStagePos, stagePos, 1f))
            {
                MouseMove(stagePos);
                prevStagePos = stagePos;
            }

            for (int b = 0; b < 3; b++)
            {
                bool isDown = Input.GetMouseButton(b);
                if (isDown && !prevMouseButtons[b])
                {
                    ClickTime[b] += delta;
                    float dist = (stagePos - ClickPos[b]).magnitude;
                    if (ClickCount[b] == 0 || (ClickTime[b] <= doubleClickTime && dist <= doubleClickMaxDist))
                    {
                        ClickTime[b] = 0;
                        ClickCount[b] = (ClickCount[b] + 1) % 3;
                    }
    
                    ClickPos[b] = stagePos;
                    MouseDown(stagePos, b, ClickCount[b]);
                }
                else if (!isDown && prevMouseButtons[b])
                {
                    MouseUp(stagePos, b, ClickCount[b]);
                    ClickCount[b] = 0;
                }

                prevMouseButtons[b] = isDown;
            }

            Vector2 scroll = Input.mouseScrollDelta;
            if (Mathf.Abs(scroll.x) > 0f || Mathf.Abs(scroll.y) > 0f)
            {
                MouseWheel(stagePos, -scroll);
            }
        }

        public CefEventFlags GetKeyModifiers(byte[]? keyStates = null)
        {
            if (keyStates == null) keyStates = this.keyStates;
            CefEventFlags mods = 0;


            if ((keyStates[VK_SHIFT] & 0x80) != 0 || (keyStates[VK_LSHIFT] & 0x80) != 0 || (keyStates[VK_RSHIFT] & 0x80) != 0)
                mods |= CefEventFlags.ShiftDown;
            if ((keyStates[VK_CONTROL] & 0x80) != 0 || (keyStates[VK_LCONTROL] & 0x80) != 0 || (keyStates[VK_RCONTROL] & 0x80) != 0)
                mods |= CefEventFlags.ControlDown;
            if ((keyStates[VK_MENU] & 0x80) != 0 || (keyStates[VK_LMENU] & 0x80) != 0 || (keyStates[VK_RMENU] & 0x80) != 0)
                mods |= CefEventFlags.AltDown;
            if ((keyStates[VK_LWIN] & 0x80) != 0 || (keyStates[VK_RWIN] & 0x80) != 0)
                mods |= CefEventFlags.CommandDown;

            // Toggle states
            if ((keyStates[VK_CAPITAL] & 0x01) != 0)
                mods |= CefEventFlags.CapsLockOn;

            return mods;
        }

        private char GetCharFromKey(byte vk, byte[] keyStates)
        {
            try
            {
                // Map virtual-key to scan code
                uint scan = CEFRainworldNative.MapVirtualKey(vk, 0);
                var sb = new System.Text.StringBuilder(4);
                int rc = CEFRainworldNative.ToUnicode(vk, scan, keyStates, sb, sb.Capacity, 0);
                if (rc > 0)
                {
                    return sb[0];
                }
            }
            catch (Exception)
            {
                // best-effort: ignore
            }
            return '\0';
        }
    }
}