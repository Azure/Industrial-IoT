// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure shell extensions
    /// </summary>
    public static class SecureShellEx {

        /// <summary>
        /// Download with default timeout of 1 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="destBuff"></param>
        /// <param name="maxCount"></param>
        /// <param name="fileName"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task<int> DownloadAsync(this ISecureShell shell,
            byte[] destBuff, int maxCount, string fileName,
            string fromPath, bool isUserHomeBased) {
            return shell.DownloadAsync(destBuff, maxCount, fileName, fromPath,
                isUserHomeBased, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Download with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="destBuff"></param>
        /// <param name="maxCount"></param>
        /// <param name="fileName"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<int> DownloadAsync(this ISecureShell shell,
            byte[] destBuff, int maxCount, string fileName,
            string fromPath, bool isUserHomeBased, TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.DownloadAsync(destBuff, maxCount, fileName, fromPath,
                    isUserHomeBased, cts.Token);
            }
        }

        /// <summary>
        /// Download with default timeout of 1 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="fileName"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task<string> DownloadAsync(this ISecureShell shell,
            string fileName, string fromPath, bool isUserHomeBased) {
            return shell.DownloadAsync(fileName, fromPath, isUserHomeBased,
                TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Download with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="fileName"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<string> DownloadAsync(this ISecureShell shell,
            string fileName, string fromPath, bool isUserHomeBased,
            TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.DownloadAsync(fileName, fromPath, isUserHomeBased,
                    cts.Token);
            }
        }

        /// <summary>
        /// Download folder with default timeout of 5 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="toPath"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task DownloadFolderAsync(this ISecureShell shell,
            string toPath, string fromPath, bool isUserHomeBased) {
            return shell.DownloadFolderAsync(toPath, fromPath, isUserHomeBased,
                TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Download folder  with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="toPath"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task DownloadFolderAsync(this ISecureShell shell,
            string toPath, string fromPath, bool isUserHomeBased,
            TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.DownloadFolderAsync(toPath, fromPath, isUserHomeBased,
                    cts.Token);
            }
        }

        /// <summary>
        /// Execute with default timeout of 3 minutes
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="commandToExecute"></param>
        /// <returns></returns>
        public static Task<string> ExecuteCommandAsync(this ISecureShell shell,
            string commandToExecute) {
            return shell.ExecuteCommandAsync(commandToExecute, TimeSpan.FromMinutes(3));
        }

        /// <summary>
        /// Execute with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="commandToExecute"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<string> ExecuteCommandAsync(this ISecureShell shell,
            string commandToExecute, TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.ExecuteCommandAsync(commandToExecute,
                    cts.Token);
            }
        }

        /// <summary>
        /// Upload with default timeout of 1 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="toPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Task UploadAsync(this ISecureShell shell, byte[] data,
            string fileName, string toPath, bool isUserHomeBased, string mode) {
            return shell.UploadAsync(data, fileName, toPath, isUserHomeBased, mode,
                TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Upload with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="toPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="mode"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task UploadAsync(this ISecureShell shell, byte[] data,
            string fileName, string toPath, bool isUserHomeBased, string mode,
            TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.UploadAsync(data, fileName, toPath, isUserHomeBased, mode,
                    cts.Token);
            }
        }

        /// <summary>
        /// Upload with default timeout of 1 minute and default permissions
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="toPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task UploadAsync(this ISecureShell shell, byte[] data,
            string fileName, string toPath, bool isUserHomeBased) {
            return shell.UploadAsync(data, fileName, toPath, isUserHomeBased,
                TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Upload with timeout and default permissions
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="toPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task UploadAsync(this ISecureShell shell, byte[] data,
            string fileName, string toPath, bool isUserHomeBased,
            TimeSpan timeout) {
            using (var cts = new CancellationTokenSource(timeout)) {
                return shell.UploadAsync(data, fileName, toPath, isUserHomeBased, null,
                    cts.Token);
            }
        }

        /// <summary>
        /// Upload file with default permissions
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="toPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task UploadAsync(this ISecureShell shell, byte[] data,
            string fileName, string toPath, bool isUserHomeBased,
            CancellationToken ct) {
            return shell.UploadAsync(data, fileName, toPath, isUserHomeBased, null, ct);
        }
    }
}
