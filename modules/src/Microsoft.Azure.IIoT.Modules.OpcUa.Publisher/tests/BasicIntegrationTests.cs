// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using System;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Currently, we create new independent instances of server, publisher and mocked IoT services for each test,
    /// this could be optimised e.g. create only single instance of server and publisher between tests in the same class.
    /// </summary>
    public class BasicIntegrationTests : PublisherIntegrationTestBase, IClassFixture<OPCUAServerFixture> {
        [Theory]
        [InlineData(@"./PublishedNodes/DataItems.json")]
        public async Task CanSendDataItemToIoTHubTest(string publishedNodesFile) {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(publishedNodesFile);

            // Assert
            Assert.Single(messages);
            Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1259", messages[0].RootElement[0].GetProperty("NodeId").GetString());
            Assert.InRange(messages[0].RootElement[0].GetProperty("Value").GetProperty("Value").GetDouble(), double.MinValue, double.MaxValue);
        }

        [Theory]
        [InlineData(@"./PublishedNodes/SimpleEvents.json")]
        public async Task CanSendEventToIoTHubTest(string publishedNodesFile) {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(publishedNodesFile);

            // Assert
            Assert.Single(messages);
            Assert.Equal("i=2253", messages[0].RootElement[0].GetProperty("NodeId").GetString());
            Assert.NotEmpty(messages[0].RootElement[0].GetProperty("Value").GetProperty("Value").GetProperty("EventId").GetString());
        }

        [Theory]
        [InlineData(@"./PublishedNodes/PendingAlarms.json")]
        public async Task CanSendPendingAlarmsToIoTHubTest(string publishedNodesFile) {
            // Arrange
            // Act
            var messages = await ProcessMessagesAsync(publishedNodesFile, new TimeSpan(0, 0, 1), new TimeSpan(0, 2, 0), 1);

            // Assert
            Assert.Single(messages);            
            Assert.Equal("i=2253", messages[0].RootElement[0].GetProperty("NodeId").GetString());
            Assert.Equal("PendingAlarms", messages[0].RootElement[0].GetProperty("DisplayName").GetString());
            Assert.Equal("http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank", messages[0].RootElement[0].GetProperty("Value")[1].GetProperty("SourceNode").GetString());
            Assert.Equal("100", messages[0].RootElement[0].GetProperty("Value")[1].GetProperty("Value").GetProperty("Severity").ToString());          
        }
    }
}