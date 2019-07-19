// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapter for streams
    /// </summary>
    public class StreamAdapter : Stream, IDisposable {

        /// <inheritdoc/>
        public override bool CanRead => _inner.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanTimeout => _inner.CanTimeout;

        /// <inheritdoc/>
        public override bool CanWrite => _inner.CanWrite;

        /// <inheritdoc/>
        public override long Length => _inner.Length;

        /// <inheritdoc/>
        public override long Position {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        /// <inheritdoc/>
        public override int ReadTimeout {
            get => _inner.ReadTimeout;
            set => _inner.ReadTimeout = value;
        }

        /// <inheritdoc/>
        public override int WriteTimeout {
            get => _inner.WriteTimeout;
            set => _inner.WriteTimeout = value;
        }

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="inner"></param>
        public StreamAdapter(Stream inner) {
            _inner = inner ??
                throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc/>
        public override object InitializeLifetimeService() {
            return _inner.InitializeLifetimeService();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) {
            return _inner.Seek(offset, origin);
        }

        /// <inheritdoc/>
        public override void SetLength(long value) {
            _inner.SetLength(value);
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer,
            int offset, int count, AsyncCallback callback, object state) {
            return _inner.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer,
            int offset, int count, AsyncCallback callback, object state) {
            return _inner.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult) {
            return _inner.EndRead(asyncResult);
        }

        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) {
            _inner.EndWrite(asyncResult);
        }

        /// <inheritdoc/>
        public override void Close() {
            _inner.Close();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) {
            return _inner.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override int ReadByte() {
            return _inner.ReadByte();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) {
            _inner.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value) {
            _inner.WriteByte(value);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken) {
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken) {
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken) {
            return _inner.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) {
            return _inner.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override void Flush() {
            _inner.Flush();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return _inner.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return _inner.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString() {
            return _inner.ToString();
        }

        /// <inheritdoc/>
        public new virtual void Dispose() {
            _inner.Dispose();
        }

        /// <summary>
        /// The inner stream
        /// </summary>
        protected readonly Stream _inner;
    }

}

