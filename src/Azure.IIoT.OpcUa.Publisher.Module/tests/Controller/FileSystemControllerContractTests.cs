// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class FileSystemControllerContractTests
    {
        [Fact]
        public async Task DownloadFailureWritesServiceResultContractAsync()
        {
            var error = new ServiceResultModel
            {
                StatusCode = 1,
                ErrorMessage = "read failed"
            };
            var files = new Mock<IFileSystemServices<ConnectionModel>>();
            files.Setup(x => x.OpenReadAsync(It.IsAny<ConnectionModel>(),
                    It.IsAny<FileSystemObjectModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<Stream> { ErrorInfo = error });
            var controller = new FileSystemController(files.Object);
            var context = new DefaultHttpContext();

            await controller.DownloadAsync("{}", "{}", context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.True(context.Response.Headers.TryGetValue("errorInfo", out StringValues header));
            Assert.Equal(error, Json.Deserialize(header.ToString(),
                Json.GetTypeInfo<ServiceResultModel>()));
        }
    }
}
