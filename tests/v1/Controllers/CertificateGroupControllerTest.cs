// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.v1.Controllers
{
    public class CertificateGroupControllerTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper _log;

        private readonly Mock<ICertificateGroup> _group;
        private readonly CertificateGroupController _target;

        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        // Execute this code before every test
        // Note: for complex setups, where many dependencies need to be
        // prepared before a test, and this method grows too big:
        // 1. First try to reduce the complexity of the class under test
        // 2. If #1 is not possible, use a context object, e.g.
        //      see https://dzone.com/articles/introducing-unit-testing
        public CertificateGroupControllerTest(ITestOutputHelper log)
        {
            // This is a logger, in case we want to output some text during the test
            _log = log;

            // This is a dependency of the controller, that we mock, so that
            // we can test the class in isolation
            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart
            _group = new Mock<ICertificateGroup>();

            // By convention we call "target" the class under test
            _target = new CertificateGroupController(_group.Object);
        }

        [Fact, Trait(Constants.Type, Constants.ControllerTest)]
        public void ItFetchesACertificateGroupConfigurationFromTheServiceLayer()
        {
            // Test syntax: "AAA" see https://msdn.microsoft.com/en-us/library/hh694602.aspx
            // *Arrange*: setup the current context, in preparation for the test
            // *Act*:     execute an action in the system under test (SUT)
            // *Assert*:  verify that the test succeeded
            var id = "Default";
            var configuration = new CertificateGroupConfigurationModel()
            {
                Id = id
            };

            // Inject a fake response when Devices.GetAsync() is invoked
            // Moq Quickstart: https://github.com/Moq/moq4/wiki/Quickstart
            _group.Setup(x => x.GetCertificateGroupConfiguration(id)).ReturnsAsync(configuration);

            // Act
            // Note: don't use "var" so to implicitly assert that the
            // method is returning an object of the expected type. We test only
            // public methods, i.e. to test code inside private methods, we
            // write a test that starts from a public method.
            CertificateGroupConfigurationApiModel result = _target.GetCertificateGroupConfigurationAsync(id).Result;

            // Verify that Devices.GetAsync() has been called, exactly once
            // with the correct parameters
            _group.Verify(x => x.GetCertificateGroupConfiguration(It.Is<string>(s => s == id)), Times.Once);
        }

        [Fact, Trait(Constants.Type, Constants.ControllerTest)]
        public void TestTemplate()
        {
            // Arrange

            // Act

            // Assert
        }
    }
}
