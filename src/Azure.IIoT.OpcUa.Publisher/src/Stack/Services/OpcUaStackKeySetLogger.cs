// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;
    using System.Globalization;
    using System.IO;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;

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
        public OpcUaStackKeySetLogger(IOptions<OpcUaClientOptions> options,
            IClientDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
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
            await foreach (var change in _diagnostics.GetConnectionDiagnosticAsync(
                ct).ConfigureAwait(false))
            {
                var entry = change.ChannelDiagnostics;
                if (entry?.Client == null || entry?.Server == null || change.RemotePort == null)
                {
                    // Not a valid entry, channel without keys
                    continue;
                }

                var id = change.Connection.CreateConsistentHash();
                var path = Path.Combine(folderName, "port",
                    change.RemotePort.Value.ToString(CultureInfo.InvariantCulture),
                    "connection", id.ToString("X", CultureInfo.InvariantCulture));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var logFileName = Path.Combine(path, "opcua_debug.log");
                var log = File.AppendText(logFileName);
                await using (var _ = log.ConfigureAwait(false))
                {
                    await log.WriteAsync($"Timestamp={change.TimeStamp};")
                        .ConfigureAwait(false);
                    await log.WriteAsync($"Connection={change.Connection};")
                        .ConfigureAwait(false);
                    await log.WriteAsync($"LocalEP={change.LocalIpAddress}:{change.LocalPort};")
                        .ConfigureAwait(false);
                    await log.WriteAsync($"RemoteEP={change.RemoteIpAddress}:{change.RemotePort};")
                        .ConfigureAwait(false);
                    await log.WriteLineAsync($"ChannelId={entry.ChannelId};TokenId={entry.TokenId}")
                        .ConfigureAwait(false);

                    await log.FlushAsync(ct).ConfigureAwait(false);
                }

                var keysetsFileName = Path.Combine(path, "opcua_debug.txt");
                var keysets = File.AppendText(keysetsFileName);
                await using (var _ = keysets.ConfigureAwait(false))
                {
                    await keysets.WriteAsync($"client_iv_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(Convert.ToHexString(entry.Client.Iv.ToArray()))
                        .ConfigureAwait(false);
                    await keysets.WriteAsync($"client_key_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(Convert.ToHexString(entry.Client.Key.ToArray()))
                        .ConfigureAwait(false);
                    await keysets.WriteAsync($"client_siglen_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(entry.Client.SigLen.ToString(CultureInfo.InvariantCulture))
                        .ConfigureAwait(false);

                    await keysets.WriteAsync($"server_iv_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(Convert.ToHexString(entry.Server.Iv.ToArray()))
                        .ConfigureAwait(false);
                    await keysets.WriteAsync($"server_key_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(Convert.ToHexString(entry.Server.Key.ToArray()))
                        .ConfigureAwait(false);
                    await keysets.WriteAsync($"server_siglen_{entry.ChannelId}_{entry.TokenId}: ")
                        .ConfigureAwait(false);
                    await keysets.WriteLineAsync(entry.Server.SigLen.ToString(CultureInfo.InvariantCulture))
                        .ConfigureAwait(false);

                    await keysets.FlushAsync(ct).ConfigureAwait(false);
                }
            }
        }

        private readonly Task _task;
        private readonly CancellationTokenSource _cts;
        private readonly IClientDiagnostics _diagnostics;
    }
}
