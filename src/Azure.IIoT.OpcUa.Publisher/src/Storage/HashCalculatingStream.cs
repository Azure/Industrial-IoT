// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using System;
    using System.IO;
    using System.IO.Hashing;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Create hashing stream
    /// </summary>
    internal class HashCalculatingStream : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => _inner.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => _inner.CanWrite;

        /// <inheritdoc/>
        public override long Length => _inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        /// <summary>
        /// Get hash and reset
        /// </summary>
        public ReadOnlyMemory<byte> HashAndReset => _hash.GetHashAndReset();

        /// <summary>
        /// Create hashing stream
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="hash"></param>
        public HashCalculatingStream(Stream inner,
            NonCryptographicHashAlgorithm hash)
        {
            _inner = inner;
            _hash = hash;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            _inner.Flush();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            _hash.Append(buffer.AsSpan().Slice(offset, read));
            return read;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _hash.Append(buffer.AsSpan().Slice(offset, count));
            _inner.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken)
        {
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            var read = await _inner.ReadAsync(buffer, offset,
                count, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            if (read != -1)
            {
                _hash.Append(buffer.AsSpan().Slice(offset, read));
            }
            return read;
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read != -1)
            {
                _hash.Append(buffer.Span[..read]);
            }
            return read;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            var read = _inner.ReadByte();
            if (read != -1)
            {
                var b = (byte)read;
                _hash.Append(new ReadOnlySpan<byte>(ref b));
            }
            return read;
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _hash.Append(buffer);
            _inner.Write(buffer);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken)
        {
            _hash.Append(buffer.AsSpan(offset, count));
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            _hash.Append(buffer.Span);
            return _inner.WriteAsync(buffer, cancellationToken);
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            _hash.Append(new ReadOnlySpan<byte>(ref value));
            _inner.WriteByte(value);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            _inner.Close();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Cannot seek hash stream");
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length");
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }

        private readonly Stream _inner;
        private readonly NonCryptographicHashAlgorithm _hash;
    }
}
