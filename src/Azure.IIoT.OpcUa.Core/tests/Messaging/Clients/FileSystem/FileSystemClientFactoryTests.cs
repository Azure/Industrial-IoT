// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.IO;
    using Xunit;

    public sealed class FileSystemClientFactoryTests
    {
        [Fact]
        public void NameIdentifiesFileSystemTransport()
        {
            var factory = new FileSystemClientFactory(
                NullLogger<FileSystemClientFactory>.Instance);

            Assert.Equal("FileSystem", factory.Name);
        }

        [Fact]
        public void CreateEventClientCreatesOutputFolderAndClient()
        {
            var outputFolder = CreateUniqueOutputFolder();
            var factory = new FileSystemClientFactory(
                NullLogger<FileSystemClientFactory>.Instance);

            try
            {
                using var registration = factory.CreateEventClient(outputFolder,
                    out var client);

                Assert.Equal(true, Directory.Exists(outputFolder));
                Assert.Equal("FileSystem", client.Name);
            }
            finally
            {
                DeleteIfExists(outputFolder);
            }
        }

        [Fact]
        public void CreateEventClientReferenceCountsClientsByFullPath()
        {
            var outputFolder = CreateUniqueOutputFolder();
            var factory = new FileSystemClientFactory(
                NullLogger<FileSystemClientFactory>.Instance);

            try
            {
                var firstRegistration = factory.CreateEventClient(outputFolder,
                    out var first);
                var secondRegistration = factory.CreateEventClient(outputFolder,
                    out var second);
                firstRegistration.Dispose();
                var thirdRegistration = factory.CreateEventClient(outputFolder,
                    out var third);
                secondRegistration.Dispose();
                thirdRegistration.Dispose();
                using var fourthRegistration = factory.CreateEventClient(outputFolder,
                    out var fourth);

                Assert.Same(first, second);
                Assert.Same(first, third);
                Assert.NotSame(first, fourth);
            }
            finally
            {
                DeleteIfExists(outputFolder);
            }
        }

        private static string CreateUniqueOutputFolder()
        {
            return Path.Combine(AppContext.BaseDirectory, "filesystem-client-factory",
                Guid.NewGuid().ToString("N"));
        }

        private static void DeleteIfExists(string outputFolder)
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }
        }
    }
}
