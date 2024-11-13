// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class BrowseTests<T>
    {
        /// <summary>
        /// Create metadata tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="tempPath"></param>
        public BrowseTests(Func<IFileSystemServices<T>> services, T connection, string tempPath)
        {
            _services = services;
            _connection = connection;
            _tempPath = tempPath;
        }

        public async Task GetFileSystemsTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var drives = DriveInfo.GetDrives().Select(d => d.Name).ToHashSet();
            await foreach (var fs in services.GetFileSystemsAsync(_connection, ct).ConfigureAwait(false))
            {
                Assert.Null(fs.ErrorInfo);
                Assert.NotNull(fs.Result);
                Assert.NotNull(fs.Result.Name);
                Assert.True(drives.Remove(fs.Result.Name),
                    $"{fs.Result.Name} not found in {string.Join('\n', drives)}");
            }
            Assert.Empty(drives);
        }

        public async Task GetDirectoriesTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);
            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            Assert.Empty(directories.Result);
        }

        public async Task GetDirectoriesTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = Path.Combine(path, Path.GetRandomFileName());
            Directory.CreateDirectory(path2);
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            var item = Assert.Single(directories.Result);
            Assert.NotNull(item);
            Assert.Equal(Path.GetFileName(path2), item.Name);
        }

        public async Task GetDirectoriesTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            for (var i = 0; i < 10; i++)
            {
                var path2 = Path.Combine(path, i.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(path2);
            }
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            var result = directories.Result.ToList();
            Assert.Equal(10, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal(i.ToString(CultureInfo.InvariantCulture), item));
        }

        public async Task GetDirectoriesTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            for (var i = 0; i < 10; i++)
            {
                CreateFile(path, i.ToString(CultureInfo.InvariantCulture), 1024);
            }

            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            Assert.Empty(directories.Result);
        }

        public async Task GetDirectoriesTest5Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.NotNull(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            Assert.Empty(directories.Result);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, directories.ErrorInfo.StatusCode);
        }

        public async Task GetDirectoriesTest6Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var p3 = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2, p3);
            Directory.CreateDirectory(path);
            var path2 = Path.Combine(path, Path.GetRandomFileName());
            Directory.CreateDirectory(path2);

            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string>
                {
                    $"nsu=FileSystem;{p1}",
                    $"nsu=FileSystem;{p2}",
                    $"nsu=FileSystem;{p3}"
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            var item = Assert.Single(directories.Result);
            Assert.NotNull(item);
            Assert.Equal(Path.GetFileName(path2), item.Name);
        }

        public async Task GetFilesTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            for (var i = 10; i < 20; i++)
            {
                var path2 = Path.Combine(path, i.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(path2);
            }
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            Assert.Empty(files.Result);
        }

        public async Task GetFilesTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            for (var i = 0; i < 10; i++)
            {
                CreateFile(path, i.ToString(CultureInfo.InvariantCulture), 1024);
            }

            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            var result = files.Result.ToList();
            Assert.Equal(10, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal(i.ToString(CultureInfo.InvariantCulture), item));
        }

        public async Task GetFilesTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            for (var i = 0; i < 10; i++)
            {
                CreateFile(path, i.ToString(CultureInfo.InvariantCulture), 1024);
            }
            for (var i = 10; i < 20; i++)
            {
                var path2 = Path.Combine(path, i.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(path2);
            }
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            var result = files.Result.ToList();
            Assert.Equal(10, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal(i.ToString(CultureInfo.InvariantCulture), item));
        }

        public async Task GetFilesTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            for (var i = 0; i < 5; i++)
            {
                CreateFile(path, i.ToString(CultureInfo.InvariantCulture), 1024);
            }
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            var result = files.Result.ToList();
            Assert.Equal(5, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal(i.ToString(CultureInfo.InvariantCulture), item));

            for (var i = 5; i < 10; i++)
            {
                CreateFile(path, i.ToString(CultureInfo.InvariantCulture), 1024);
            }

            files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            result = files.Result.ToList();
            Assert.Equal(10, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal(i.ToString(CultureInfo.InvariantCulture), item));

            for (var i = 0; i < 6; i++)
            {
                var path2 = Path.Combine(path, i.ToString(CultureInfo.InvariantCulture));
                File.Delete(path2);
            }

            files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            result = files.Result.ToList();
            Assert.Equal(4, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Name));
            Assert.All(result.Select(r => r.Name).Order(),
                (item, i) => Assert.Equal((i + 6).ToString(CultureInfo.InvariantCulture), item));

            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);
            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            Assert.Empty(files.Result);
        }

        public async Task GetFilesTest5Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";

            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.NotNull(files.Result);
            Assert.Empty(files.Result);
            Assert.NotNull(files.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, files.ErrorInfo.StatusCode);
        }

        public async Task GetFilesTest6Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var p3 = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2, p3);
            Directory.CreateDirectory(path);
            CreateFile(path, "test", 1000);
            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p1}", $"nsu=FileSystem;{p2}", $"nsu=FileSystem;{p3}" }
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            var item = Assert.Single(files.Result);
            Assert.Equal("test", item.Name);
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
        private readonly string _tempPath;
        private readonly Func<IFileSystemServices<T>> _services;
    }
}
