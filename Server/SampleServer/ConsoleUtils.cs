/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 *  
 * ======================================================================*/
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace SampleServer
{
    /// <summary>
    /// Utilities for handling OS specific console behaviors
    /// </summary>
    static class ConsoleUtils
    {
        #region Windows OS Specific
        public static class WindowsConsoleUtils
        {
            #region Private Methods

            /// <summary>
            /// Retrieve last error message
            /// </summary>
            /// <returns></returns>
            private static string GetErrorMessage()
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = new Win32Exception(error).Message;
                return errorMessage;
            }

            #endregion

            #region Public Static Classes

            public static class WindowsConsole
            {
                #region Private Constants
                private const int StdInputHandle = -10;

                /// <summary>
                /// This flag enables the user to use the mouse to select and edit stringValue. To enable
                /// this option, you must also set the ExtendedFlags flag.
                /// </summary>
                private const int QuickEditMode = 64;

                /// <summary>
                /// ExtendedFlags must be enabled in order to enable QuickEditMode.
                /// </summary>
                private const int ExtendedFlags = 128;
                #endregion

                #region Private External Declarations

                [DllImport("Kernel32.dll", SetLastError = true)]
                private static extern IntPtr GetStdHandle(int stdHandle);

                [DllImport("kernel32.dll", SetLastError = true)]
                private static extern bool GetConsoleMode(
                    IntPtr hConsoleHandle,
                    out int lpMode);

                [DllImport("kernel32.dll", SetLastError = true)]
                private static extern bool SetConsoleMode(
                    IntPtr hConsoleHandle,
                    int ioMode);

                #endregion

                #region Public Methods

                /// <summary>
                /// Convenience method for disabling the mouse select in the Console,
                /// which if left selected leads to blocking all running threads that write to the console.
                /// </summary>
                public static void DisableQuickEdit()
                {
                    IntPtr conHandle = GetStdHandle(StdInputHandle);
                    int mode;

                    if (!GetConsoleMode(conHandle, out mode))
                    {
                        Console.WriteLine(String.Format("Error getting the console mode: {0}", GetErrorMessage()));
                        return;
                    }

                    mode &= ~(QuickEditMode | ExtendedFlags);

                    if (!SetConsoleMode(conHandle, mode))
                    {
                        // error getting the console mode. Exit.
                        Console.WriteLine(String.Format("Error setting the console mode to DisableQuickEdit: {0}", GetErrorMessage()));
                    }
                }

                /// <summary>
                /// Convenience method for enabling the mouse select in the Console,
                /// which if left selected leads to blocking all running threads that write to the console.
                /// </summary>
                public static void EnableQuickEdit()
                {
                    IntPtr conHandle = GetStdHandle(StdInputHandle);
                    int mode;

                    if (!GetConsoleMode(conHandle, out mode))
                    {
                        Console.WriteLine(String.Format("Error getting the console mode: {0}", GetErrorMessage()));
                        return;
                    }

                    mode |= QuickEditMode | ExtendedFlags;

                    if (!SetConsoleMode(conHandle, mode))
                    {
                        Console.WriteLine(String.Format("Error setting the console mode to EnableQuickEdit: {0}", GetErrorMessage()));
                    }
                }

                #endregion
            }

            public static class WindowsClipboard
            {
                #region Private Constants

                /// <summary>
                /// The UnicodeText format code
                /// </summary>
                private const uint CfUnicodeText = 13;

                #endregion

                #region Private External Declarations

                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                private static extern bool OpenClipboard(IntPtr hWndNewOwner);

                [DllImport("user32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                private static extern bool CloseClipboard();

                [DllImport("user32.dll", SetLastError = true)]
                private static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

                [DllImport("user32.dll")]
                private static extern bool EmptyClipboard();

                [DllImport("kernel32.dll", SetLastError = true)]
                private static extern IntPtr GlobalLock(IntPtr hMem);

                [DllImport("kernel32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                private static extern bool GlobalUnlock(IntPtr hMem);

                #endregion

                #region Private Methods
                /// <summary>
                /// Open the Windows Clipboard
                /// </summary>
                private static void OpenWindowsClipboard()
                {
                    int retryCount = 3;
                    while (true)
                    {
                        if (OpenClipboard(IntPtr.Zero) || --retryCount == 0)
                        {
                            if (retryCount == 0)
                            {
                                Console.WriteLine(String.Format("Error calling OpenClipboard: {0}", GetErrorMessage()));
                            }
                            break;
                        }

                        Thread.Sleep(333);
                    }
                }

                /// <summary>
                /// Close the Windows Clipboard
                /// </summary>
                private static void CloseWindowsClipboard()
                {
                    int retryCount = 3;
                    while (true)
                    {
                        if (CloseClipboard() || --retryCount == 0)
                        {
                            if (retryCount == 0)
                            {
                                Console.WriteLine(String.Format("Error calling CloseClipboard: {0}", GetErrorMessage()));
                            }
                            break;
                        }

                        Thread.Sleep(333);
                    }
                }

                #endregion

                #region Public Methods

                /// <summary>
                /// Sets the stringValue of the Clipboard
                /// </summary>
                /// <param name="stringValue"></param>
                public static void SetTextValue(string stringValue)
                {
                    OpenWindowsClipboard();

                    EmptyClipboard();

                    IntPtr hMem = IntPtr.Zero;
                    try
                    {
                        // Allocate necessary unmanaged bytes to hold the Unicode string value
                        var bytes = (stringValue.Length + 1) * 2;
                        
                        hMem = Marshal.AllocHGlobal(bytes);

                        if (hMem == IntPtr.Zero)
                        {
                            Console.WriteLine(String.Format("Error calling Marshal.AllocHGlobal: {0}", GetErrorMessage()));
                            return;
                        }

                        var destination = GlobalLock(hMem);

                        if (destination == IntPtr.Zero)
                        {
                            Console.WriteLine(String.Format("Error calling GlobalLock: {0}", GetErrorMessage()));
                            return;
                        }

                        try
                        {
                            Marshal.Copy(stringValue.ToCharArray(), 0, destination, stringValue.Length);
                        }
                        finally
                        {
                            GlobalUnlock(destination);
                        }

                        if (SetClipboardData(CfUnicodeText, hMem) == IntPtr.Zero)
                        {
                            Console.WriteLine(String.Format("Error calling SetClipboardData: {0}", GetErrorMessage()));
                            return;
                        }

                        hMem = IntPtr.Zero;
                    }
                    finally
                    {
                        if (hMem != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(hMem);
                        }

                        CloseWindowsClipboard();
                    }
                }

                #endregion
            }

            #endregion
        }
        #endregion
    }
}
