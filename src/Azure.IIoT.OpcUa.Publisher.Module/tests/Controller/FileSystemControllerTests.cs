// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class FileSystemControllerTests
    {
        [Fact]
        public async Task GetDirectoriesDelegatesConnectionAndRequestAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);
            var connection = CreateConnection();
            var directory = new FileSystemObjectModel { NodeId = "directory" };
            var expected = new ServiceResponse<IEnumerable<FileSystemObjectModel>>
            {
                Result = new[] { new FileSystemObjectModel { NodeId = "child" } }
            };
            service.Setup(s => s.GetDirectoriesAsync(connection, directory,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.GetDirectoriesAsync(
                new RequestEnvelope<FileSystemObjectModel>
                {
                    Connection = connection,
                    Request = directory
                });

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task CreateFileRejectsBlankNameBeforeCallingServiceAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => controller.CreateFileAsync(
                CreateEnvelope(), " "));

            service.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteFileOrDirectoryCreatesChildObjectFromNodeIdAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);
            var envelope = CreateEnvelope();
            var expected = new ServiceResultModel { StatusCode = 0 };

            service.Setup(s => s.DeleteFileSystemObjectAsync(envelope.Connection,
                    It.Is<FileSystemObjectModel>(file => file.NodeId == "child-node"),
                    envelope.Request!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.DeleteFileOrDirectoryAsync(envelope, "child-node");

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task DeleteFileSystemObjectDelegatesExistingObjectAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);
            var envelope = CreateEnvelope();
            var expected = new ServiceResultModel { StatusCode = 0 };

            service.Setup(s => s.DeleteFileSystemObjectAsync(envelope.Connection,
                    envelope.Request!, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            var actual = await controller.DeleteFileSystemObjectAsync(envelope);

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task DownloadWithoutHttpContextIsNotSupportedAsync()
        {
            var controller = new FileSystemController(
                new Mock<IFileSystemServices<ConnectionModel>>().Object);

            var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
                controller.DownloadAsync(Serialize(CreateConnection()),
                    Serialize(new FileSystemObjectModel { NodeId = "file" })));

            Assert.Equal("Download not supported", exception.Message);
        }

        [Fact]
        public async Task UploadWithoutHttpContextIsNotSupportedAsync()
        {
            var controller = new FileSystemController(
                new Mock<IFileSystemServices<ConnectionModel>>().Object);

            var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
                controller.UploadAsync(Serialize(CreateConnection()),
                    Serialize(new FileSystemObjectModel { NodeId = "file" }),
                    Serialize(new FileOpenWriteOptionsModel())));

            Assert.Equal("Upload not supported", exception.Message);
        }

        [Fact]
        public async Task DownloadCopiesServiceErrorToResponseHeaderAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            var error = new ServiceResultModel
            {
                StatusCode = 123,
                ErrorMessage = "open failed"
            };

            service.Setup(s => s.OpenReadAsync(It.IsAny<ConnectionModel>(),
                    It.Is<FileSystemObjectModel>(file => file.NodeId == "file"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<Stream> { ErrorInfo = error })
                .Verifiable();

            await controller.DownloadAsync(Serialize(CreateConnection()),
                Serialize(new FileSystemObjectModel { NodeId = "file" }), httpContext);

            Assert.Equal(StatusCodes.Status500InternalServerError,
                httpContext.Response.StatusCode);
            var errorInfo = Assert.Single(httpContext.Response.Headers["errorInfo"]);
            Assert.Contains("open failed", errorInfo, StringComparison.Ordinal);
            service.Verify();
        }

        [Fact]
        public async Task UploadCopiesServiceErrorToResponseHeaderAsync()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream([1, 2, 3]);
            var error = new ServiceResultModel
            {
                StatusCode = 456,
                ErrorMessage = "write failed"
            };

            service.Setup(s => s.OpenWriteAsync(It.IsAny<ConnectionModel>(),
                    It.Is<FileSystemObjectModel>(file => file.NodeId == "file"),
                    It.IsAny<FileOpenWriteOptionsModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<Stream> { ErrorInfo = error })
                .Verifiable();

            await controller.UploadAsync(Serialize(CreateConnection()),
                Serialize(new FileSystemObjectModel { NodeId = "file" }),
                Serialize(new FileOpenWriteOptionsModel()), httpContext);

            Assert.Equal(StatusCodes.Status500InternalServerError,
                httpContext.Response.StatusCode);
            var errorInfo = Assert.Single(httpContext.Response.Headers["errorInfo"]);
            Assert.Contains("write failed", errorInfo, StringComparison.Ordinal);
            service.Verify();
        }

        [Theory]
        [InlineData(null, "file", "mode")]
        [InlineData("connection", null, "mode")]
        [InlineData("connection", "file", null)]
        [InlineData(" ", "file", "mode")]
        [InlineData("connection", " ", "mode")]
        [InlineData("connection", "file", " ")]
        public async Task UploadRejectsMissingHeaderPayloadsAsync(
            string connectionJson, string fileObjectJson, string writeOptionsJson)
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);

            await Assert.ThrowsAnyAsync<ArgumentException>(() => controller.UploadAsync(
                connectionJson!, fileObjectJson!, writeOptionsJson!,
                new DefaultHttpContext()));

            service.VerifyNoOtherCalls();
        }

        [Fact]
        public void GetFileSystemsRejectsNullConnection()
        {
            var service = new Mock<IFileSystemServices<ConnectionModel>>(MockBehavior.Strict);
            var controller = new FileSystemController(service.Object);

            Assert.Throws<ArgumentNullException>(() =>
                controller.GetFileSystemsAsync(null!));

            service.VerifyNoOtherCalls();
        }

        private static RequestEnvelope<FileSystemObjectModel> CreateEnvelope()
        {
            return new RequestEnvelope<FileSystemObjectModel>
            {
                Connection = CreateConnection(),
                Request = new FileSystemObjectModel { NodeId = "parent" }
            };
        }

        private static ConnectionModel CreateConnection()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                }
            };
        }

        private static string Serialize<T>(T value)
        {
            return Json.SerializeToString(value, Json.GetTypeInfo<T>());
        }
    }
}
