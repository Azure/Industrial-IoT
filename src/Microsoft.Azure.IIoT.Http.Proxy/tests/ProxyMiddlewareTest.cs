// Copyright (c) Microsoft. All rights reserved.

namespace ProxyAgent.Test {

    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Proxy;
    using Microsoft.Azure.IIoT.Services.Http.Proxy;
    using Moq;
    using Xunit;
    public class ProxyMiddlewareTest {
        [Fact]
        public void UsesProxy() {

            // Arrange
            var next = new Mock<RequestDelegate>();
            var proxy = new Mock<IProxy>();
            var log = new Mock<ILogger>();

            var request = new Mock<HttpRequest>();
            var response = new Mock<HttpResponse>();
            var context = new Mock<HttpContext>();
            context.SetupGet(x => x.Request).Returns(request.Object);
            context.SetupGet(x => x.Response).Returns(response.Object);

            var target = new ProxyMiddleware(next.Object, proxy.Object);

            // Act
            target.Invoke(context.Object).Wait();

            // Assert
            proxy.Verify(x => x.ForwardAsync(request.Object, response.Object), Times.Once);
        }
    }
}