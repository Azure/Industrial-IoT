// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using System.IO;
    using System.Threading.Tasks;
    using Serilog;
    using System.Collections.Generic;

    /// <summary>
    /// Writes messages to file
    /// </summary>
    public class FileSystemMessageSink : IMessageSink {

        /// <inheritdoc/>
        public ulong SentMessagesCount { get; private set; }

        /// <summary>
        /// Create sink
        /// </summary>
        /// <param name="messageSinkConfiguration"></param>
        /// <param name="logger"></param>
        public FileSystemMessageSink(IFileSystemConfiguration messageSinkConfiguration, ILogger logger) {
            if (!Directory.Exists(messageSinkConfiguration.Directory)) {
                Directory.CreateDirectory(messageSinkConfiguration.Directory);
            }
            _directory = messageSinkConfiguration.Directory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<NetworkMessageModel> messages) {
            foreach (var message in messages) {
                var filename = kFilenamePattern.ToLower().Replace("{timestamp}",
                    message.Timestamp.ToString("yyyyMMdd-HHmmss")).Replace("{id}", message.MessageId);
                var fullName = Path.Combine(_directory, filename);
                _logger.Debug($"Saved messages to disk: {fullName}");
                SentMessagesCount++;
                File.WriteAllBytes(fullName, message.Body);
            }
            return Task.CompletedTask;
        }

        private const string kFilenamePattern = "{Timestamp}_{Id}.json";
        private readonly string _directory;
        private readonly ILogger _logger;
    }
}