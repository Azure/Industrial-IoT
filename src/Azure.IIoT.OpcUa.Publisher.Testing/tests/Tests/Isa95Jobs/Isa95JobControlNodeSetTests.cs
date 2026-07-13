// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using Xunit;

    /// <summary>
    /// Guards the complete ISA-95 Job Control NodeSet2 input.
    /// </summary>
    public sealed class Isa95JobControlNodeSetTests
    {
        [Fact]
        public void IsTheOfficialIsa95JobControlTwoPointZeroNodeSet()
        {
            var bytes = File.ReadAllBytes(GetNodeSetPath());

            Assert.Equal(kOfficialSha256, Convert.ToHexString(SHA256.HashData(bytes)));
        }

        [Fact]
        public void ContainsTheCompleteModelWithoutBrokenLocalReferences()
        {
            var document = XDocument.Load(GetNodeSetPath());
            var root = Assert.IsType<XElement>(document.Root);
            var ns = root.Name.Namespace;
            var model = Assert.Single(root.Element(ns + "Models")!.Elements(ns + "Model"));
            var nodes = root.Elements().Where(element =>
                element.Name.LocalName.StartsWith("UA", StringComparison.Ordinal)).ToList();
            var nodeIds = nodes
                .Select(node => node.Attribute("NodeId")?.Value)
                .Where(nodeId => nodeId != null)
                .ToHashSet(StringComparer.Ordinal);
            var unresolvedReferences = nodes
                .SelectMany(node => node.Element(ns + "References")?.Elements(ns + "Reference") ?? [])
                .Select(reference => reference.Value.Trim())
                .Where(target => target.StartsWith("ns=1;", StringComparison.Ordinal))
                .Where(target => !nodeIds.Contains(target));

            Assert.Equal("http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/",
                model.Attribute("ModelUri")!.Value);
            Assert.Equal("2.0.0", model.Attribute("Version")!.Value);
            Assert.Equal("2024-01-31T00:00:00Z", model.Attribute("PublicationDate")!.Value);
            Assert.Equal(258, nodes.Count);
            Assert.Equal(134, nodes.Count(node => node.Name.LocalName == "UAVariable"));
            Assert.Contains(nodes, node => node.Attribute("NodeId")!.Value == "ns=1;i=6047" &&
                node.Attribute("BrowseName")!.Value == "1:JobOrder");
            Assert.Contains(nodes, node => node.Attribute("NodeId")!.Value == "ns=1;i=6019" &&
                node.Attribute("BrowseName")!.Value == "NamespaceUri");
            Assert.Empty(unresolvedReferences);
        }

        private static string GetNodeSetPath()
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "UAModel.ISA95_JOBCONTROL_V2.NodeSet2.xml");
        }

        private const string kOfficialSha256 =
            "52327E736BEB604C7253A5A5393D1AD525B9ECF35B71FBACB4F429CACB334064";
    }
}
