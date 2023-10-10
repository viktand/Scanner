using Saraff.Twain;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DriverScanner

{
    public class ProcessMG
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }

        public struct Input
        {
            public int type;
            public InputUnion u;
        }       

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();     
       
        public void CheckAndClose()
        {
            Process[] localByName = Process.GetProcessesByName("CustomLanguage");
            if(localByName.Length > 0)
            {
                foreach (Process process in localByName)
                {
                    Console.WriteLine($"Close prosess {process.Id} - twain");
                    Thread.Sleep(1000);                  

                    Input[] inputs = new Input[]
                    {
                        new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = 0x0F, // right
                                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        },
                        new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = 0x0F, // right
                                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        },
                         new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = 0x1C, // right
                                    dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        },
                        new Input
                        {
                            type = (int)InputType.Keyboard,
                            u = new InputUnion
                            {
                                ki = new KeyboardInput
                                {
                                    wVk = 0,
                                    wScan = 0x1C, // right
                                    dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                                    dwExtraInfo = GetMessageExtraInfo()
                                }
                            }
                        }
                    };
                    _ = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));                 
                }
            }        
        }
    }
}
