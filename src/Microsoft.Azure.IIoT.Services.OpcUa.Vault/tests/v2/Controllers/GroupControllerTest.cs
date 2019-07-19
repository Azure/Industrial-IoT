// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Tests.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Tests.Helpers;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Controllers;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Moq;
    using Xunit;
    using System.Threading;

    public class GroupControllerTest {

        // Execute this code before every test
        // Note: for complex setups, where many dependencies need to be
        // prepared before a test, and this method grows too big:
        // 1. First try to reduce the complexity of the class under test
        // 2. If #1 is not possible, use a context object, e.g.
        //      see https://dzone.com/articles/introducing-unit-testing
        public GroupControllerTest() {

            // This is a dependency of the controller, that we mock, so that
            // we can test the class in isolation
            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart
            _groups = new Mock<ITrustGroupStore>();
            _services = new Mock<ITrustGroupServices>();

            // By convention we call "target" the class under test
            _target = new TrustGroupsController(_groups.Object, _services.Object);
        }

        [Fact, Trait(Constants.Type, Constants.ControllerTest)]
        public void ItFetchesACertificateGroupConfigurationFromTheServiceLayer() {
            var id = "Default";
            var configuration = new TrustGroupRegistrationModel {
                Id = id
            };

            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart

            // Arrange
            _groups.Setup(x => x.GetGroupAsync(id, CancellationToken.None)).ReturnsAsync(configuration);

            // Act
            var result = _target.GetGroupAsync(id).Result;

            // Verify
            _groups.Verify(x => x.GetGroupAsync(
                It.Is<string>(s => s == id), CancellationToken.None), Times.Once);
        }

        private readonly Mock<ITrustGroupStore> _groups;
        private readonly Mock<ITrustGroupServices> _services;
        private readonly TrustGroupsController _target;

        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
    }
}
