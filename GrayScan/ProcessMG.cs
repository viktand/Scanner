using Saraff.Twain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrayScan
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

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        //public ProcessMG()
        //{
        //    SetCursorPos(726, 564);
        //}

        public void CheckAndClose(Twain32 scaner)
        {
            Process[] localByName = Process.GetProcessesByName("CustomLanguage");
            if(localByName.Length > 0)
            {
                Console.WriteLine($"Count processes: {localByName.Length}");

                foreach (Process process in localByName)
                {
                    Console.WriteLine($"Close prosess {process.Id} - twain");
                    Thread.Sleep(1000);
                    //process.Dispose();
                    //process.Close();

                    //scaner.CloseDataSource();
                    //scaner.Dispose();
                    //process.Kill();
                    Console.WriteLine("Press...");
                    //ActivateApp("CustomLanguage"); // вызов окна 
                    //SendKeys.Send("{RIGHT}"); // отправка нажатия
                    //SendKeys.Send("{ENTER}");

                    //getting notepad's process | at least one instance of notepad must be running
                    //var process1 = Process.GetProcessesByName("notepad")[0];

                    //getting notepad's textbox handle from the main window's handle
                    //the textbox is called 'Edit'
                    //IntPtr box = FindWindowEx(process.MainWindowHandle, IntPtr.Zero, "Edit", null);
                    //sending the message to the textbox
                    //SendMessage(box, WM_SETTEXT, 0, "1131313");
                    //IntPtr val = new IntPtr((Int32)'A');
                    //PostMessage(box, VK_ENTER, new IntPtr(VK_RETURN), new IntPtr(0));

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
                    var result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
                    Console.WriteLine($"Input result = {result}");
                    //GetCursorPos(out POINT point);
                    //Console.WriteLine(point.X);
                    //Console.WriteLine(point.Y);
                    //SetCursorPos(726, 564);
                }
            }        
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        //include FindWindowEx
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        //include SendMessage
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        //this is a constant indicating the window that we want to send a text message
        const int WM_SETTEXT = 0X000C;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        const Int32 WM_CHAR = 0x0102;
        const Int32 VK_RETURN = 0x0D;
        const int VK_ENTER = 0x0D;


        void ActivateApp(string processName)
        {
            Process[] g = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (g.Count() > 0)
                SetForegroundWindow(g[0].MainWindowHandle);
        }
    }
}
