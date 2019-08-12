// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Moq;
    using Xunit;
    using System.Threading;

    public class TrustGroupsControllerTest {

        // Execute this code before every test
        public TrustGroupsControllerTest() {

            // This is a dependency of the controller, that we mock, so that
            // we can test the class in isolation
            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart
            _groups = new Mock<ITrustGroupStore>();
            _services = new Mock<ITrustGroupServices>();

            // By convention we call "target" the class under test
            _target = new TrustGroupsController(_groups.Object, _services.Object);
        }

        [Fact]
        public void ItFetchesAGroupFromTheServiceLayer() {
            var id = "Default";
            var configuration = new TrustGroupRegistrationModel {
                Id = id
            };

            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart

            // Arrange
            _groups.Setup(x => x.GetGroupAsync(id, CancellationToken.None))
                .ReturnsAsync(configuration);

            // Act
            var result = _target.GetGroupAsync(id).Result;

            // Verify
            _groups.Verify(x => x.GetGroupAsync(
                It.Is<string>(s => s == id), CancellationToken.None), Times.Once);
        }

        private readonly Mock<ITrustGroupStore> _groups;
        private readonly Mock<ITrustGroupServices> _services;
        private readonly TrustGroupsController _target;
    }
}
