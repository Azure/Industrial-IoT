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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Additional tests for <see cref="DotHttpFileParser"/> covering directive
    /// success-paths and edge cases not exercised by DotHttpFileParserTests.
    /// </summary>
    public sealed class DotHttpFileParserMoreTests : IDisposable
    {
        public DotHttpFileParserMoreTests()
        {
            _root = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                "DotHttpFileParserMoreTests_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(_root))
            {
                System.IO.Directory.Delete(_root, recursive: true);
            }
        }

        // ── @timeout directive (TryGetDuration – success path) ───────────────

        [Fact]
        public async Task TimeoutDirectiveWithLargeValueDoesNotTriggerTimeoutAsync()
        {
            var invocations = new List<Invocation>();

            // A very large timeout value — the request completes before it fires.
            var output = await ParseAsync("""
                // @timeout 999999
                FastMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("FastMethod", invocations[0].Method.String);
        }

        // ── @delay directive (TryGetDuration – success path with 0-second delay) ──

        [Fact]
        public async Task DelayDirectiveZeroSecondsDoesNotDelay()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                // @delay 0
                NoDelayMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("NoDelayMethod", invocations[0].Method.String);
        }

        // ── @no-log directive with a value throws FormatException ────────────

        [Fact]
        public async Task NoLogDirectiveWithValueThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @no-log unexpected
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // ── @continue-on-error with value throws FormatException ─────────────

        [Fact]
        public async Task ContinueOnErrorDirectiveWithValueThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @continue-on-error unexpected
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // ── @on-error with value throws FormatException ───────────────────────

        [Fact]
        public async Task OnErrorDirectiveWithValueThrowsFormatExceptionAsync()
        {
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @on-error unexpected
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // ── CONNECT HTTP method ───────────────────────────────────────────────

        [Fact]
        public async Task ParsesHttpConnectMethodAsync()
        {
            var invocations = new List<Invocation>();

            await ParseAsync("""
                CONNECT http://proxy:443 HTTP/1.1

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Equal("CONNECT", invocations[0].Method.String);
        }

        // ── @name directive ───────────────────────────────────────────────────

        [Fact]
        public async Task NameDirectiveWithEmptyValueThrowsFormatExceptionAsync()
        {
            // @name with no argument should throw
            await Assert.ThrowsAsync<FormatException>(async () =>
                await ParseAsync("""
                    // @name
                    Method

                    """).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // ── Non-json body with json content type is allowed ───────────────────

        [Fact]
        public async Task EmptyBodyWithNonJsonContentTypeIsAllowedAsync()
        {
            // No body → payload is empty, so no "Only json content type supported" error
            var invocations = new List<Invocation>();

            await ParseAsync("""
                Upload
                Content-Type: application/octet-stream

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Empty(invocations[0].Payload);
        }

        // ── Hash-comment directive (# prefix) ────────────────────────────────

        [Fact]
        public async Task HashCommentIsWrittenToOutputAsync()
        {
            var invocations = new List<Invocation>();

            var output = await ParseAsync("""
                # hash comment
                SimpleMethod

                """, Capture(invocations)).ConfigureAwait(false);

            Assert.Single(invocations);
            Assert.Contains("# hash comment", output);
        }

        // ── @retries with success on second attempt ───────────────────────────

        [Fact]
        public async Task RetriesSucceedOnSecondAttemptAsync()
        {
            var invocations = new List<Invocation>();
            var statusCodes = new Queue<int>([500, 204]);

            await ParseAsync("""
                // @retries 1
                RetryMethod

                """, Capture(invocations, nextStatus: () => statusCodes.Dequeue()))
                .ConfigureAwait(false);

            Assert.Equal(2, invocations.Count);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private Task<string> ParseAsync(string request, Execute? execute = null,
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
