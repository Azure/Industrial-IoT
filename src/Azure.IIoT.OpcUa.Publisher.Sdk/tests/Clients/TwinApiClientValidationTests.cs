// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Every twin method validates its connection and its request the same
    /// way, and this sweeps all of them rather than picking representatives.
    /// </summary>
    /// <remarks>
    /// Validation is the branchiest part of these clients - three decisions per
    /// method, none of them exercised by a happy-path test - and it is the part
    /// whose failure is worst: an argument that travels becomes a round trip
    /// and a remote error for something that was knowable locally.
    /// <para>
    /// The methods are swept from one list so that adding a client method and
    /// forgetting to test it is visible here as an omission from the list,
    /// rather than invisible as a test class that was never written.
    /// </para>
    /// </remarks>
    public sealed class TwinApiClientValidationTests : ApiClientTestBase
    {
        private TwinApiClient Client => new(MethodClient.Object, Target);

        /// <summary>
        /// Each method invoked with a caller-supplied connection and a valid
        /// request, so the connection can be made null or endpointless.
        /// </summary>
        private static IEnumerable<(string Name, Func<TwinApiClient, ConnectionModel, Task> Call)> WithConnection()
        {
            yield return ("TestConnection", (c, x) => c.TestConnectionAsync(x, new TestConnectionRequestModel(), default));
            yield return ("NodeBrowseFirst", (c, x) => c.NodeBrowseFirstAsync(x, new BrowseFirstRequestModel(), default));
            yield return ("NodeBrowseNext", (c, x) => c.NodeBrowseNextAsync(x, new BrowseNextRequestModel { ContinuationToken = "t" }, default));
            yield return ("NodeBrowsePath", (c, x) => c.NodeBrowsePathAsync(x, new BrowsePathRequestModel { BrowsePaths = [["a"]] }, default));
            yield return ("NodeRead", (c, x) => c.NodeReadAsync(x, new ReadRequestModel { Attributes = [] }, default));
            yield return ("NodeWrite", (c, x) => c.NodeWriteAsync(x, new WriteRequestModel { Attributes = [] }, default));
            yield return ("NodeValueRead", (c, x) => c.NodeValueReadAsync(x, new ValueReadRequestModel(), default));
            yield return ("NodeValueWrite", (c, x) => c.NodeValueWriteAsync(x, new ValueWriteRequestModel { Value = null }, default));
            yield return ("NodeMethodGetMetadata", (c, x) => c.NodeMethodGetMetadataAsync(x, new MethodMetadataRequestModel(), default));
            yield return ("NodeMethodCall", (c, x) => c.NodeMethodCallAsync(x, new MethodCallRequestModel(), default));
            yield return ("GetServerCapabilities", (c, x) => c.GetServerCapabilitiesAsync(x, null, default));
            yield return ("GetMetadata", (c, x) => c.GetMetadataAsync(x, new NodeMetadataRequestModel(), default));
            yield return ("CompileQuery", (c, x) => c.CompileQueryAsync(x, new QueryCompilationRequestModel { Query = "q" }, default));
            yield return ("HistoryGetServerCapabilities", (c, x) => c.HistoryGetServerCapabilitiesAsync(x, null, default));
            yield return ("HistoryGetConfiguration", (c, x) => c.HistoryGetConfigurationAsync(x, new HistoryConfigurationRequestModel { NodeId = "i=1" }, default));
            yield return ("HistoryRead", (c, x) => c.HistoryReadAsync(x, new HistoryReadRequestModel<JsonNode> { Details = JsonValue.Create(1)!, NodeId = "i=1" }, default));
            yield return ("HistoryReadNext", (c, x) => c.HistoryReadNextAsync(x, new HistoryReadNextRequestModel { ContinuationToken = "t" }, default));
            yield return ("HistoryUpdate", (c, x) => c.HistoryUpdateAsync(x, new HistoryUpdateRequestModel<JsonNode> { Details = JsonValue.Create(1)! }, default));
        }

        /// <summary>
        /// Each method invoked with a valid connection and a null request.
        /// </summary>
        private static IEnumerable<(string Name, Func<TwinApiClient, Task> Call)> WithNullRequest()
        {
            var connection = Valid();
            yield return ("TestConnection", c => c.TestConnectionAsync(connection, null!, default));
            yield return ("NodeBrowseFirst", c => c.NodeBrowseFirstAsync(connection, null!, default));
            yield return ("NodeBrowseNext", c => c.NodeBrowseNextAsync(connection, null!, default));
            yield return ("NodeBrowsePath", c => c.NodeBrowsePathAsync(connection, null!, default));
            yield return ("NodeRead", c => c.NodeReadAsync(connection, null!, default));
            yield return ("NodeWrite", c => c.NodeWriteAsync(connection, null!, default));
            yield return ("NodeValueRead", c => c.NodeValueReadAsync(connection, null!, default));
            yield return ("NodeValueWrite", c => c.NodeValueWriteAsync(connection, null!, default));
            yield return ("NodeMethodGetMetadata", c => c.NodeMethodGetMetadataAsync(connection, null!, default));
            yield return ("NodeMethodCall", c => c.NodeMethodCallAsync(connection, null!, default));
            yield return ("GetMetadata", c => c.GetMetadataAsync(connection, null!, default));
            yield return ("CompileQuery", c => c.CompileQueryAsync(connection, null!, default));
            yield return ("HistoryGetConfiguration", c => c.HistoryGetConfigurationAsync(connection, null!, default));
            yield return ("HistoryRead", c => c.HistoryReadAsync(connection, null!, default));
            yield return ("HistoryReadNext", c => c.HistoryReadNextAsync(connection, null!, default));
            yield return ("HistoryUpdate", c => c.HistoryUpdateAsync(connection, null!, default));
        }

        [Fact]
        public async Task EveryMethodRejectsANullConnectionAsync()
        {
            var offenders = await CollectAsync(WithConnection(),
                (call, client) => call(client, null!));

            Assert.Empty(offenders);
        }

        [Fact]
        public async Task EveryMethodRejectsAConnectionWithNoEndpointUrlAsync()
        {
            //
            // The url is what the module dials. A connection without one is
            // undialable, so it is rejected here rather than producing a
            // connect failure that looks like the server is down. Endpoint
            // itself is a required member, so the empty url is the only way to
            // express this.
            //
            var offenders = await CollectAsync(WithConnection(),
                (call, client) => call(client, new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = string.Empty }
                }),
                typeof(ArgumentException));

            Assert.Empty(offenders);
        }

        [Fact]
        public async Task EveryMethodRejectsANullRequestAsync()
        {
            var offenders = new List<string>();
            foreach (var (name, call) in WithNullRequest())
            {
                try
                {
                    await call(Client);
                    offenders.Add($"{name}: no exception");
                }
                catch (ArgumentNullException)
                {
                }
                catch (Exception ex)
                {
                    offenders.Add($"{name}: {ex.GetType().Name}");
                }
            }

            Assert.Empty(offenders);
            AssertNotCalled();
        }

        private async Task<List<string>> CollectAsync(
            IEnumerable<(string Name, Func<TwinApiClient, ConnectionModel, Task> Call)> methods,
            Func<Func<TwinApiClient, ConnectionModel, Task>, TwinApiClient, Task> invoke,
            Type? expected = null)
        {
            expected ??= typeof(ArgumentNullException);
            var offenders = new List<string>();
            foreach (var (name, call) in methods)
            {
                try
                {
                    await invoke(call, Client);
                    offenders.Add($"{name}: no exception");
                }
                catch (Exception ex) when (expected.IsInstanceOfType(ex))
                {
                }
                catch (Exception ex)
                {
                    offenders.Add($"{name}: {ex.GetType().Name}");
                }
            }
            //
            // A rejected argument must not have travelled.
            //
            AssertNotCalled();
            return offenders;
        }

        private static ConnectionModel Valid()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = "opc.tcp://server:4840" }
            };
        }

        [Fact]
        public void TheSweepCoversEveryPublicClientMethod()
        {
            //
            // The point of the sweep is that it is exhaustive, so this asserts
            // it stays that way: a method added to the client without an entry
            // above fails here instead of silently going untested.
            //
            var swept = WithConnection().Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
            var declared = typeof(TwinApiClient).GetMethods()
                .Where(m => m.IsPublic && !m.IsStatic && m.Name.EndsWith("Async", StringComparison.Ordinal))
                .Select(m => m.Name[..^"Async".Length])
                .ToHashSet(StringComparer.Ordinal);

            Assert.Empty(declared.Except(swept));
        }
    }
}
