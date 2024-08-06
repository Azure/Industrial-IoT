// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ReadTests<T>
    {
        /// <summary>
        /// Create tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public ReadTests(Func<IFileSystemServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task ReadFileTest0Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 1 * 1024 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    var buffer = new byte[256 * 1024];
                    await stream.Result.ReadExactlyAsync(buffer, ct).ConfigureAwait(false);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }

                    await stream.Result.ReadExactlyAsync(buffer, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task ReadFileTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(1024, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);
                    Assert.False(fi.Result.Writable);

                    var buffer = new byte[1024];
                    var read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(read, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }

                    read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(0, read);
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
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task ReadFileTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 1 * 1024 * 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(1 * 1024 * 1024, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);
                    Assert.False(fi.Result.Writable);

                    var buffer = new byte[256 * 1024];
                    var read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(read, buffer.Length);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }

                    read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(buffer.Length, read);
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
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task ReadFileTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);

                Assert.Null(stream.ErrorInfo);
                Assert.NotNull(stream.Result);
                await using (var _ = stream.Result.ConfigureAwait(false))
                {
                    var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.NotNull(fi.Result);
                    Assert.Equal(1024, fi.Result.Size);
                    Assert.Equal(1, fi.Result.OpenCount);
                    Assert.False(fi.Result.Writable);

                    var buffer = new byte[2 * 1024];
                    var read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(1024, read);
                    for (var i = 0; i < read; i++)
                    {
                        Assert.Equal((byte)i, buffer[i]);
                    }

                    read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                    Assert.Equal(0, read);
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
                }
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task ReadFileTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var file = CreateFile(path, "testfile", 1024);

                var fileNodeId = $"nsu=FileSystem;s=2:{file}";
                var stream1 = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = fileNodeId
                }, ct).ConfigureAwait(false);
                Assert.Null(stream1.ErrorInfo);
                Assert.NotNull(stream1.Result);
                await using (var __ = stream1.Result.ConfigureAwait(false))
                {
                    var stream = await services.OpenReadAsync(_connection, new FileSystemObjectModel
                    {
                        NodeId = fileNodeId
                    }, ct).ConfigureAwait(false);

                    Assert.Null(stream.ErrorInfo);
                    Assert.NotNull(stream.Result);
                    await using (var _ = stream.Result.ConfigureAwait(false))
                    {
                        var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                        {
                            NodeId = fileNodeId
                        }, ct).ConfigureAwait(false);

                        Assert.NotNull(fi.Result);
                        Assert.Equal(1024, fi.Result.Size);
                        Assert.Equal(2, fi.Result.OpenCount);
                        Assert.False(fi.Result.Writable);

                        var buffer = new byte[2 * 1024];
                        var read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                        Assert.Equal(1024, read);
                        for (var i = 0; i < read; i++)
                        {
                            Assert.Equal((byte)i, buffer[i]);
                        }

                        read = await stream.Result.ReadAsync(buffer, ct).ConfigureAwait(false);
                        Assert.Equal(0, read);
                    }
                    {
                        // Now check it is closed
                        var fi = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
                        {
                            NodeId = fileNodeId
                        }, ct).ConfigureAwait(false);

                        Assert.NotNull(fi.Result);
                        Assert.Equal(1, fi.Result.OpenCount);
                        Assert.False(fi.Result.Writable);
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
