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
    using System.Security.Cryptography;
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

        private readonly T _connection;
        private readonly Func<IFileSystemServices<T>> _services;
    }
}
