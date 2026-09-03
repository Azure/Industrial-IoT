// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Microsoft.Extensions.Options;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MqttServerTests
    {
        [Fact]
        public async Task ConcurrentServersUseDistinctOsSelectedPortsAsync()
        {
            var options = Enumerable.Range(0, 8)
                .Select(_ => Options.Create(new MqttOptions
                {
                    Port = 0,
                    UseTls = false
                }))
                .ToArray();

            var serverTasks = options.Select(option =>
                Task.Run(() => new MqttServer(option))).ToArray();
            try
            {
                var servers = await Task.WhenAll(serverTasks);
                Assert.All(servers, server => Assert.True(server.Port > 0));
                Assert.Equal(servers.Length,
                    servers.Select(server => server.Port).Distinct().Count());
                Assert.All(options, option =>
                    Assert.Contains(option.Value.Port,
                        servers.Select(server => (int?)server.Port)));
            }
            finally
            {
                foreach (var task in serverTasks.Where(
                    task => task.IsCompletedSuccessfully))
                {
                    task.Result.Dispose();
                }
            }
        }
    }
}
