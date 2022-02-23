//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Tests.Models {

    using Xunit;
    using System.Collections.Generic;
    using System;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    public class PublishNodesEndpointApiModelTests {

        [Fact]
        public void UseSecurityDeserializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""NodeId"": {
        ""Identifier"": ""ns=0;i=2261""
    }
}
";

            var model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.False(model.UseSecurity);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""UseSecurity"": false,
    ""NodeId"": {
        ""Identifier"": ""ns=0;i=2261""
    }
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.False(model.UseSecurity);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""UseSecurity"": true,
    ""NodeId"": {
        ""Identifier"": ""ns=0;i=2261""
    }
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.True(model.UseSecurity);
        }

        [Fact]
        public void UseSecuritySerializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            var modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"useSecurity\":false", modeJson);

            model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                UseSecurity = false,
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"useSecurity\":false", modeJson);

            model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                UseSecurity = true,
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"useSecurity\":true", modeJson);
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

            var model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.Equal(AuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcAuthenticationMode"": ""anonymous"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.Equal(AuthenticationMode.Anonymous, model.OpcAuthenticationMode);

            modelJson = @"
{
    ""EndpointUrl"": ""opc.tcp://localhost:50002"",
    ""OpcAuthenticationMode"": ""usernamePassword"",
    ""OpcNodes"": [
        { ""Identifier"": ""ns=0;i=2261"" }
    ]
}
";

            model = newtonSoftJsonSerializer.Deserialize<PublishNodesEndpointApiModel>(modelJson);
            Assert.Equal(AuthenticationMode.UsernamePassword, model.OpcAuthenticationMode);
        }

        [Fact]
        public void OpcAuthenticationModeSerializationTest() {
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();

            var model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            var modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"opcAuthenticationMode\":\"anonymous\"", modeJson);

            model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcAuthenticationMode = AuthenticationMode.Anonymous,
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"opcAuthenticationMode\":\"anonymous\"", modeJson);

            model = new PublishNodesEndpointApiModel {
                EndpointUrl = "opc.tcp://localhost:50000",
                OpcAuthenticationMode = AuthenticationMode.UsernamePassword,
                OpcNodes = new List<PublishedNodeApiModel> {
                    new PublishedNodeApiModel {
                        Id = "i=2258"
                    }
                }
            };

            modeJson = newtonSoftJsonSerializer.SerializeToString(model);
            Assert.Contains("\"opcAuthenticationMode\":\"usernamePassword\"", modeJson);
        }
    }
}
