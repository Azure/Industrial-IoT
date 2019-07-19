// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Ssh {
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure shell extensions
    /// </summary>
    public static class SecureShellEx {

        /// <summary>
        /// Bind shell to current terminal/console
        /// </summary>
        /// <returns></returns>
        public static Task BindAsync(this ISecureShell shell,
            CancellationToken ct) {
            return BindAsync(shell, false, ct);
        }

        /// <summary>
        /// Bind shell to terminal/console
        /// </summary>
        /// <returns></returns>
        public static async Task BindAsync(this ISecureShell shell,
            bool forceVTerm, CancellationToken ct) {
            var win = IsWindowsAnniversaryEdition(forceVTerm);
            var inMode = ~0u;
            var outMode = ~0u;
            if (IsWindowsAnniversaryEdition(forceVTerm)) {
                EnableVtermOnWindows10AnniversaryEdition(
                    out inMode, out outMode);
            }
            var ctrlC = Console.TreatControlCAsInput;
            Console.TreatControlCAsInput = true;
            using (var input = Console.OpenStandardInput())
            using (var output = Console.OpenStandardOutput()) {
                await shell.BindAsync(input, output,
                    Console.WindowWidth, Console.WindowHeight,
                    Console.BufferWidth, Console.BufferHeight, ct);
            }
            Console.TreatControlCAsInput = ctrlC;
            if (inMode != ~0u && outMode != ~0u) {
                RestoreConsoleModes(inMode, outMode);
            }
        }

        /// <summary>
        /// Check whether to enable vterm processing on console
        /// </summary>
        /// <param name="forceVTerm"></param>
        /// <returns></returns>
        private static bool IsWindowsAnniversaryEdition(bool forceVTerm) {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                if ((Environment.OSVersion.Version.Major == 6 &&
                    Environment.OSVersion.Version.Minor >= 2 &&
                    Environment.OSVersion.Version.Build >= 9200) ||
                    (Environment.OSVersion.Version.Major > 6)) {
                    return true;
                }
                if (forceVTerm) {
                    throw new PlatformNotSupportedException(
                        $"Windows {Environment.OSVersion.VersionString} not supported.");
                }
            }
            return false;
        }

        /// <summary>
        /// Windows interop
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>
        /// Enable vterm mode on windows anniversary edition
        /// </summary>
        private static void EnableVtermOnWindows10AnniversaryEdition(
            out uint oldinput, out uint oldOutput) {
            // Enable virtual output
            var handle = GetStdHandle(kStdOut);
            GetConsoleMode(handle, out oldOutput);
            const uint k_DISABLE_NEWLINE_AUTO_RETURN = 0x8;
            const uint k_ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x4;
            SetConsoleMode(handle, oldOutput |
                k_ENABLE_VIRTUAL_TERMINAL_PROCESSING |
                k_DISABLE_NEWLINE_AUTO_RETURN);

            // Enable virtual input
            handle = GetStdHandle(kStdIn);
            GetConsoleMode(handle, out oldinput);
            const uint kENABLE_VIRTUAL_TERMINAL_INPUT = 0x200;
            SetConsoleMode(handle, oldinput |
                kENABLE_VIRTUAL_TERMINAL_INPUT);
        }

        /// <summary>
        /// Set input and output modes
        /// </summary>
        private static void RestoreConsoleModes(
            uint input, uint output) {
            var handle = GetStdHandle(kStdOut);
            SetConsoleMode(handle, output);
            handle = GetStdHandle(kStdIn);
            SetConsoleMode(handle, input);
        }

        private const int kStdOut = -11;
        private const int kStdIn = -10;
    }
}
