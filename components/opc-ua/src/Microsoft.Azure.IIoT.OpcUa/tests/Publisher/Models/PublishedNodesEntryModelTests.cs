// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Tests.Publisher.Config.Models {

    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class PublishedNodesEntryModelTests {

        [Fact]
        public void UseSecurityDeserializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            var model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.False(model.UseSecurity);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""UseSecurity"": false,
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.False(model.UseSecurity);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""UseSecurity"": true,
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.True(model.UseSecurity);
        }

        [Fact]
        public void UseSecuritySerializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            var modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"UseSecurity\":false", modeJson);

            model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                UseSecurity = false,
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"UseSecurity\":false", modeJson);

            model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                UseSecurity = true,
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"UseSecurity\":true", modeJson);
        }

        [Fact]
        public void OpcAuthenticationModeDeserializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            var model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcAuthenticationMode"": ""anonymous"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcAuthenticationMode"": ""usernamePassword"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishedNodesEntryModel>(modelJson);
            Assert.Equal(OpcAuthenticationMode.UsernamePassword, model.OpcAuthenticationMode);
        }

        [Fact]
        public void OpcAuthenticationModeSerializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            var modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"anonymous\"", modeJson);

            model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                OpcAuthenticationMode = OpcAuthenticationMode.Anonymous,
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"anonymous\"", modeJson);

            model = new PublishedNodesEntryModel {
                EndpointUrl = new Uri("opc.tcp://localhost:50000"),
                OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword,
                OpcNodes = new List<OpcNodeModel> {
                    new OpcNodeModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"OpcAuthenticationMode\":\"usernamePassword\"", modeJson);
        }
    }
}
