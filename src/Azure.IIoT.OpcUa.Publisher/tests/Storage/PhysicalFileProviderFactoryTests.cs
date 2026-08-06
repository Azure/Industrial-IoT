// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Storage
{
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.IO;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PhysicalFileProviderFactory"/>.
    /// </summary>
    public sealed class PhysicalFileProviderFactoryTests : IDisposable
    {
        private readonly string _testRoot = Path.Combine("D:\\buildtemp", "PhysFileProviderTests",
            Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testRoot))
                {
                    Directory.Delete(_testRoot, true);
                }
            }
            catch { }
        }

        [Fact]
        public void Create_ExistingDirectory_ReturnsProvider()
        {
            Directory.CreateDirectory(_testRoot);
            using var sut = CreateSut();

            var provider = sut.Create(_testRoot);

            Assert.NotNull(provider);
        }

        [Fact]
        public void Create_NonExistingDirectory_CreatesDirectoryAndReturnsProvider()
        {
            var newDir = Path.Combine(_testRoot, "created-by-factory");
            using var sut = CreateSut();

            var provider = sut.Create(newDir);

            Assert.NotNull(provider);
            Assert.True(Directory.Exists(newDir),
                $"Expected '{newDir}' to be created by the factory.");
        }

        [Fact]
        public void Create_SameDirectoryTwice_ReturnsSameInstance()
        {
            Directory.CreateDirectory(_testRoot);
            using var sut = CreateSut();

            var p1 = sut.Create(_testRoot);
            var p2 = sut.Create(_testRoot);

            Assert.Same(p1, p2);
        }

        [Fact]
        public void Create_DifferentDirectories_ReturnsDifferentInstances()
        {
            var dir1 = Path.Combine(_testRoot, "dir1");
            var dir2 = Path.Combine(_testRoot, "dir2");
            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir2);
            using var sut = CreateSut();

            var p1 = sut.Create(dir1);
            var p2 = sut.Create(dir2);

            Assert.NotSame(p1, p2);
        }

        [Fact]
        public void Create_NullOrWhitespaceRoot_UsesCurrentDirectory()
        {
            using var sut = CreateSut();

            // null → should fall back to Environment.CurrentDirectory
            var p1 = sut.Create(null!);
            var p2 = sut.Create(string.Empty);
            var p3 = sut.Create("   ");

            Assert.NotNull(p1);
            // They should all map to the same resolved path (cwd)
            Assert.Same(p1, p2);
            Assert.Same(p2, p3);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            Directory.CreateDirectory(_testRoot);
            var sut = CreateSut();
            sut.Create(_testRoot);

            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_WithoutAnyCreate_DoesNotThrow()
        {
            var sut = CreateSut();

            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void Create_WithPollingEnabled_ReturnsFunctionalProvider()
        {
            Directory.CreateDirectory(_testRoot);
            var options = Options.Create(new PublisherOptions
            {
                UseFileChangePolling = true
            });
            using var sut = new PhysicalFileProviderFactory(options,
                NullLogger<PhysicalFileProviderFactory>.Instance);

            var provider = sut.Create(_testRoot);
            Assert.NotNull(provider);
        }

        private PhysicalFileProviderFactory CreateSut()
        {
            return new PhysicalFileProviderFactory(
                Options.Create(new PublisherOptions()),
                NullLogger<PhysicalFileProviderFactory>.Instance);
        }
    }
}
