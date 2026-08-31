// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Models
{
    using Opc.Ua;
    using System.Text.Json;
    using Xunit;

    public sealed class EncodeableJTokenTests
    {
        [Fact]
        public void ConstructorClonesJsonElementStorage()
        {
            EncodeableJToken token;
            using (var document = JsonDocument.Parse("""{"name":"value"}"""))
            {
                token = new EncodeableJToken(document.RootElement,
                    (ExpandedNodeId)"i=1");
            }

            Assert.Equal("value", token.JToken.GetProperty("name").GetString());
        }
    }
}
