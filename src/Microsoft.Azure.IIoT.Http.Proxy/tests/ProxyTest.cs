// Copyright (c) Microsoft. All rights reserved.

namespace ProxyAgent.Test {

    using System;
    using System.Linq;
    using System.Net;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using ProxyAgent.Test.helpers;
    using Xunit;
    using Xunit.Abstractions;
    using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
    using HttpResponse = Microsoft.AspNetCore.Http.HttpResponse;
    public class ProxyTest {
     //  private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
     //
     //  private readonly Mock<IHttpClient> client;
     //  private readonly Mock<IProxyConfig> config;
     //  private readonly Proxy target;
     //
     //  public ProxyTest(ITestOutputHelper log) {
     //      this.client = new Mock<IHttpClient>();
     //      this.config = new Mock<IProxyConfig>();
     //      this.target = new Proxy(this.client.Object, this.config.Object, new TargetLogger(log));
     //  }
     //
     //  /**
     //   * Feature test
     //   */
     //  [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
     //  public void ItPingsTheConfiguredEndpoint() {
     //      // Arrange - Configuration
     //      var remoteEndpoint = "https://" + Guid.NewGuid() + "/";
     //      this.config.SetupGet(x => x.Endpoint).Returns(remoteEndpoint);
     //      // Arrange - Remote endpoint response
     //      var endpointResponse = new Mock<IHttpResponse>();
     //      this.client
     //          .Setup(x => x.GetAsync(It.IsAny<Microsoft.Azure.IoTSolutions.ReverseProxy.HttpClient.HttpRequest>()))
     //          .ReturnsAsync(endpointResponse.Object);
     //
     //      // Act
     //      this.target.PingAsync().Wait(TestTimeout);
     //
     //      // Assert - it sends a request
     //      this.client.Verify(
     //          x => x.GetAsync(It.IsAny<Microsoft.Azure.IoTSolutions.ReverseProxy.HttpClient.HttpRequest>()), Times.Once);
     //      // Assert - it sends a request to the configured remote endpoint
     //      this.client.Verify(
     //          x => x.GetAsync(It.Is<Microsoft.Azure.IoTSolutions.ReverseProxy.HttpClient.HttpRequest>(r => r.Uri.AbsoluteUri.Equals(remoteEndpoint))), Times.Once);
     //      // Assert - it accepts self signed SSL certs (because it uses cert pinning)
     //      this.client.Verify(
     //          x => x.GetAsync(It.Is<Microsoft.Azure.IoTSolutions.ReverseProxy.HttpClient.HttpRequest>(r => r.Options.AllowInsecureSslServer)), Times.Once);
     //  }
     //
     //  /**
     //   * Feature test
     //   */
     //  [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
     //  public void ItRedirectsHttpToHttps() {
     //      // Arrange - HTTPS redirection is enabled in the configuration
     //      this.config.SetupGet(x => x.RedirectHttpToHttps).Returns(true);
     //      // Arrange - HTTP request with path and querystring
     //      var (request, response) = PrepareContext(port: 80, path: "/foo", query: "?bar");
     //
     //      // Act
     //      this.target.ProcessAsync("http://" + request.Host, request, response).Wait(TestTimeout);
     //
     //      // Assert - No request is made to the remote endpoint
     //      this.client.Verify(x => x.GetAsync(It.IsAny<IHttpRequest>()), Times.Never);
     //      // Assert - The client is redirected to HTTPS
     //      Assert.Equal(301, response.StatusCode);
     //      Assert.True(response.Headers.ContainsKey("Location"));
     //      Assert.True(response.Headers["Location"].FirstOrDefault().StartsWith("https://"));
     //      Assert.True(response.Headers["Location"].First().EndsWith("/foo?bar"));
     //  }
     //
     //  /**
     //   * Feature test
     //   */
     //  [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
     //  public void ItDoesntProxySomeHeaders() {
     //      // Arrange
     //      var (request, response) = PrepareContext();
     //
     //      // Arrange - Client request with headers to block
     //      request.Headers["Connection"] = "something";
     //      request.Headers["Content-Length"] = "something";
     //      request.Headers["Keep-Alive"] = "something";
     //      request.Headers["Host"] = "something";
     //      request.Headers["Upgrade"] = "something";
     //      request.Headers["Upgrade-Insecure-Requests"] = "something";
     //
     //      // Arrange - Some legit request headers
     //      request.Headers["Content-Type"] = "one";
     //      request.Headers["Cookie"] = "two";
     //      request.Headers["X-ActivityId"] = "three";
     //      request.Headers["X-CorrelationId"] = "four";
     //
     //      // Arrange - Remote endpoint response with headers to block
     //      var clientResponse = new Mock<IHttpResponse>();
     //      this.client.Setup(x => x.GetAsync(It.IsAny<IHttpRequest>())).ReturnsAsync(clientResponse.Object);
     //      var clientResponseHeaders = new HttpHeadersY
     //      {
     //          { "Connection", "something" },
     //          { "Server", "something" },
     //          { "Transfer-Encoding", "something" },
     //          { "Upgrade", "something" },
     //          { "X-Powered-By", "something" },
     //          { "Strict-Transport-Security", "something" },
     //      };
     //
     //      // Arrange - Some legit response headers
     //      clientResponseHeaders.Add("Content-Type", "one");
     //      clientResponseHeaders.Add("Content-Length", "55555");
     //      clientResponseHeaders.Add("Set-Cookie", "two");
     //      clientResponseHeaders.Add("X-ActivityId", "three");
     //      clientResponseHeaders.Add("X-CorrelationId", "four");
     //      clientResponse.SetupGet(x => x.Headers).Returns(clientResponseHeaders);
     //
     //      // Act
     //      this.target.ProcessAsync("https://" + request.Host, request, response).Wait(TestTimeout);
     //
     //      // Assert - these request headers are allowed
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => r.Headers.Contains("Content-Type"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => r.Headers.Contains("Cookie"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => r.Headers.Contains("X-ActivityId"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => r.Headers.Contains("X-CorrelationId"))), Times.Once);
     //
     //      // Assert - these request headers are blocked
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Connection"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Content-Length"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Keep-Alive"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Host"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Upgrade"))), Times.Once);
     //      this.client.Verify(x => x.GetAsync(It.Is<IHttpRequest>(r => !r.Headers.Contains("Upgrade-Insecure-Requests"))), Times.Once);
     //
     //      // Assert - these response headers are allowed
     //      Assert.Equal("one", response.Headers["Content-Type"].First());
     //      Assert.Equal("55555", response.Headers["Content-Length"].First());
     //      Assert.Equal("two", response.Headers["Set-Cookie"].First());
     //      Assert.Equal("three", response.Headers["X-ActivityId"].First());
     //      Assert.Equal("four", response.Headers["X-CorrelationId"].First());
     //
     //      // Assert - these response headers are blocked
     //      Assert.False(response.Headers.ContainsKey("Connection"));
     //      Assert.False(response.Headers.ContainsKey("Server"));
     //      Assert.False(response.Headers.ContainsKey("Strict-Transport-Security"));
     //      Assert.False(response.Headers.ContainsKey("Transfer-Encoding"));
     //      Assert.False(response.Headers.ContainsKey("Upgrade"));
     //      Assert.False(response.Headers.ContainsKey("X-Powered-By"));
     //  }
     //
     //  /**
     //   * Bugfix test
     //   * https://github.com/Azure/azure-iot-pcs-remote-monitoring-dotnet/issues/12
     //   * https://github.com/Azure/reverse-proxy-dotnet/pull/10
     //   * "Fix duplicate headers causing invalid requests"
     //   */
     //  [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
     //  public void ItDoesntFailInCaseOfDuplicateRequestHeaders() {
     //      // Arrange - Request with duplicate headers
     //      var (request, response) = PrepareContext();
     //      request.Headers.Add("X-MyHeader", "foo");
     //      request.Headers["X-MyHeader"].Append("bar");
     //      request.Headers["X-MyHeader"].Append("baz");
     //      request.Headers.Add("Content-Type", "application/json");
     //      request.Headers["Content-Type"].Append("text/plain");
     //      // Arrange - Remote endpoint response
     //      var statusCode = HttpStatusCode.MultipleChoices;
     //      var clientResponse = new Mock<IHttpResponse>();
     //      clientResponse.SetupGet(x => x.StatusCode).Returns(statusCode);
     //      this.client.Setup(x => x.GetAsync(It.IsAny<IHttpRequest>())).ReturnsAsync(clientResponse.Object);
     //
     //      // Act
     //      this.target.ProcessAsync("https://" + request.Host, request, response).Wait(TestTimeout);
     //
     //      // Assert - the request ran, without exceptions
     //      this.client.Verify(x => x.GetAsync(It.IsAny<IHttpRequest>()), Times.Once);
     //  }
     //
     //  /**
     //   * Bugfix test
     //   * https://github.com/Azure/reverse-proxy-dotnet/issues/17
     //   * "Proxy doesn't support duplicate response headers"
     //   */
     //  [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
     //  public void ItSupportsDuplicateResponseHeaders() {
     //      // Arrange - Remote endpoint response with duplicate headers
     //      var (request, response) = PrepareContext();
     //      var statusCode = HttpStatusCode.MultipleChoices;
     //      var clientResponse = new Mock<IHttpResponse>();
     //      clientResponse.SetupGet(x => x.StatusCode).Returns(statusCode);
     //      var clientResponseHeaders = new HttpHeadersY { { "X-Foo", "one" }, { "X-Foo", "two" }, { "X-Foo", "three" } };
     //      clientResponse.SetupGet(x => x.Headers).Returns(clientResponseHeaders);
     //      this.client.Setup(x => x.GetAsync(It.IsAny<IHttpRequest>())).ReturnsAsync(clientResponse.Object);
     //
     //      // Act
     //      this.target.ProcessAsync("https://" + request.Host, request, response).Wait(TestTimeout);
     //
     //      // Assert - the status code matches
     //      Assert.Equal((int)statusCode, response.StatusCode);
     //      // Assert - all the header values are present
     //      Assert.Equal(3, response.Headers["X-Foo"].Count);
     //      Assert.Contains("one", response.Headers["X-Foo"]);
     //      Assert.Contains("two", response.Headers["X-Foo"]);
     //      Assert.Contains("three", response.Headers["X-Foo"]);
     //  }
     //
     //  // Prepare HTTP context similarly to what ASP.NET does, as far as tests are concerned
     //  private static (HttpRequest, HttpResponse) PrepareContext(
     //      string host = "",
     //      int port = 0,
     //      string path = "/somepath",
     //      string query = "?foo=bar") {
     //      host = string.IsNullOrEmpty(host) ? Guid.NewGuid().ToString() : host;
     //      port = port > 0 ? port : 443;
     //
     //      var context = new DefaultHttpContext();
     //      var request = context.Request;
     //      var response = context.Response;
     //
     //      request.Method = "GET";
     //      request.Host = new HostString(host, port);
     //      request.Path = path;
     //      request.QueryString = new QueryString(query);
     //
     //      return (request, response);
     //  }
    }
}
