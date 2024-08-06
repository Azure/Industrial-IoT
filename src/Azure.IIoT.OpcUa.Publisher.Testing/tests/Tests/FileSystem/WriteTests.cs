// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class WriteTests<T>
    {
        /// <summary>
        /// Create tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public WriteTests(Func<IFileSystemServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task WriteFileTest0Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 8 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, FileWriteMode.Create, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    // Now write file
                    var buffer = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
                    await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                }
                {
                    var buffer = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                    Assert.Equal(1024, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task WriteFileTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 8 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.NotNull(fi.Result);
                Assert.Equal(8 * 1024, fi.Result.Size);
                Assert.Equal(0, fi.Result.OpenCount);

                var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, FileWriteMode.Create, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(0, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);

                    // Now write file
                    var buffer = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
                    await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                }
                {
                    // Now check it is closed
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(0, fi.Result.OpenCount);
                    Assert.True(fi.Result.Writable);
                    Assert.Equal(1024, fi.Result.Size);

                    var buffer = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                    Assert.Equal(1024, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task WriteFileTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 2 * 1024 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.NotNull(fi.Result);
                Assert.Equal(2 * 1024 * 1024, fi.Result.Size);
                Assert.Equal(0, fi.Result.OpenCount);

                var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, FileWriteMode.Write, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(2 * 1024 * 1024, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);

                    // Now write first half of file
                    var buffer = new byte[1 * 1024 * 1024];
                    await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                }
                {
                    // Now check it is closed
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(0, fi.Result.OpenCount);
                    Assert.True(fi.Result.Writable);
                    Assert.Equal(2 * 1024 * 1024, fi.Result.Size);

                    var buffer = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                    Assert.Equal(2 * 1024 * 1024, buffer.Length);
                    for (var i = 0; i < buffer.Length / 2; i++)
                    {
                        Assert.Equal(0, buffer[i]);
                    }
                    for (var i = buffer.Length / 2; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task AppendFileTest0Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 8 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, FileWriteMode.Append, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    // Now write file
                    var buffer = Enumerable.Range(8 * 1024, 2 * 1024).Select(b => (byte)b).ToArray();
                    await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                }
                {
                    var buffer = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                    Assert.Equal(10 * 1024, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }


        public async Task AppendFileTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 8 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.NotNull(fi.Result);
                Assert.Equal(8 * 1024, fi.Result.Size);
                Assert.Equal(0, fi.Result.OpenCount);

                var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, FileWriteMode.Append, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(8 * 1024, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);

                    // Now write file
                    var buffer = Enumerable.Range(8 * 1024, 2 * 1024).Select(b => (byte)b).ToArray();
                    await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                }
                {
                    // Now check it is closed
                    fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(0, fi.Result.OpenCount);
                    Assert.True(fi.Result.Writable);
                    Assert.Equal(10 * 1024, fi.Result.Size);

                    var buffer = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                    Assert.Equal(10 * 1024, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task AppendFileTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = Path.Combine(path, "testfile");
                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                await File.Create(file).DisposeAsync().ConfigureAwait(false);

                for (var i = 0; i < 10; i++)
                {
                    var stream = await services.OpenWriteAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, FileWriteMode.Append, ct).ConfigureAwait(false);

                    Assert.Null(stream.ErrorInfo);
                    Assert.NotNull(stream.Result);
                    await using (var _ = stream.Result.ConfigureAwait(false))
                    {
                        // Now write file
                        var buffer = new byte[130000];
                        Array.Fill(buffer, (byte)i);
                        await stream.Result.WriteAsync(buffer, ct).ConfigureAwait(false);
                    }
                }
                {
                    // Now check it is closed
                    var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(0, fi.Result.OpenCount);
                    Assert.True(fi.Result.Writable);
                    Assert.Equal(10 * 130000, fi.Result.Size);

                    var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await using var _ = fs.ConfigureAwait(false);
                    for (var i = 0; i < 10; i++)
                    {
                        fs.Seek(i * 130000, SeekOrigin.Begin);
                        Assert.Equal(i, fs.ReadByte());
                    }
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        private static string CreateFile(string path, string name, long length)
        {
            var fullPath = Path.Combine(path, name);
            using var f = File.Create(fullPath);
            var buffer = new byte[length];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)i;
            }
            f.Write(buffer);
            return fullPath;
        }

        private readonly T _connection;
        private readonly Func<IFileSystemServices<T>> _services;
    }
}
