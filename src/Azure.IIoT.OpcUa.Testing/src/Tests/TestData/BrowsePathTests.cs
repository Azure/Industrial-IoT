// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Tests
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class BrowsePathTests<T>
    {
        /// <summary>
        /// Create browse path tests
        /// </summary>
        public BrowsePathTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
            _serializer = new DefaultJsonSerializer();
        }

        public async Task NodeBrowsePathStaticScalarMethod3Test1Async()
        {
            const string nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                "http://test.org/UA/Data/#Static",
                "http://test.org/UA/Data/#MethodTest",
                "http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.BrowsePathAsync(_connection,
                new BrowsePathRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    BrowsePaths = new List<string[]> { pathElements }
                }).ConfigureAwait(false);

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target =>
            {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }

        public async Task NodeBrowsePathStaticScalarMethod3Test2Async()
        {
            const string nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.BrowsePathAsync(_connection,
                new BrowsePathRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    BrowsePaths = new List<string[]> { pathElements }
                }).ConfigureAwait(false);

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target =>
            {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }

        public async Task NodeBrowsePathStaticScalarMethod3Test3Async()
        {
            const string nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements = new[] {
                "<HasComponent>http://test.org/UA/Data/#Static",
                "<HasComponent>http://test.org/UA/Data/#MethodTest",
                "<HasComponent>http://test.org/UA/Data/#ScalarMethod3"
            };

            var browser = _services();

            // Act
            var results = await browser.BrowsePathAsync(_connection,
                new BrowsePathRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    BrowsePaths = new List<string[]> { pathElements }
                }).ConfigureAwait(false);

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target =>
            {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }

        public async Task NodeBrowsePathStaticScalarMethodsTestAsync()
        {
            const string nodeId = "http://test.org/UA/Data/#i=10157"; // Data
            var pathElements3 = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod3"
            };
            var pathElements2 = new[] {
                ".http://test.org/UA/Data/#Static",
                ".http://test.org/UA/Data/#MethodTest",
                ".http://test.org/UA/Data/#ScalarMethod2"
            };

            var browser = _services();

            // Act
            var results = await browser.BrowsePathAsync(_connection,
                new BrowsePathRequestModel
                {
                    Header = new RequestHeaderModel
                    {
                        Diagnostics = new DiagnosticsModel
                        {
                            Level = DiagnosticsLevel.None
                        }
                    },
                    NodeId = nodeId,
                    BrowsePaths = new List<string[]> { pathElements3, pathElements2 }
                }).ConfigureAwait(false);

            // Assert
            Assert.Null(results.ErrorInfo);
            Assert.Collection(results.Targets, target =>
            {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod3", target.Target.BrowseName);
                Assert.Equal("ScalarMethod3", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10762", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            }, target =>
            {
                Assert.Equal("http://test.org/UA/Data/#ScalarMethod2", target.Target.BrowseName);
                Assert.Equal("ScalarMethod2", target.Target.DisplayName);
                Assert.Equal("http://test.org/UA/Data/#i=10759", target.Target.NodeId);
                Assert.Equal(false, target.Target.Children);
                Assert.Equal(-1, target.RemainingPathIndex);
            });
        }

        private readonly T _connection;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly DefaultJsonSerializer _serializer;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly Func<INodeServices<T>> _services;
    }
}
