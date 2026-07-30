// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using System;
    using System.Globalization;
    using Xunit;

    /// <summary>
    /// Assertions over nodes a test server materialises from the type model.
    /// </summary>
    internal static class InstanceNodeAssert
    {
        /// <summary>
        /// Asserts that a node identifier belongs to the given instance
        /// namespace, without pinning the number the server assigned it.
        /// </summary>
        /// <remarks>
        /// Instance nodes take their identifier from a counter that the node
        /// manager advances once per node <c>NodeState.Create</c> materialises
        /// from the type model. Every identifier therefore moves whenever the
        /// stack changes how many children a type has, which is what happened
        /// when the pinned stack moved: with the address space otherwise
        /// identical, one alarm went from 1 to 93, another from 162 to 521, and
        /// the second boiler from 1 to 50.
        ///
        /// The number records allocation order rather than identity, so pinning
        /// it documents the type model of whichever stack the fixture last ran
        /// against and nothing about the behaviour under test. The tests that
        /// used it already assert the node's browse name, display name and node
        /// class, which is what actually identifies it; this asserts the one
        /// remaining property the number carried, that the node is owned by the
        /// expected namespace.
        /// </remarks>
        /// <param name="nodeId">The node identifier to check.</param>
        /// <param name="namespaceUri">The owning namespace.</param>
        public static void IsInstanceNodeOf(string? nodeId, string namespaceUri)
        {
            Assert.NotNull(nodeId);
            var prefix = namespaceUri + "#i=";
            Assert.StartsWith(prefix, nodeId, StringComparison.Ordinal);
            Assert.True(uint.TryParse(nodeId[prefix.Length..], NumberStyles.None,
                CultureInfo.InvariantCulture, out var identifier),
                $"'{nodeId}' does not carry a numeric identifier.");
            Assert.NotEqual(0u, identifier);
        }
    }
}
