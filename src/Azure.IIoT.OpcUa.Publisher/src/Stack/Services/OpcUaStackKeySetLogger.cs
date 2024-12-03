// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// Wireshark 4.3 now allows decryption of the UA binary protocol using a keyset log
    /// (opc ua debug file). The file contains records in the following format:
    /// </para>
    /// <para>
    /// client_iv_%channel_id%_%token_id%: %hex-string%
    /// client_key_%channel_id%_%token_id%: %hex-string%
    /// client_siglen_%channel_id%_%token_id%: 32
    /// server_iv_%channel_id%_%token_id%: %hex-string%
    /// server_key_%channel_id%_%token_id%: %hex-string%
    /// server_siglen_%channel_id%_%token_id%: 16|24|32
    /// ...
    /// </para>
    /// <para>
    /// This class writes the file to disk if a file was configured
    /// See: https://gitlab.com/wireshark/wireshark/-/blob/master/plugins/epan/opcua/opcua.c#L232
    /// </para>
    /// </summary>
    public sealed class OpcUaStackKeySetLogger : IDisposable
    {
        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="options"></param>
        /// <param name="diagnostics"></param>
        /// <param name="logger"></param>
        public OpcUaStackKeySetLogger(IOptions<OpcUaClientOptions> options,
            IClientDiagnostics diagnostics, ILogger<OpcUaStackKeySetLogger> logger)
        {
            _diagnostics = diagnostics;
            _logger = logger;
            _cts = new CancellationTokenSource();
            if (options.Value.OpcUaKeySetLogFolderName == null)
            {
                _task = Task.CompletedTask;
                return;
            }
            _task = WriteDebugFileAsync(options.Value.OpcUaKeySetLogFolderName, _cts.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _task.GetAwaiter().GetResult();
            }
            catch
            {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Log debug file to disk
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task WriteDebugFileAsync(string folderName, CancellationToken ct)
        {
            var rootFolder = Path.Combine(folderName, "opcua_debug");
            if (Directory.Exists(rootFolder))
            {
                Directory.Delete(rootFolder, true);
            }
            await foreach (var change in _diagnostics.WatchChannelDiagnosticsAsync(
                ct).ConfigureAwait(false))
            {
                try
                {
                    await WriteDebugLogFileAsync(rootFolder, change, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write changes to debug log file.");
                }
            }
        }

        /// <summary>
        /// Write diagnostic change to log file
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="change"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task WriteDebugLogFileAsync(string rootFolder,
            ChannelDiagnosticModel change, CancellationToken ct)
        {
            if (change?.Client == null || change?.Server == null ||
                change.SessionCreated == null || change.RemotePort == null)
            {
                // Not a valid change, channel without keys
                return;
            }

            var keySetLogPath = Path.Combine(rootFolder, "ports",
                change.RemotePort.Value.ToString(CultureInfo.InvariantCulture),
                    "connection", change.Connection.CreateConsistentHash()
                        .ToString("X", CultureInfo.InvariantCulture),
                    change.SessionCreated.Value.UtcDateTime.ToBinary()
                        .ToString(CultureInfo.InvariantCulture));
            if (!Directory.Exists(keySetLogPath))
            {
                Directory.CreateDirectory(keySetLogPath);
            }
            var keysetsFileName = Path.Combine(keySetLogPath, "opcua_debug.txt");
            var keysetFileRoot = keysetsFileName.Replace(rootFolder, ".",
                StringComparison.OrdinalIgnoreCase);
            var log = File.AppendText(Path.Combine(rootFolder, "log.md"));
            await using (var _ = log.ConfigureAwait(false))
            {
                await log.WriteLineAsync($"# {change.TimeStamp}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"[KeySetFile]({keysetFileRoot})")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"EndpointUrl: {change.Connection.Endpoint?.Url}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"RemoteEP: {change.RemoteIpAddress}:{change.RemotePort}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"LocalEP: {change.LocalIpAddress}:{change.LocalPort}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"ChannelId: {change.ChannelId}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"TokenId: {change.TokenId}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"Session: {change.SessionId}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"Created: {change.SessionCreated}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"SecurityMode: {change.Connection.Endpoint?.SecurityMode}")
                    .ConfigureAwait(false);
                await log.WriteLineAsync($"SecurityProfile: {change.Connection.Endpoint?.SecurityPolicy}")
                    .ConfigureAwait(false);

                await log.FlushAsync(ct).ConfigureAwait(false);
            }

            var keysets = File.AppendText(keysetsFileName);
            await using (var _ = keysets.ConfigureAwait(false))
            {
                await keysets.WriteAsync($"client_iv_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(Convert.ToHexString([.. change.Client.Iv]))
                    .ConfigureAwait(false);
                await keysets.WriteAsync($"client_key_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(Convert.ToHexString([.. change.Client.Key]))
                    .ConfigureAwait(false);
                await keysets.WriteAsync($"client_siglen_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(change.Client.SigLen.ToString(CultureInfo.InvariantCulture))
                    .ConfigureAwait(false);

                await keysets.WriteAsync($"server_iv_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(Convert.ToHexString([.. change.Server.Iv]))
                    .ConfigureAwait(false);
                await keysets.WriteAsync($"server_key_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(Convert.ToHexString([.. change.Server.Key]))
                    .ConfigureAwait(false);
                await keysets.WriteAsync($"server_siglen_{change.ChannelId}_{change.TokenId}: ")
                    .ConfigureAwait(false);
                await keysets.WriteLineAsync(change.Server.SigLen.ToString(CultureInfo.InvariantCulture))
                    .ConfigureAwait(false);

                await keysets.FlushAsync(ct).ConfigureAwait(false);
            }
        }

        private readonly Task _task;
        private readonly CancellationTokenSource _cts;
        private readonly IClientDiagnostics _diagnostics;
        private readonly ILogger<OpcUaStackKeySetLogger> _logger;
    }
}
