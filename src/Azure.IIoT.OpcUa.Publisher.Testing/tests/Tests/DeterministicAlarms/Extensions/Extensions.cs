// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace DeterministicAlarms.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using System.Threading.Tasks;

    internal static class Extensions
    {
        public static async Task NodeShouldHaveStatesAsync<T>(this INodeServices<T> server,
            T connectionId, string node, string activeState, string enabledState)
        {
            await server.NodeShouldHaveStateAsync(connectionId, node, "ActiveState",
                activeState).ConfigureAwait(false);
            await server.NodeShouldHaveStateAsync(connectionId, node, "EnabledState",
                enabledState).ConfigureAwait(false);
        }

        private static async Task NodeShouldHaveStateAsync<T>(this INodeServices<T> server,
            T connectionId, string node, string state, string expectedValue)
        {
            var value = await server.ValueReadAsync(connectionId, new ValueReadRequestModel
            {
                NodeId = node,
                BrowsePath = new[] { "." + state }
            }).ConfigureAwait(false);

            var text = value?.Value?["Text"];
            text.Should().NotBeNull().And.Be(expectedValue,
                "{0} should be {1}", state, expectedValue);
            var loc = value?.Value?["Locale"];
            loc.Should().NotBeNull().And.Be("en-US");
        }
    }
}
