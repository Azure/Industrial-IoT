// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Ssh {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Renci.SshNet;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure shell implementation
    /// </summary>
    public sealed class SshSecureShell : ISecureShell {

        /// <summary>
        /// Creates SSHShell.
        /// </summary>
        /// <param name="host">the host name</param>
        /// <param name="port">the ssh port</param>
        /// <param name="userName">the ssh user name</param>
        /// <param name="password">the ssh password</param>
        /// <param name="logger">the ssh password</param>
        public SshSecureShell(string host, int port, string userName, string password,
            ILogger logger) {
            _logger = logger;
            _homeDir = UBUNTU_HOME_DIRECTORY + userName + "/";
            _sshClient = new SshClient(host, port, userName, password);
            _sshClient.Connect();
            _scpClient = new ScpClient(host, port, userName, password);
            _scpClient.Connect();
        }

        /// <inheritdoc/>
        public async Task BindAsync(Stream input, Stream output,
            int cols, int rows, int width, int height, CancellationToken ct) {
            if (_sshClient == null) {
                return;
            }

            var bufLen = Math.Min(1024, width * height);
            using (var shell = _sshClient.CreateShellStream(string.Empty,
                (uint)cols, (uint)rows, (uint)width, (uint)height, bufLen)) {
                if (shell == null) {
                    return;
                }

                var tcs = new TaskCompletionSource<bool>();
                var cts = new CancellationTokenSource();
                ct.Register(() => {
                    // Cancel pumps
                    cts.Cancel();
                    // Cancel the binding
                    tcs.TrySetCanceled();
                });

                shell.ErrorOccurred += (_, e) => {
                    // Cancel pumps
                    cts.Cancel();
                    // Except the binding
                    if (e.Exception != null) {
                        tcs.TrySetException(e.Exception);
                    }
                };

                if (output != null) {
                    shell.DataReceived += (_, e) => output.Write(e.Data);
                }
                if (input != null) {
                    _ = Task.Run(() => PumpInputAsync(input, shell, tcs,
                        cts.Token));
                }
                try {
                    // Wait until we are cancelled or error occurs
                    await tcs.Task;
                }
                finally {
                    Try.Op(shell.Close);
                }
            }
        }

        /// <inheritdoc/>
        public Task<string> ExecuteCommandAsync(string commandToExecute,
            CancellationToken ct) {
            if (_sshClient == null) {
                return Task.FromResult((string)null);
            }
            return Task.Run(() => {
                using (var command = _sshClient.CreateCommand(commandToExecute)) {
                    var result = command.Execute();
                    _logger.Debug("SSH> {command}\n{result}", commandToExecute, result);
                    return result;
                }
            }, ct);
        }

        /// <inheritdoc/>
        public Task<string> DownloadAsync(string fileName, string fromPath,
            bool isUserHomeBased, CancellationToken ct) {
            if (_scpClient == null) {
                return Task.FromResult((string)null);
            }

            var path = fromPath;
            if (isUserHomeBased) {
                path = _homeDir + path;
            }

            return Task.Run(() => {
                using (var mstream = new MemoryStream()) {
                    var target = path + "/" + fileName;
                    _scpClient.Download(target, mstream);
                    _logger.Debug("SCP> {target} downloaded.", target);
                    mstream.Position = 0;
                    return new StreamReader(mstream).ReadToEnd();
                }
            }, ct);
        }

        /// <inheritdoc/>
        public Task<int> DownloadAsync(byte[] destBuff, int maxCount, string fileName,
            string fromPath, bool isUserHomeBased, CancellationToken ct) {
            if (_scpClient == null) {
                return Task.FromResult(-1);
            }

            var path = fromPath;
            if (isUserHomeBased) {
                path = _homeDir + path;
            }

            return Task.Run(() => {
                using (var mstream = new MemoryStream()) {
                    var target = path + "/" + fileName;
                    _scpClient.Download(target, mstream);
                    if (mstream.Position >= maxCount) {
                        return -1;
                    }
                    _logger.Debug("SCP> {target} downloaded.", target);
                    mstream.Position = 0;
                    return mstream.Read(destBuff, 0, maxCount);
                }
            }, ct);
        }

        /// <inheritdoc/>
        public Task DownloadFolderAsync(string toPath,
            string fromPath, bool isUserHomeBased, CancellationToken ct) {
            if (_scpClient == null) {
                return Task.FromResult(-1);
            }

            var path = fromPath;
            if (isUserHomeBased) {
                path = _homeDir + path;
            }

            return Task.Run(() => {
                _scpClient.Download(path, new DirectoryInfo(toPath));
                _logger.Debug("SCP> {path} downloaded to {toPath}.", path, toPath);
            }, ct);
        }

        /// <inheritdoc/>
        public Task UploadAsync(byte[] data, string fileName, string toPath,
            bool isUserHomeBased, string mode, CancellationToken ct) {
            if (_scpClient == null || _sshClient == null) {
                return Task.CompletedTask;
            }

            var path = toPath;
            if (isUserHomeBased) {
                path = _homeDir + path;
            }
            return Task.Run(() => {
                // Create the directory on the remote host
                using (var command = _sshClient.CreateCommand("mkdir -p " + path)) {
                    command.Execute();
                }

                var target = path + "/" + fileName;

                // Create the file containing the uploaded data
                using (var mstream = new MemoryStream(data)) {
                    _scpClient.Upload(mstream, target);
                    _logger.Debug("SCP> {target} uploaded.", target);
                }

                if (string.IsNullOrEmpty(mode)) {
                    return;
                }

                // Change file mode
                using (var command = _sshClient.CreateCommand($"chmod {mode} {target}")) {
                    command.Execute();
                }
            }, ct);
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_sshClient != null) {
                _sshClient.Disconnect();
                _sshClient.Dispose();
            }
            if (_scpClient != null) {
                _scpClient.Disconnect();
                _scpClient.Dispose();
            }
        }

        /// <summary>
        /// Pump input to output stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="tcs"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task PumpInputAsync(Stream input, Stream output,
            TaskCompletionSource<bool> tcs, CancellationToken ct) {
            var buf = new byte[1024];
            // var escape = false;

            // Pump until error occurs or pump is cancelled
            while (!ct.IsCancellationRequested) {
                try {
                    var read = await input.ReadAsync(buf, ct);
                    if (read == 0) {
                        // TODO: Also need to fix - EOL is indicatead by
                        // this...
                        if (!input.CanRead) {
                            throw new ObjectDisposedException(nameof(input));
                        }
                        continue;
                    }
                    // TODO need to make vterm work correctly on Windows
                    // and also linux
                    //
                    // Read console on .net does not unblock when esc is
                    // emitted.  Need to read key by key to also read up
                    // down and then convert to ESC rather than relying
                    // on new vt input stream.

                    await output.WriteAsync(buf, 0, read, ct);
                    // if (!escape && buf[0] != 13) {
                    //     escape = (buf[0] < 32 && buf[0] > 127);
                    //     continue;
                    // }
                    await output.FlushAsync(ct);
                    // escape = false;

                    // BUGBUG: should get disconnected event Poll for 1
                    // second until flush hits object disposed exception
                    // meaning the underlying channel was disconnected.

                    //if (buf.Contains("exit")) {
                    //    for (var i = 0; i < 10; i++) {
                    //        await Task.Delay(100);
                    //        shell.Flush();
                    //    }
                    //}
                }
                catch (OperationCanceledException) {
                    return;
                }
                catch (ObjectDisposedException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.Error(ex, "input error");
                    continue;
                }
            }
            tcs.TrySetResult(true);
        }

        const string UBUNTU_HOME_DIRECTORY = "/home/";
        private readonly SshClient _sshClient;
        private readonly ScpClient _scpClient;
        private readonly string _homeDir;
        private readonly ILogger _logger;
    }
}
