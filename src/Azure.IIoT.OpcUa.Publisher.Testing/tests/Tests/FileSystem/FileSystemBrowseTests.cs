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

    public class FileSystemBrowseTests<T>
    {
        /// <summary>
        /// Create metadata tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public FileSystemBrowseTests(Func<IFileSystemServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task GetFileSystemsTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var drives = DriveInfo.GetDrives().ToHashSet();
            var found = new HashSet<string>();
            await foreach (var fs in services.GetFileSystemsAsync(_connection, ct))
            {
                Assert.NotNull(fs.ErrorInfo);
                Assert.Equal(0u, fs.ErrorInfo.StatusCode);
                Assert.NotNull(fs.Result);
                Assert.NotNull(fs.Result.Name);
                Assert.Contains(drives, d => d.RootDirectory.FullName == fs.Result?.Name);
                found.Add(fs.Result.Name);
            }
            // TODO: Assert.True(drives.Count <= found.Count);
        }

        public async Task GetDirectoriesTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);
                Assert.Empty(directories);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetDirectoriesTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                var path2 = Path.Combine(path, Path.GetRandomFileName());
                Directory.CreateDirectory(path2);
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                var item = Assert.Single(directories);
                Assert.NotNull(item.Result);
                Assert.Equal(Path.GetFileName(path2), item.Result.Name);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetDirectoriesTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    Directory.CreateDirectory(path2);
                }
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                var result = directories.ToList();
                Assert.Equal(10, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal(i.ToString(), item));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetDirectoriesTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    using var _ = File.Create(path2);
                }

                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);
                Assert.Empty(directories);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetDirectoriesTest5Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);
            var error = Assert.Single(directories);
            Assert.Null(error.Result);
            Assert.NotNull(error.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, error.ErrorInfo.StatusCode);
        }

        public async Task GetFilesTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 10; i < 20; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    Directory.CreateDirectory(path2);
                }
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                Assert.Empty(files);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetFilesTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    using var _ = File.Create(path2);
                }

                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                var result = files.ToList();
                Assert.Equal(10, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal(i.ToString(), item));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetFilesTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    using var _ = File.Create(path2);
                }
                for (var i = 10; i < 20; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    Directory.CreateDirectory(path2);
                }
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var found = new HashSet<string>();
                var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                var result = files.ToList();
                Assert.Equal(10, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal(i.ToString(), item));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetFilesTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    using var _ = File.Create(path2);
                }
                var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

                var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                var result = files.ToList();
                Assert.Equal(5, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal(i.ToString(), item));

                for (var i = 5; i < 10; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    using var _ = File.Create(path2);
                }

                files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                result = files.ToList();
                Assert.Equal(10, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal(i.ToString(), item));

                for (var i = 0; i < 6; i++)
                {
                    var path2 = Path.Combine(path, i.ToString());
                    File.Delete(path2);
                }

                files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);

                result = files.ToList();
                Assert.Equal(4, result.Count);
                Assert.All(result, item => Assert.NotNull(item.Result?.Name));
                Assert.All(result.Select(r => r.Result!.Name).Order(),
                    (item, i) => Assert.Equal((i + 6).ToString(), item));

                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
                {
                    NodeId = directoryNodeId
                }, ct).ConfigureAwait(false);
                Assert.Empty(files);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        public async Task GetFilesTest5Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);
            var error = Assert.Single(files);
            Assert.Null(error.Result);
            Assert.NotNull(error.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, error.ErrorInfo.StatusCode);
        }

        private readonly T _connection;
        private readonly Func<IFileSystemServices<T>> _services;
    }
}
