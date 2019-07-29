// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Xunit;

    public class StatusControllerTest : IClassFixture<WebAppFixture> {

        [Fact]
        public async Task TestStatus() {

            // Arrange
            var client = _factory.CreateClient();

            // Act
            using (var response = await client.GetAsync(VersionInfo.PATH + "/status")) {

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal("application/json; charset=utf-8",
                    response.Content.Headers.ContentType.ToString());

                var result = await response.Content.ReadAsStringAsync();
                var status = JsonConvertEx.DeserializeObject<StatusResponseApiModel>(result);
                Assert.Equal("OK:Alive and well", status.Status);
            }
        }

        public StatusControllerTest(WebAppFixture factory) {
            _factory = factory;
        }

        private readonly WebAppFixture _factory;
    }
}
