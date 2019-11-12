// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Util.Uds
{
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class HttpBufferedStream : Stream
    {
        const char CR = '\r';
        const char LF = '\n';
        readonly BufferedStream innerStream;

        public HttpBufferedStream(Stream stream)
        {
            innerStream = new BufferedStream(stream);
        }

        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => innerStream.CanSeek;

        public override bool CanWrite => innerStream.CanWrite;

        public override long Length => innerStream.Length;

        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            int position = 0;
            var buffer = new byte[1];
            bool crFound = false;
            var builder = new StringBuilder();
            while (true)
            {
                int length = await innerStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (length == 0)
                {
                    throw new IOException("Unexpected end of stream.");
                }

                if (crFound && (char)buffer[position] == LF)
                {
                    builder.Remove(builder.Length - 1, 1);
                    return builder.ToString();
                }

                builder.Append((char)buffer[position]);
                crFound = (char)buffer[position] == CR;
            }
        }

        public string ReadLine()
        {
            int position = 0;
            var buffer = new byte[1];
            bool crFound = false;
            var builder = new StringBuilder();
            while (true)
            {
                int length = innerStream.Read(buffer, 0, buffer.Length);
                if (length == 0)
                {
                    throw new IOException("Unexpected end of stream.");
                }

                if (crFound && (char)buffer[position] == LF)
                {
                    builder.Remove(builder.Length - 1, 1);
                    return builder.ToString();
                }

                builder.Append((char)buffer[position]);
                crFound = (char)buffer[position] == CR;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            innerStream.Dispose();
        }
    }
}
