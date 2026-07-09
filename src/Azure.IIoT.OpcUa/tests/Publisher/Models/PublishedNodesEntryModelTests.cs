// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Config.Models
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using Xunit;

    public class PublishedNodesEntryModelTests
    {
        [Fact]
        public void UseSecurityDeserializationTest()
        {

            var modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            var model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Null(model.UseSecurity);

            modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "UseSecurity": false,
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.False(model.UseSecurity);

            modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "UseSecurity": true,
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.True(model.UseSecurity);
        }

        [Fact]
        public void UseSecuritySerializationTest()
        {

            var model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcNodes =
                [
                    new()
                    {
                        Id = "i=2258"
                    }
                ]
            };

            var modeJson = Json.SerializeToString(model);
            Assert.DoesNotContain("\"UseSecurity\":false", modeJson, StringComparison.Ordinal);

            model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                UseSecurity = false,
                OpcNodes =
                [
                    new()
                    {
                        Id = "i=2258"
                    }
                ]
            };

            modeJson = Json.SerializeToString(model);
            Assert.Contains("\"UseSecurity\":false", modeJson, StringComparison.Ordinal);

            model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                UseSecurity = true,
                OpcNodes =
                [
                    new()
                    {
                        Id = "i=2258"
                    }
                ]
            };

            modeJson = Json.SerializeToString(model);
            Assert.Contains("\"UseSecurity\":true", modeJson, StringComparison.Ordinal);
        }

        [Fact]
        public void OpcAuthenticationModeDeserializationTest()
        {

            var modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            var model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "OpcAuthenticationMode": "anonymous",
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = """

{
    "EndpointUrl": "opc.tcp://localhost:50002",
    "OpcAuthenticationMode": "usernamePassword",
    "OpcNodes": [
        { "Id": "ns=0;i=2261" }
    ]
}

""";

            model = Json.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.UsernamePassword, model.OpcAuthenticationMode);
        }

        [Fact]
        public void OpcAuthenticationModeSerializationTest()
        {

            var model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcNodes = [
                    new() {
                        Id = "i=2258"
                    }
                ]
            };

            var modeJson = Json.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"Anonymous\"", modeJson, StringComparison.Ordinal);

            model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcAuthenticationMode = OpcAuthenticationMode.Anonymous,
                OpcNodes = [
                    new() {
                        Id = "i=2258"
                    }
                ]
            };

            modeJson = Json.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"Anonymous\"", modeJson, StringComparison.Ordinal);

            model = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                OpcNodes = [
                    new() {
                        Id = "i=2258"
                    }
                ]
            };

            modeJson = Json.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"UsernamePassword\"", modeJson, StringComparison.Ordinal);
        }
    }
}
