using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NesmakerDarkmode
{
    /// <summary>
    /// Provides utility functions for allocating and redirecting a console window.
    /// </summary>
    public static class ConsoleUtility
    {
        // P/Invoke for console allocation
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        // P/Invoke for setting standard handles
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        // P/Invoke for creating/opening files (using Unicode version)
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
             string lpFileName,
             uint dwDesiredAccess,
             uint dwShareMode,
             IntPtr lpSecurityAttributes,
             uint dwCreationDisposition,
             uint dwFlagsAndAttributes,
             IntPtr hTemplateFile
        );

        // Access rights constants for CreateFileW
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_READ = 0x80000000;
        // Share mode constants
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        // Creation disposition constants
        private const uint OPEN_EXISTING = 3;

        // Standard stream constants
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const int STD_INPUT_HANDLE = -10;

        /// <summary>
        /// Allocates a new console window and redirects standard I/O streams to it.
        /// </summary>
        /// <returns>True if the console was allocated and streams redirected successfully, false otherwise.</returns>
        public static bool InitializeConsole()
        {
            if (!AllocConsole())
            {
                // Console already exists or allocation failed
                int error = Marshal.GetLastWin32Error();
                // 183 (ERROR_ALREADY_EXISTS) is common if console exists, can potentially ignore
                // Or handle other errors as needed
                return false; // Indicate failure or existing console
            }

            try
            {
                RedirectConsoleIO();
                Console.WriteLine("Debug Console Initialized.");
                return true;
            }
            catch (Exception ex)
            {
                // Log or handle the exception during redirection
                Console.WriteLine($"Error redirecting console I/O: {ex.Message}");
                return false;
            }
        }

        private static void RedirectConsoleIO()
        {
            // Redirect stdout and stderr
            SafeFileHandle hStdOut = CreateFileW("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (!hStdOut.IsInvalid)
            {
                if (!SetStdHandle(STD_OUTPUT_HANDLE, hStdOut.DangerousGetHandle()) ||
                    !SetStdHandle(STD_ERROR_HANDLE, hStdOut.DangerousGetHandle())) // Redirect error to the same handle
                {
                     // Handle SetStdHandle error if needed
                     Console.WriteLine($"SetStdHandle for OUT/ERR failed. Error: {Marshal.GetLastWin32Error()}");
                }


                var stdOutStream = new FileStream(hStdOut, FileAccess.Write);
                var stdOutWriter = new StreamWriter(stdOutStream, Console.OutputEncoding) { AutoFlush = true };
                Console.SetOut(stdOutWriter);

                // Use existing handle, don't own it for the second stream
                var stdErrStream = new FileStream(new SafeFileHandle(hStdOut.DangerousGetHandle(), false), FileAccess.Write);
                var stdErrWriter = new StreamWriter(stdErrStream, Console.OutputEncoding) { AutoFlush = true };
                Console.SetError(stdErrWriter);
            }
            else
            {
                Console.WriteLine($"CreateFileW for CONOUT$ failed. Error: {Marshal.GetLastWin32Error()}");
            }


            // Redirect stdin
            SafeFileHandle hStdIn = CreateFileW("CONIN$", GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (!hStdIn.IsInvalid)
            {
                if (!SetStdHandle(STD_INPUT_HANDLE, hStdIn.DangerousGetHandle()))
                {
                    // Handle SetStdHandle error if needed
                    Console.WriteLine($"SetStdHandle for IN failed. Error: {Marshal.GetLastWin32Error()}");
                }

                var stdInStream = new FileStream(hStdIn, FileAccess.Read);
                var stdInReader = new StreamReader(stdInStream, Console.InputEncoding);
                Console.SetIn(stdInReader);
            }
             else
            {
                Console.WriteLine($"CreateFileW for CONIN$ failed. Error: {Marshal.GetLastWin32Error()}");
            }
        }
    }
} 