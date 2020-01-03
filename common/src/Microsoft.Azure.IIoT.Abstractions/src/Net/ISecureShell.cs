// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a shell that can be used to interact with a system.
    /// </summary>
    public interface ISecureShell : IDisposable {

        /// <summary>
        /// Attaches input, output and error streams to the shell and
        /// pipes data in and out until an error occurs or the passed
        /// int cancellation token is cancelled.
        /// </summary>
        /// <param name="input">The input writer</param>
        /// <param name="output">The output writer</param>
        /// <param name="width">Terminal width</param>
        /// <param name="cols">Terminal columns</param>
        /// <param name="rows">Terminal rows</param>
        /// <param name="height">Terminal height</param>
        /// <param name="ct">A cancellation token to cancel the binding
        /// </param>
        /// <returns>Cancellation token to stop the shell or notify the user
        /// the shell is stopped</returns>
        Task BindAsync(Stream input, Stream output,
            int cols, int rows, int width, int height, CancellationToken ct);

        /// <summary>
        /// Downloads the content of a file from the remote host into a
        /// buffer.
        /// </summary>
        /// <param name="destBuff">The buffer to download into</param>
        /// <param name="maxCount">Max number of bytes to download.</param>
        /// <param name="fileName">the name of the file for which the content
        /// will be downloaded</param>
        /// <param name="fromPath">the path of the file for which the content
        /// will be downloaded</param>
        /// <param name="isUserHomeBased">true if the path of the file is
        /// relative to the user's home directory</param>
        /// <param name="ct">A cancellation token to cancel the download
        /// </param>
        /// <returns>the number of bytes read</returns>
        Task<int> DownloadAsync(byte[] destBuff, int maxCount, string fileName,
            string fromPath, bool isUserHomeBased, CancellationToken ct);

        /// <summary>
        /// Downloads the content of a file from the remote host as a String.
        /// </summary>
        /// <param name="fileName">the name of the file for which the content
        /// will be downloaded</param>
        /// <param name="fromPath">the path of the file for which the content
        /// will be downloaded</param>
        /// <param name="isUserHomeBased">true if the path of the file is
        /// relative to the user's home directory</param>
        /// <param name="ct">A cancellation token to cancel the download
        /// </param>
        /// <returns>the content of the file</returns>
        Task<string> DownloadAsync(string fileName, string fromPath,
            bool isUserHomeBased, CancellationToken ct);

        /// <summary>
        /// Executes a command on the remote host.
        /// </summary>
        /// <param name="commandToExecute">the command to be executed</param>
        /// <param name="ct">A cancellation token to cancel the command
        /// </param>
        /// <returns> the content of the remote output from executing
        /// the command</returns>
        Task<string> ExecuteCommandAsync(string commandToExecute,
            CancellationToken ct);

        /// <summary>
        /// Creates a new file on the remote host using the input content.
        /// </summary>
        /// <param name="data">the byte array content to be uploaded</param>
        /// <param name="fileName"> the name of the file for which the content
        /// will be saved into</param>
        /// <param name="toPath">the path of the file for which the content
        /// will be saved into</param>
        /// <param name="isUserHomeBased">true if the path of the file is
        /// relative to the user's home directory</param>
        /// <param name="mode">File mode of file to create</param>
        /// <param name="ct">A cancellation token to cancel the upload
        /// </param>
        Task UploadAsync(byte[] data, string fileName, string toPath,
            bool isUserHomeBased, string mode, CancellationToken ct);

        /// <summary>
        /// Downloads the content of a folder from the remote host into a
        /// local folder.
        /// </summary>
        /// <param name="toPath">The folder to download into</param>
        /// <param name="fromPath">the path from which the files will be
        /// downloaded</param>
        /// <param name="isUserHomeBased">true if the path of the file is
        /// relative to the user's home directory</param>
        /// <param name="ct">A cancellation token to cancel the download
        /// </param>
        Task DownloadFolderAsync(string toPath,
            string fromPath, bool isUserHomeBased, CancellationToken ct);
    }
}
