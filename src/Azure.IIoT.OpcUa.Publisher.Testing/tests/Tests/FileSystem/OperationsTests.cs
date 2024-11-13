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
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class OperationsTests<T>
    {
        /// <summary>
        /// Create tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="tempPath"></param>
        public OperationsTests(Func<IFileSystemServices<T>> services, T connection, string tempPath)
        {
            _services = services;
            _connection = connection;
            _tempPath = tempPath;
        }

        public async Task CreateDirectoryTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var directory = await services.CreateDirectoryAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, "testdirectory", ct).ConfigureAwait(false);

            Assert.Null(directory.ErrorInfo);
            Assert.True(Directory.Exists(Path.Combine(path, "testdirectory")));

            var directories = await services.GetDirectoriesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(directories.ErrorInfo);
            Assert.NotNull(directories.Result);
            var item = Assert.Single(directories.Result);
            Assert.Equal(item, directory.Result);
        }

        public async Task CreateDirectoryTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.Combine(path, "testdirectory"));

            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var directory = await services.CreateDirectoryAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, "testdirectory", ct).ConfigureAwait(false);

            Assert.NotNull(directory.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadBrowseNameDuplicated, directory.ErrorInfo.StatusCode);
        }

        public async Task CreateDirectoryTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var p3 = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2, p3);
            Directory.CreateDirectory(path);
            Assert.Empty(Directory.GetDirectories(path));

            var directory = await services.CreateDirectoryAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p1}", $"nsu=FileSystem;{p2}", $"nsu=FileSystem;{p3}" }
            }, "testdir", ct).ConfigureAwait(false);

            Assert.Null(directory.ErrorInfo);
            Assert.NotEmpty(Directory.GetDirectories(path));
            Assert.True(Directory.Exists(Path.Combine(path, "testdir")));
        }

        public async Task CreateDirectoryTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var p3 = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2, p3);
            Directory.CreateDirectory(path);
            Assert.Empty(Directory.GetDirectories(path));

            var directory = await services.CreateDirectoryAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p1}", $"nsu=FileSystem;{p2}", "nsu=FileSystem;Bad" }
            }, "testdir", ct).ConfigureAwait(false);

            Assert.NotNull(directory.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadNotFound, directory.ErrorInfo.StatusCode);
            Assert.Empty(Directory.GetDirectories(path));
        }

        public async Task DeleteDirectoryTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = Path.Combine(path, "testDirectory");
            Directory.CreateDirectory(Path.Combine(path, "testDirectory"));

            var parentDirectoryId = $"nsu=FileSystem;s=1:{path}";

            Assert.NotEmpty(Directory.GetDirectories(path));

            var nodeToDelete = $"nsu=FileSystem;s=1:{path2}";
            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = nodeToDelete
                },
                new FileSystemObjectModel
                {
                    NodeId = parentDirectoryId
                }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(0u, result.StatusCode);
            Assert.Empty(Directory.GetDirectories(path));
        }

        public async Task DeleteDirectoryTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = Path.Combine(path, "testDirectory");
            Directory.CreateDirectory(Path.Combine(path, "testDirectory"));

            Assert.NotEmpty(Directory.GetDirectories(path));

            var nodeToDelete = $"nsu=FileSystem;s=1:{path2}";
            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = nodeToDelete
                }, ct: ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(0u, result.StatusCode);
            Assert.Empty(Directory.GetDirectories(path));
        }

        public async Task DeleteDirectoryTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var parentDirectoryId = $"nsu=FileSystem;s=1:{path}";

            var fileToDeleteId = $"nsu=FileSystem;s=1:{Path.Combine(path, "wrong")}";
            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = fileToDeleteId
                },
                new FileSystemObjectModel
                {
                    NodeId = parentDirectoryId
                }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(Opc.Ua.StatusCodes.BadNotFound, result.StatusCode);
        }

        public async Task CreateFileTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var file = await services.CreateFileAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, "testfile", ct).ConfigureAwait(false);

            Assert.Null(file.ErrorInfo);
            Assert.True(File.Exists(Path.Combine(path, "testfile")));

            var files = await services.GetFilesAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(files.ErrorInfo);
            Assert.NotNull(files.Result);
            var item = Assert.Single(files.Result);
            Assert.Equal(item, file.Result);
        }

        public async Task CreateFileTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var directoryNodeId = $"nsu=FileSystem;s=1:{path}";
            var file = await services.CreateFileAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, "testfile", ct).ConfigureAwait(false);

            Assert.Null(file.ErrorInfo);
            Assert.True(File.Exists(Path.Combine(path, "testfile")));

            var file2 = await services.CreateFileAsync(_connection, new FileSystemObjectModel
            {
                NodeId = directoryNodeId
            }, "testfile", ct).ConfigureAwait(false);

            Assert.Null(file2.Result);
            Assert.NotNull(file2.ErrorInfo);
            Assert.Equal(Opc.Ua.StatusCodes.BadBrowseNameDuplicated, file2.ErrorInfo.StatusCode);
        }

        public async Task CreateFileTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var f = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2);
            Directory.CreateDirectory(path);

            Assert.Empty(Directory.GetFiles(path));

            var file = await services.CreateFileAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p1}", $"nsu=FileSystem;{p2}" }
            }, "testfile", ct).ConfigureAwait(false);

            Assert.Null(file.ErrorInfo);
            Assert.True(File.Exists(Path.Combine(path, "testfile")));
        }

        public async Task CreateFileTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2);
            Directory.CreateDirectory(path);

            Assert.Empty(Directory.GetFiles(path));

            var file = await services.CreateFileAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{root}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p1}", "nsu=FileSystem;Bad" }
            }, "testfile", ct).ConfigureAwait(false);

            Assert.NotNull(file.ErrorInfo);
        }

        public async Task GetFileInfoTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = CreateFile(path, "testfile", 1024);
            var fileNodeId = $"nsu=FileSystem;s=2:{path2}";
            var fileInfo = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
            {
                NodeId = fileNodeId
            }, ct).ConfigureAwait(false);

            Assert.Null(fileInfo.ErrorInfo);
            Assert.NotNull(fileInfo.Result);
            Assert.Equal(1024, fileInfo.Result.Size);
            Assert.True(fileInfo.Result.Writable);
            Assert.Equal(0, fileInfo.Result.OpenCount);
        }

        public async Task GetFileInfoTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var fileNodeId = $"nsu=FileSystem;s=2:{Path.Combine(path, "bad")}";
            var fileInfo = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
            {
                NodeId = fileNodeId
            }, ct).ConfigureAwait(false);

            Assert.NotNull(fileInfo.ErrorInfo);
            Assert.Null(fileInfo.Result);
        }

        public async Task GetFileInfoTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var f = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2);
            Directory.CreateDirectory(path);
            CreateFile(path, f, 100);
            var fileInfo = await services.GetFileInfoAsync(_connection, new FileSystemObjectModel
            {
                NodeId = $"nsu=FileSystem;s=1:{Path.Combine(root, p1)}",
                BrowsePath = new List<string> { $"nsu=FileSystem;{p2}", $"nsu=FileSystem;{f}" }
            }, ct).ConfigureAwait(false);

            Assert.Null(fileInfo.ErrorInfo);
            Assert.NotNull(fileInfo.Result);
            Assert.Equal(100, fileInfo.Result.Size);
            Assert.True(fileInfo.Result.Writable);
            Assert.Equal(0, fileInfo.Result.OpenCount);
        }

        public async Task DeleteFileTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = CreateFile(path, "testfile", 1024);
            var fileToDeleteId = $"nsu=FileSystem;s=2:{path2}";

            Assert.NotEmpty(Directory.GetFiles(path));

            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = fileToDeleteId
                }, ct: ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(0u, result.StatusCode);
            Assert.Empty(Directory.GetFiles(path));
        }

        public async Task DeleteFileTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var path2 = CreateFile(path, "testfile", 1024);

            Assert.NotEmpty(Directory.GetFiles(path));

            var parentDirectoryId = $"nsu=FileSystem;s=1:{path}";

            var fileToDeleteId = $"nsu=FileSystem;s=2:{path2}";
            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = fileToDeleteId
                },
                new FileSystemObjectModel
                {
                    NodeId = parentDirectoryId
                }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(0u, result.StatusCode);
            Assert.Empty(Directory.GetFiles(path));
        }

        public async Task DeleteFileTest3Async(CancellationToken ct = default)
        {
            var services = _services();

            var path = Path.Combine(_tempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            var parentDirectoryId = $"nsu=FileSystem;s=1:{path}";

            var fileToDeleteId = $"nsu=FileSystem;s=2:{Path.Combine(path, "wrong")}";
            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = fileToDeleteId
                },
                new FileSystemObjectModel
                {
                    NodeId = parentDirectoryId
                }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(Opc.Ua.StatusCodes.BadNotFound, result.StatusCode);
        }

        public async Task DeleteFileTest4Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var f = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2);
            Directory.CreateDirectory(path);
            CreateFile(path, f, 100);

            Assert.NotEmpty(Directory.GetFiles(path));

            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = $"nsu=FileSystem;s=1:{Path.Combine(root, p1)}",
                    BrowsePath = new List<string> { $"nsu=FileSystem;{p2}", $"nsu=FileSystem;{f}" }
                }, ct: ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(0u, result.StatusCode);
            Assert.Empty(Directory.GetFiles(path));
        }

        public async Task DeleteFileTest5Async(CancellationToken ct = default)
        {
            var services = _services();

            var root = _tempPath;
            var p1 = Path.GetRandomFileName();
            var p2 = Path.GetRandomFileName();
            var f = Path.GetRandomFileName();

            var path = Path.Combine(root, p1, p2);
            Directory.CreateDirectory(path);
            CreateFile(path, f, 100);
            Assert.NotEmpty(Directory.GetFiles(path));

            var result = await services.DeleteFileSystemObjectAsync(_connection,
                new FileSystemObjectModel
                {
                    NodeId = $"nsu=FileSystem;s=1:{Path.Combine(root, p1)}",
                    BrowsePath = new List<string> { $"nsu=FileSystem;{p2}", "nsu=FileSystem;Notexisting" }
                }, ct: ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(Opc.Ua.StatusCodes.BadNotFound, result.StatusCode);
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
