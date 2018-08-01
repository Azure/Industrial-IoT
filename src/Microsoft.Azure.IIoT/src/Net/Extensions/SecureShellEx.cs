// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

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
            string fromPath, bool isUserHomeBased) =>
            shell.DownloadAsync(destBuff, maxCount, fileName, fromPath,
                isUserHomeBased, TimeSpan.FromMinutes(1));

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
            string fromPath, bool isUserHomeBased, TimeSpan timeout) =>
            shell.DownloadAsync(destBuff, maxCount, fileName, fromPath,
                isUserHomeBased, new CancellationTokenSource(timeout).Token);

        /// <summary>
        /// Download with default timeout of 1 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="fileName"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task<string> DownloadAsync(this ISecureShell shell,
            string fileName, string fromPath, bool isUserHomeBased) =>
            shell.DownloadAsync(fileName, fromPath, isUserHomeBased,
                TimeSpan.FromMinutes(1));

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
            TimeSpan timeout) =>
            shell.DownloadAsync(fileName, fromPath, isUserHomeBased,
                new CancellationTokenSource(timeout).Token);

        /// <summary>
        /// Download folder with default timeout of 5 minute
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="toPath"></param>
        /// <param name="fromPath"></param>
        /// <param name="isUserHomeBased"></param>
        /// <returns></returns>
        public static Task DownloadFolderAsync(this ISecureShell shell,
            string toPath, string fromPath, bool isUserHomeBased) =>
            shell.DownloadFolderAsync(toPath, fromPath, isUserHomeBased,
                TimeSpan.FromMinutes(5));

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
            TimeSpan timeout) =>
            shell.DownloadFolderAsync(toPath, fromPath, isUserHomeBased,
                new CancellationTokenSource(timeout).Token);

        /// <summary>
        /// Execute with default timeout of 3 minutes
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="commandToExecute"></param>
        /// <returns></returns>
        public static Task<string> ExecuteCommandAsync(this ISecureShell shell,
            string commandToExecute) =>
            shell.ExecuteCommandAsync(commandToExecute, TimeSpan.FromMinutes(3));

        /// <summary>
        /// Execute with timeout
        /// </summary>
        /// <param name="shell"></param>
        /// <param name="commandToExecute"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<string> ExecuteCommandAsync(this ISecureShell shell,
            string commandToExecute, TimeSpan timeout) =>
            shell.ExecuteCommandAsync(commandToExecute,
                new CancellationTokenSource(timeout).Token);

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
            string fileName, string toPath, bool isUserHomeBased, string mode) =>
            shell.UploadAsync(data, fileName, toPath, isUserHomeBased, mode,
                TimeSpan.FromMinutes(1));

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
            TimeSpan timeout) =>
            shell.UploadAsync(data, fileName, toPath, isUserHomeBased, mode,
                new CancellationTokenSource(timeout).Token);

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
            string fileName, string toPath, bool isUserHomeBased) =>
            shell.UploadAsync(data, fileName, toPath, isUserHomeBased,
                TimeSpan.FromMinutes(1));

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
            TimeSpan timeout) =>
            shell.UploadAsync(data, fileName, toPath, isUserHomeBased, null,
                new CancellationTokenSource(timeout).Token);

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
            CancellationToken ct) =>
            shell.UploadAsync(data, fileName, toPath, isUserHomeBased, null, ct);

    }
}
