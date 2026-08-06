// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Servers
{
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class DotHttpFileParserTests : IDisposable
    {
        public DotHttpFileParserTests()
        {
            _root = Path.Combine(Directory.GetCurrentDirectory(),
                "DotHttpFileParserTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Fact]
        public async Task ParsesHttpRequestLineIntoMethodUriAndProtocolAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                post http://localhost/rpc/call?api-version=1 HTTP/1.1

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("POST", invocations[0].Method.String);
            Assert.Equal("/rpc/call?api-version=1",
                invocations[0].Method.Uri?.PathAndQuery);
            Assert.Equal("HTTP/1.1", invocations[0].Method.ProtocolVersion);
            Assert.Contains("204", output);
        }

        [Fact]
        public async Task ParsesRpcMethodLineWithoutUriAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                Publish_V1

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("Publish_V1", invocations[0].Method.String);
            Assert.Null(invocations[0].Method.Uri);
            Assert.Null(invocations[0].Method.ProtocolVersion);
        }

        [Fact]
        public async Task PassesHeadersUsingOrdinalIgnoreCaseLookupAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                POST /rpc HTTP/1.1
                content-type: application/json
                X-Correlation-Id: 42

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            // Verify the parser's dictionary comparer, not one supplied by the test.
            Assert.Same(StringComparer.OrdinalIgnoreCase,
                invocations[0].Headers.Comparer);
            Assert.Equal("application/json", invocations[0].Headers["Content-Type"]);
            Assert.Equal("42", invocations[0].Headers["x-correlation-id"]);
        }

        [Fact]
        public async Task PassesInlineJsonBodyToExecuteAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                POST /rpc HTTP/1.1
                Content-Type: application/json

                { "value": 42 }
                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("""{ "value": 42 }""",
                Encoding.UTF8.GetString(invocations[0].Payload));
        }

        [Fact]
        public async Task ParsesMultipleRequestsSeparatedByDelimiterAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                FirstMethod

                ###
                SecondMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Equal(2, invocations.Count);
            Assert.Equal("FirstMethod", invocations[0].Method.String);
            Assert.Equal("SecondMethod", invocations[1].Method.String);
            Assert.Contains("###", output);
        }

        [Fact]
        public async Task IgnoresCommentsAndBlankLinesBeforeRequestAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""

                // a comment
                # another comment

                GetStatus

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("GetStatus", invocations[0].Method.String);
            Assert.Contains("// a comment", output);
            Assert.Contains("# another comment", output);
        }

        [Fact]
        public async Task WritesReturnedStatusAndJsonBodyToResponseStreamAsync()
        {
            var output = await ParseAsync("""
                GetStatus

                """, Capture([], 207, """{"state":"accepted"}""")).ConfigureAwait(false);

            Assert.Contains("207", output);
            Assert.Contains("""{"state":"accepted"}""", output);
        }

        [Fact]
        public async Task NoLogDirectiveSuppressesResponseOutputAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                // @no-log
                HiddenMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal(string.Empty, output);
        }

        [Fact]
        public async Task OnErrorDirectiveSkipsRequestAfterSuccessfulRequestAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                First

                ###
                // @on-error
                OnlyOnError

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("First", invocations[0].Method.String);
            Assert.Contains("// @skipped reason = success", output);
        }

        [Fact]
        public async Task ContinueOnErrorDirectiveAllowsFollowingRequestAsync()
        {
            var invocations = new List<Invocation>();
            var statusCodes = new Queue<int>([500, 204]);

            await ParseAsync("""
                // @continue-on-error
                Failing

                ###
                AfterFailure

                """, Capture(invocations, nextStatus: () => statusCodes.Dequeue()))
                .ConfigureAwait(false);

            Assert.Equal(2, invocations.Count);
            Assert.Equal("Failing", invocations[0].Method.String);
            Assert.Equal("AfterFailure", invocations[1].Method.String);
        }

        [Fact]
        public async Task UnknownDirectiveIsIgnoredAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @unknown
                MethodWithUnknownDirective

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("MethodWithUnknownDirective", invocations[0].Method.String);
        }

        [Fact]
        public async Task RetriesDirectiveUsesDirectiveNameForLookupAsync()
        {
            var invocations = new List<Invocation>();
            var statusCodes = new Queue<int>([500, 204]);

            var output = await ParseAsync("""
                // @retries 1
                RetryMethod

                """, Capture(invocations, nextStatus: () => statusCodes.Dequeue()))
                .ConfigureAwait(false);

            Assert.Equal(2, invocations.Count);
            Assert.Equal("RetryMethod", invocations[0].Method.String);
            Assert.Equal("RetryMethod", invocations[1].Method.String);
            Assert.Contains("// @retry attempt = 1", output);
            Assert.Contains("// @retry attempt = 2", output);
        }

        [Fact]
        public async Task ReadsRequestBodyFromFileAsync()
        {
            var invocations = new List<Invocation>();
            await File.WriteAllTextAsync(Path.Combine(_root, "payload.json"),
                """{"from":"file"}""").ConfigureAwait(false);

            await ParseAsync("""
                FileInput
                Content-Type: application/json

                < payload.json
                """, Capture(invocations), root: _root).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("""{"from":"file"}""",
                Encoding.UTF8.GetString(invocations[0].Payload));
        }

        [Fact]
        public async Task WritesResponseBodyToFileAsync()
        {
            var output = await ParseAsync("""
                FileOutput
                Content-Type: application/json

                >> response.json
                """, Capture([], response: """{"to":"file"}"""), root: _root)
                .ConfigureAwait(false);

            Assert.Contains("204", output);
            Assert.Equal("""{"to":"file"}""",
                await File.ReadAllTextAsync(Path.Combine(_root, "response.json"))
                    .ConfigureAwait(false));
        }

        [Fact]
        public async Task ThrowsFormatExceptionForBadRequestLineAsync()
        {
            var exception = await Assert.ThrowsAsync<FormatException>(
                async () => await ParseAsync("""
                    BREW /coffee HTTP/1.1

                    """).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.Contains("Invalid method format", exception.Message);
        }

        [Fact]
        public async Task ThrowsFormatExceptionForInvalidHeaderAsync()
        {
            var exception = await Assert.ThrowsAsync<FormatException>(
                async () => await ParseAsync("""
                    GET /rpc HTTP/1.1
                    MissingColon

                    """).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.Contains("Invalid header", exception.Message);
        }

        [Fact]
        public async Task ThrowsFormatExceptionForInlineNonJsonBodyAsync()
        {
            var exception = await Assert.ThrowsAsync<FormatException>(
                async () => await ParseAsync("""
                    Upload
                    Content-Type: application/octet-stream

                    bytes
                    """).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.Contains("Only json content type supported inline",
                exception.Message);
        }

        [Fact]
        public async Task PropagatesCancellationToExecuteDelegateAsync()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync().ConfigureAwait(false);

            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await ParseAsync("""
                    CancelMe

                    """, (method, request, headers, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult((204, default(ReadOnlySequence<byte>)));
                }, ct: cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task NameDirectiveWithValueIsAcceptedAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @name MyRequest
                NamedMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("NamedMethod", invocations[0].Method.String);
        }

        [Fact]
        public async Task NameDirectiveWithoutValueThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @name
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task RetriesDirectiveWithNonIntegerThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @retries notanumber
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ConnectionTimeoutDirectiveIsSkippedAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @connection-timeout
                Method

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
        }

        [Fact]
        public async Task SkipsSubsequentRequestAfterFailureWithoutOnErrorAsync()
        {
            var invocations = new List<Invocation>();
            var output = await ParseAsync("""
                FirstMethod

                ###
                SecondMethod

                """, Capture(invocations, status: 500)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("FirstMethod", invocations[0].Method.String);
            Assert.Contains("// @skipped reason = error", output);
        }

        [Fact]
        public async Task AppendToFileAppendsResponseAsync()
        {
            var file = Path.Combine(_root, "append.json");
            await File.WriteAllTextAsync(file, "first").ConfigureAwait(false);

            await ParseAsync("""
                AppendMethod
                Content-Type: application/json

                >>! append.json
                """, Capture([], response: "second"), root: _root)
                .ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(file).ConfigureAwait(false);
            Assert.Equal("firstsecond", content);
        }

        [Fact]
        public async Task StreamOverloadProducesOutputAsync()
        {
            var invocations = new List<Invocation>();
            var req = new System.IO.MemoryStream(
                System.Text.Encoding.UTF8.GetBytes("StreamMethod\n\n"));
            var res = new System.IO.MemoryStream();
            await using (req.ConfigureAwait(false))
            await using (res.ConfigureAwait(false))
            {
                await DotHttpFileParser.ParseAsync(req, res,
                    Capture(invocations), NullLogger.Instance)
                    .ConfigureAwait(false);
            }

            Assert.Single(invocations);
            Assert.Equal("StreamMethod", invocations[0].Method.String);
        }

        [Fact]
        public async Task ParsesHttpGetMethodAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                GET http://localhost/data HTTP/1.1

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("GET", invocations[0].Method.String);
        }

        [Fact]
        public async Task ParsesHttpPutDeletePatchMethodsAsync()
        {
            foreach (var method in new[] { "PUT", "DELETE", "PATCH" })
            {
                var invocations = new List<Invocation>();
                await ParseAsync($"""
                    {method} http://localhost/resource HTTP/1.1

                    """, Capture(invocations)).ConfigureAwait(false);

                Assert.Single(invocations);
                Assert.Equal(method, invocations[0].Method.String);
            }
        }

        [Fact]
        public async Task ParsesHttpOptionsAndHeadMethodsAsync()
        {
            foreach (var method in new[] { "OPTIONS", "HEAD", "TRACE" })
            {
                var invocations = new List<Invocation>();
                await ParseAsync($"""
                    {method} http://localhost/resource HTTP/1.1

                    """, Capture(invocations)).ConfigureAwait(false);

                Assert.Single(invocations);
                Assert.Equal(method, invocations[0].Method.String);
            }
        }

        [Fact]
        public async Task DelayDirectiveWithNonIntegerThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @delay notanumber
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TimeoutDirectiveWithNonIntegerThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @timeout notanumber
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task NoCookieJarDirectiveIsSkippedAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @no-cookie-jar
                Method

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
        }

        [Fact]
        public async Task NoRedirectDirectiveIsSkippedAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @no-redirect
                Method

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
        }

        [Fact]
        public async Task OnErrorDirectiveRunsRequestAfterPreviousFailureAsync()
        {
            var invocations = new List<Invocation>();
            var statusCodes = new Queue<int>([500, 204]);

            await ParseAsync("""
                FailingMethod

                ###
                // @on-error
                ErrorHandler

                """, Capture(invocations, nextStatus: () => statusCodes.Dequeue()))
                .ConfigureAwait(false);

            Assert.Equal(2, invocations.Count);
            Assert.Equal("FailingMethod", invocations[0].Method.String);
            Assert.Equal("ErrorHandler", invocations[1].Method.String);
        }

        [Fact]
        public async Task RetriesExhaustedLeaveExecutionInFailedStateAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                // @retries 1
                RetryMethod

                ###
                AfterExhausted

                """, Capture(invocations, status: 500)).ConfigureAwait(false);

            // Both retry attempts ran, then next request is skipped due to failure.
            Assert.Equal(2, invocations.Count);
            Assert.Equal("RetryMethod", invocations[0].Method.String);
            Assert.Contains("// @skipped reason = error", output);
        }

        private static Task<string> ParseAsync(string request, Execute? execute = null,
            string? root = null, CancellationToken ct = default)
        {
            return DotHttpFileParser.ParseAsync(request,
                execute ?? Capture([]), NullLogger.Instance, root, ct: ct);
        }

        private static Execute Capture(List<Invocation> invocations,
            int status = 204, string response = "",
            Func<int>? nextStatus = null)
        {
            return (method, request, headers, ct) =>
            {
                invocations.Add(new Invocation(method, request.ToArray(), headers));
                return Task.FromResult((nextStatus?.Invoke() ?? status,
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(response))));
            };
        }

        private sealed record class Invocation(Method Method, byte[] Payload,
            Dictionary<string, string> Headers);

        private readonly string _root;
    }
}
