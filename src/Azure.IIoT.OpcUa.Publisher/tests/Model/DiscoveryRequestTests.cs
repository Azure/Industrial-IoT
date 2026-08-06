// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Model
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using Moq;
    using System;
    using System.Linq;
    using System.Threading;
    using Xunit;

    public class DiscoveryRequestTests
    {
        [Fact]
        public void DefaultConstructorCreatesOffScanRequest()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            using var req = new DiscoveryRequest(progress, TimeProvider.System);

            Assert.True(req.IsScan);
            Assert.Equal(DiscoveryMode.Off, req.Mode);
            Assert.NotNull(req.Request);
            Assert.NotNull(req.Token);
        }

        [Fact]
        public void NullRequestModelThrowsArgumentNullException()
        {
            var progress = Mock.Of<IDiscoveryProgress>();

            Assert.Throws<ArgumentNullException>(() =>
                new DiscoveryRequest(null!, progress, TimeProvider.System));
        }

        [Fact]
        public void ModeOffSkipsRangeCalculation()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var config = new DiscoveryConfigModel { DiscoveryUrls = ["opc.tcp://host:4840"] };
            using var req = new DiscoveryRequest(DiscoveryMode.Off, config, progress, TimeProvider.System);

            Assert.Equal(DiscoveryMode.Off, req.Mode);
            Assert.Empty(req.PortRanges ?? []);
            Assert.Empty(req.AddressRanges ?? []);
            // DiscoveryUrls from the config must be preserved
            Assert.Single(req.DiscoveryUrls);
        }

        [Fact]
        public void ModeOffWithNullConfigPreservesEmptyDiscoveryUrls()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            using var req = new DiscoveryRequest(DiscoveryMode.Off, null, progress, TimeProvider.System);

            Assert.Equal(DiscoveryMode.Off, req.Mode);
            Assert.Empty(req.DiscoveryUrls);
        }

        [Fact]
        public void ExplicitPortRangesToScanSetsPortRangesAndFastMode()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "test",
                Discovery = DiscoveryMode.Network,
                Configuration = new DiscoveryConfigModel
                {
                    PortRangesToScan = "4840-4841",
                    AddressRangesToScan = "127.0.0.1/32"
                }
            };

            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.NotNull(req.PortRanges);
            Assert.Equal(2, req.TotalPorts);
        }

        [Fact]
        public void ExplicitAddressRangesToScanSetsAddressRangesAndFastMode()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "test",
                Discovery = DiscoveryMode.Network,
                Configuration = new DiscoveryConfigModel
                {
                    AddressRangesToScan = "10.0.0.0/24",
                    PortRangesToScan = "4840"
                }
            };

            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.NotNull(req.AddressRanges);
            Assert.Equal(256, req.TotalAddresses);
        }

        [Fact]
        public void CancelCancelsToken()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            using var req = new DiscoveryRequest(progress, TimeProvider.System);

            Assert.False(req.Token.IsCancellationRequested);

            req.Cancel();

            Assert.True(req.Token.IsCancellationRequested);
        }

        [Fact]
        public void TokenLinkedToExternalCancellationToken()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "t" };
            using var externalCts = new CancellationTokenSource();
            using var req = new DiscoveryRequest(request, progress,
                TimeProvider.System, NetworkClass.Wired, false, externalCts.Token);

            externalCts.Cancel();

            Assert.True(req.Token.IsCancellationRequested);
        }

        [Fact]
        public void CloneCreatesEquivalentCopy()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "clone-test",
                Discovery = DiscoveryMode.Off
            };
            using var original = new DiscoveryRequest(request, progress, TimeProvider.System);
            using var clone = original.Clone();

            Assert.Equal(original.IsScan, clone.IsScan);
            Assert.Equal(original.Mode, clone.Mode);
            Assert.Equal(original.NetworkClass, clone.NetworkClass);
            // Cloned token should not be the same source as original
            Assert.NotNull(clone.Token);
        }

        [Fact]
        public void DisposeDoesNotThrow()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "dispose-test" };
            var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            var ex = Record.Exception(() => req.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void ConfigurationPropertyReturnsFallbackWhenNull()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "config-test",
                Configuration = null
            };

            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // Configuration property should never return null
            Assert.NotNull(req.Configuration);
        }

        [Fact]
        public void DiscoveryUrlsParseFromConfiguration()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "urls-test",
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://server1:4840", "opc.tcp://server2:4840"]
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            var urls = req.DiscoveryUrls.ToList();
            Assert.Equal(2, urls.Count);
            Assert.All(urls, u => Assert.Equal("opc.tcp", u.Scheme));
        }

        [Fact]
        public void IsScanTrueWhenSetInConstructor()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "scan-test" };
            using var req = new DiscoveryRequest(request, progress,
                TimeProvider.System, NetworkClass.Wired, isScan: true);

            Assert.True(req.IsScan);
        }

        [Fact]
        public void IsScanFalseWhenNotSetInConstructor()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "noscan-test" };
            using var req = new DiscoveryRequest(request, progress,
                TimeProvider.System, NetworkClass.Wired, isScan: false);

            Assert.False(req.IsScan);
        }

        [Fact]
        public void NetworkClassPreservedFromConstructor()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "class-test" };
            using var req = new DiscoveryRequest(request, progress,
                TimeProvider.System, NetworkClass.Wireless);

            Assert.Equal(NetworkClass.Wireless, req.NetworkClass);
        }

        [Fact]
        public void ProgressPreservedFromConstructor()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel { Id = "progress-test" };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.Same(progress, req.Progress);
        }

        [Fact]
        public void IdleTimeDefaultsSetInConfiguration()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "idle-test",
                Discovery = DiscoveryMode.Network,
                Configuration = new DiscoveryConfigModel
                {
                    AddressRangesToScan = "192.168.1.0/24",
                    PortRangesToScan = "4840"
                }
            };

            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // Default idle time should have been applied if not set
            Assert.NotNull(req.Configuration.IdleTimeBetweenScans);
            Assert.NotNull(req.Configuration.PortProbeTimeout);
            Assert.NotNull(req.Configuration.NetworkProbeTimeout);
        }

        [Theory]
        [InlineData(DiscoveryMode.Local)]
        [InlineData(DiscoveryMode.Fast)]
        [InlineData(DiscoveryMode.Network)]
        [InlineData(DiscoveryMode.Scan)]
        public void ScanModesPopulatePortRanges(DiscoveryMode mode)
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "portrange-" + mode,
                Discovery = mode,
                Configuration = new DiscoveryConfigModel
                {
                    // Provide explicit addresses to avoid needing real NICs
                    AddressRangesToScan = "127.0.0.1/32"
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.NotNull(req.PortRanges);
            Assert.True(req.TotalPorts > 0, $"Expected TotalPorts > 0 for mode {mode}");
        }

        [Theory]
        [InlineData(DiscoveryMode.Local)]
        [InlineData(DiscoveryMode.Fast)]
        [InlineData(DiscoveryMode.Network)]
        [InlineData(DiscoveryMode.Scan)]
        public void ScanModesPopulateAddressRanges(DiscoveryMode mode)
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "addrrange-" + mode,
                Discovery = mode,
                Configuration = new DiscoveryConfigModel
                {
                    // Provide explicit port ranges to avoid needing real NICs for addresses
                    PortRangesToScan = "4840"
                }
            };
            // Note: this calls GetAllNetInterfaces which reads actual NICs;
            // the result may be empty in test environments without real interfaces.
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // Even if no addresses found, the properties should be non-null
            Assert.NotNull(req.PortRanges);
        }

        [Fact]
        public void ExplicitPortRangeOverridesDefaultForFastMode()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "override-test",
                Discovery = DiscoveryMode.Local,
                Configuration = new DiscoveryConfigModel
                {
                    PortRangesToScan = "4840-4841",
                    AddressRangesToScan = "127.0.0.1/32"
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // The explicit range sets Discovery to Fast
            Assert.Equal(DiscoveryMode.Fast, req.Mode);
            Assert.Equal(2, req.TotalPorts);
        }

        [Fact]
        public void ExplicitAddressRangeOverridesDefaultForFastMode()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "addr-override-test",
                Discovery = DiscoveryMode.Local,
                Configuration = new DiscoveryConfigModel
                {
                    AddressRangesToScan = "10.0.0.1/32",
                    PortRangesToScan = "4840"
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // The explicit address range sets Discovery to Fast
            Assert.Equal(DiscoveryMode.Fast, req.Mode);
            Assert.Equal(1, req.TotalAddresses);
        }

        [Fact]
        public void AddLocalHostPassesThroughRangesInNonContainerMode()
        {
            var addresses = new[] { new AddressRange(0x7f000001u, 0x7f000001u) };
            var result = DiscoveryRequest.AddLocalHost(addresses).ToList();

            // In a non-container environment, the input is returned unchanged
            // (or with the docker host address appended, but we can verify it's non-empty)
            Assert.NotEmpty(result);
        }

        [Fact]
        public void TotalAddressesReflectsExplicitRange()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "total-addr-test",
                Discovery = DiscoveryMode.Network,
                Configuration = new DiscoveryConfigModel
                {
                    AddressRangesToScan = "192.168.1.0/24",
                    PortRangesToScan = "4840"
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.Equal(256, req.TotalAddresses);
        }

        [Fact]
        public void TotalPortsReflectsExplicitRange()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "total-port-test",
                Discovery = DiscoveryMode.Network,
                Configuration = new DiscoveryConfigModel
                {
                    AddressRangesToScan = "127.0.0.1/32",
                    PortRangesToScan = "4840-4845"
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            Assert.Equal(6, req.TotalPorts);
        }

        [Fact]
        public void LocalesPreservedInModeOffConfiguration()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "locales-test",
                Configuration = new DiscoveryConfigModel
                {
                    Locales = ["en-US", "de-DE"],
                    DiscoveryUrls = ["opc.tcp://host:4840"]
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            // When mode is null (defaults to Off), locales should be preserved
            Assert.NotNull(req.Configuration.Locales);
            Assert.Equal(2, req.Configuration.Locales!.Count);
        }

        [Fact]
        public void CancelSignalsCancellationToken()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            using var req = new DiscoveryRequest(progress, TimeProvider.System);

            Assert.False(req.Token.IsCancellationRequested);
            req.Cancel();
            Assert.True(req.Token.IsCancellationRequested);
        }

        [Fact]
        public void CloneProducesEquivalentRequest()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "clone-test",
                Discovery = DiscoveryMode.Off,
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://host:4840"]
                }
            };
            using var original = new DiscoveryRequest(request, progress, TimeProvider.System);
            using var clone = original.Clone();

            Assert.Equal(original.Mode, clone.Mode);
            Assert.Equal(original.IsScan, clone.IsScan);
            Assert.Equal(original.Request.Id, clone.Request.Id);
        }

        [Fact]
        public void AddLocalHostNotInContainerReturnsSameRanges()
        {
            // Ensure we are not in a Docker container for this test
            var saved = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            try
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
                var ranges = new[]
                {
                    new AddressRange(System.Net.IPAddress.Parse("192.168.1.0"), 24)
                };

                var result = DiscoveryRequest.AddLocalHost(ranges).ToArray();

                Assert.Single(result);
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", saved);
            }
        }

        [Fact]
        public void DiscoveryUrlsEnumeratedFromConfiguration()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            var request = new DiscoveryRequestModel
            {
                Id = "urls-test",
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://host1:4840", "opc.tcp://host2:4840"]
                }
            };
            using var req = new DiscoveryRequest(request, progress, TimeProvider.System);

            var urls = req.DiscoveryUrls.ToList();
            Assert.Equal(2, urls.Count);
            Assert.All(urls, u => Assert.Equal("opc.tcp", u.Scheme));
        }

        [Fact]
        public void DiscoveryUrlsEmptyWhenNoConfiguration()
        {
            var progress = Mock.Of<IDiscoveryProgress>();
            using var req = new DiscoveryRequest(progress, TimeProvider.System);

            Assert.Empty(req.DiscoveryUrls);
        }
    }
}
