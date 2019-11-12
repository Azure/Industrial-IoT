// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Util.Uds
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    class HttpChunkedStreamReader : Stream
    {
        readonly HttpBufferedStream stream;
        int chunkBytes;
        bool eos;

        public HttpChunkedStreamReader(HttpBufferedStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.stream = stream;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (eos)
            {
                return 0;
            }

            if (chunkBytes == 0)
            {
                string line = await stream.ReadLineAsync(cancellationToken);
                if (!int.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chunkBytes))
                {
                    throw new IOException($"Cannot parse chunk header - {line}");
                }
            }

            int bytesRead = 0;
            if (chunkBytes > 0)
            {
                int bytesToRead = Math.Min(count, chunkBytes);
                bytesRead = await stream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
                if (bytesToRead == 0)
                {
                    throw new EndOfStreamException();
                }

                chunkBytes -= bytesToRead;
            }

            if (chunkBytes == 0)
            {
                await stream.ReadLineAsync(cancellationToken);
                if (bytesRead == 0)
                {
                    eos = true;
                }
            }

            return bytesRead;
        }

        public override void Flush() => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (eos)
            {
                return 0;
            }

            if (chunkBytes == 0)
            {
                string line = stream.ReadLine();
                if (!int.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chunkBytes))
                {
                    throw new IOException($"Cannot parse chunk header - {line}");
                }
            }

            int bytesRead = 0;
            if (chunkBytes > 0)
            {
                int bytesToRead = Math.Min(count, chunkBytes);
                bytesRead = stream.Read(buffer, offset, bytesToRead);
                if (bytesToRead == 0)
                {
                    throw new EndOfStreamException();
                }

                chunkBytes -= bytesToRead;
            }

            if (chunkBytes == 0)
            {
                stream.ReadLine();
                if (bytesRead == 0)
                {
                    eos = true;
                }
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}
