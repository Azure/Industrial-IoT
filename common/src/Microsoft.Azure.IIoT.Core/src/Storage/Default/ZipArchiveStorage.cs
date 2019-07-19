// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a way to create or open zip archives.
    /// </summary>
    public sealed class ZipArchiveStorage : IArchiveStorage {

        /// <inheritdoc/>
        public Task<IArchive> OpenAsync(string path, FileMode mode,
            FileAccess access) {

            var archive = new ZipArchiveWrapper(
                new ZipArchive(
                    new FileStream(path, mode),
                    access == FileAccess.Read ? ZipArchiveMode.Read :
                        ZipArchiveMode.Update, false),
                CompressionLevel.Optimal);

            return Task.FromResult<IArchive>(archive);
        }

        /// <inheritdoc/>
        private class ZipArchiveWrapper : IArchive {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="zip"></param>
            /// <param name="compressionLevel"></param>
            public ZipArchiveWrapper(ZipArchive zip,
                CompressionLevel compressionLevel) {
                _zip = zip;
                _compressionLevel = compressionLevel;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _zip.Dispose();
            }

            /// <inheritdoc/>
            public Task CloseAsync() {
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Stream GetStream(string name, FileMode mode) {
                var entry = _zip.GetEntry(name);
                switch (mode) {
                    case FileMode.CreateNew:
                        entry?.Delete();
                        entry = _zip.CreateEntry(name, _compressionLevel);
                        break;
                    case FileMode.Create:
                        if (entry != null) {
                            throw new InvalidOperationException("Entry exists");
                        }
                        break;
                    case FileMode.Open:
                        if (entry == null) {
                            throw new InvalidOperationException("Entry not exists");
                        }
                        break;
                    case FileMode.OpenOrCreate:
                        if (entry == null) {
                            entry = _zip.CreateEntry(name, _compressionLevel);
                        }
                        break;
                    default:
                        throw new NotSupportedException("Mode not supported");
                }

                return entry.Open();
            }

            private readonly ZipArchive _zip;
            private readonly CompressionLevel _compressionLevel;
        }
    }
}
