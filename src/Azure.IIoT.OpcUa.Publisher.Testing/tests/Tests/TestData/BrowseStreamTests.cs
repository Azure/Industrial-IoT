// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class BrowseStreamTests<T>
    {
        /// <summary>
        /// Create browse tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public BrowseStreamTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
            _serializer = new DefaultJsonSerializer();
        }

        public async Task NodeBrowseInRootTest1Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Contains(results,
                node =>
                {
                    if (node.Attributes?.DisplayName != "Root")
                    {
                        return false;
                    }

                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("i=84", node.SourceId);
                    Assert.Equal("Root", node.Attributes.DisplayName);
                    Assert.Equal("The root of the server address space.", node.Attributes.Description);
                    Assert.Null(node.Attributes.AccessRestrictions);
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Objects")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Objects", reference.Reference.Target.BrowseName);
                    Assert.Equal("Objects", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Types")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Types", reference.Reference.Target.BrowseName);
                    Assert.Equal("Types", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Views")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Views", reference.Reference.Target.BrowseName);
                    Assert.Equal("Views", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
        }

        public async Task NodeBrowseInRootTest2Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NoRecurse = true,
                Direction = BrowseDirection.Forward,
                ReadVariableValues = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Contains(results,
                node =>
                {
                    if (node.Attributes?.DisplayName != "Root")
                    {
                        return false;
                    }

                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("i=84", node.SourceId);
                    Assert.Equal("i=84", node.Attributes.NodeId);
                    Assert.Equal("Root", node.Attributes.DisplayName);
                    Assert.Equal("The root of the server address space.", node.Attributes.Description);
                    Assert.Null(node.Attributes.AccessRestrictions);
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Objects")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Objects", reference.Reference.Target.BrowseName);
                    Assert.Equal("Objects", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=85", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Types")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Types", reference.Reference.Target.BrowseName);
                    Assert.Equal("Types", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=86", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.Target.DisplayName != "Views")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("i=84", reference.SourceId);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("Views", reference.Reference.Target.BrowseName);
                    Assert.Equal("Views", reference.Reference.Target.DisplayName);
                    Assert.Equal("i=87", reference.Reference.Target.NodeId);
                    Assert.True(reference.Reference.Target.Value.IsNull());
                    return true;
                });
        }

        public async Task NodeBrowseBoilersObjectsTest1Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://opcfoundation.org/UA/Boiler/#i=1240" },
                Direction = BrowseDirection.Forward,
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Contains(results,
                node =>
                {
                    if (node.Reference != null)
                    {
                        return false;
                    }

                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", node.SourceId);

                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240",
                        node.Attributes.NodeId);
                    Assert.Equal("Boilers", node.Attributes.DisplayName);
                    Assert.Equal(NodeEventNotifier.SubscribeToEvents, node.Attributes.EventNotifier);
                    Assert.Null(node.Attributes.Description);
                    Assert.Null(node.Attributes.AccessRestrictions);
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.ReferenceTypeId != "i=47")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("i=47", reference.Reference.ReferenceTypeId);

                    Assert.Equal("Boiler #1", reference.Reference.Target.DisplayName);
                    Assert.Null(reference.Reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Reference.Target.NodeId);
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.ReferenceTypeId != "i=48")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("i=48", reference.Reference.ReferenceTypeId);

                    Assert.Equal("Boiler #1", reference.Reference.Target.DisplayName);
                    Assert.Null(reference.Reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1241",
                        reference.Reference.Target.NodeId);
                    return true;
                });
            Assert.Contains(results,
                reference =>
                {
                    if (reference.Reference?.ReferenceTypeId != "i=35")
                    {
                        return false;
                    }

                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler/#i=1240", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);

                    Assert.Equal("Boiler #2", reference.Reference.Target.DisplayName);
                    Assert.Null(reference.Reference.Target.NodeClass);
                    Assert.Equal("http://opcfoundation.org/UA/Boiler//Instance#i=1",
                        reference.Reference.Target.NodeId);
                    return true;
                });
        }

        public async Task NodeBrowseDataAccessObjectsTest1Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "nsu=DataAccess;s=0:TestData/Static" },
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert

            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", node.SourceId);

                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", node.Attributes.NodeId);
                    Assert.Equal("Static", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);

                    Assert.Null(node.Attributes.EventNotifier);
                    Assert.Null(node.Attributes.Description);
                    Assert.Null(node.Attributes.AccessRestrictions);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#TestData", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Backward, reference.Reference.Direction);

                    Assert.Equal("TestData", reference.Reference.Target.DisplayName);
                    Assert.Equal("nsu=DataAccess;s=0:TestData", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC1001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("FC1001", reference.Reference.Target.DisplayName);
                    Assert.Equal("nsu=DataAccess;s=1:FC1001", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC1001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("LC1001", reference.Reference.Target.DisplayName);
                    Assert.Equal("nsu=DataAccess;s=1:LC1001", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC1001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("CC1001", reference.Reference.Target.DisplayName);
                    Assert.Equal("nsu=DataAccess;s=1:CC1001", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#FC2001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("FC2001", reference.Reference.Target.DisplayName);

                    Assert.Equal("nsu=DataAccess;s=1:FC2001", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#LC2001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("LC2001", reference.Reference.Target.DisplayName);

                    Assert.Equal("nsu=DataAccess;s=1:LC2001", reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("nsu=DataAccess;s=0:TestData/Static", reference.SourceId);

                    Assert.Equal("i=35", reference.Reference.ReferenceTypeId);
                    Assert.Equal("DataAccess#CC2001", reference.Reference.Target.BrowseName);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);

                    Assert.Equal("CC2001", reference.Reference.Target.DisplayName);

                    Assert.Equal("nsu=DataAccess;s=1:CC2001", reference.Reference.Target.NodeId);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.Attributes.NodeId);
                    Assert.Equal("Scalar", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10216",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10217",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10218",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10219",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10220",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10221",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10222",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10223",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10224",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10225",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10226",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10227",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10228",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10229",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10230",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10231",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10232",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10233",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10234",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10235",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10236",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10237",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10238",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10239",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10240",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10241",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10242",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10160",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Backward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10158",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Backward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10158",
                        reference.Reference.Target.NodeId);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter1Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NodeClassFilter = new List<NodeClass> {
                        NodeClass.Method,
                        NodeClass.Object
                    },
                Direction = BrowseDirection.Forward,
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.Attributes.NodeId);
                    Assert.Equal("Scalar", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10163",
                        reference.Reference.Target.NodeId);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter2Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NodeClassFilter = new List<NodeClass> {
                        NodeClass.Method
                    },
                Direction = BrowseDirection.Forward,
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.Attributes.NodeId);
                    Assert.Equal("Scalar", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Reference.Target.NodeId);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter3Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NodeClassFilter = new List<NodeClass>
                {
                    NodeClass.Method
                },
                Direction = BrowseDirection.Forward
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.Attributes.NodeId);
                    Assert.Equal("Scalar", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Reference.Target.NodeId);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=9385", node.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=9385", node.Attributes.NodeId);
                    Assert.Equal("GenerateValues", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Method, node.Attributes.NodeClass);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("i=47", node.SourceId);
                    Assert.Equal("i=47", node.Attributes.NodeId);
                    Assert.Equal("HasComponent", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.ReferenceType, node.Attributes.NodeClass);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10161", node.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10161", node.Attributes.NodeId);
                    Assert.Equal("GenerateValues", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Method, node.Attributes.NodeClass);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter4Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NodeClassFilter = new List<NodeClass>
                {
                    NodeClass.Method
                },
                Direction = BrowseDirection.Both
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10159", node.Attributes.NodeId);
                    Assert.Equal("Scalar", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10159", reference.SourceId);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10161",
                        reference.Reference.Target.NodeId);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=9385", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=9385", node.Attributes.NodeId);
                    Assert.Equal("GenerateValues", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Method, node.Attributes.NodeClass);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("i=47", node.SourceId);

                    Assert.Equal("i=47", node.Attributes.NodeId);
                    Assert.Equal("HasComponent", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.ReferenceType, node.Attributes.NodeClass);
                },
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10161", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10161", node.Attributes.NodeId);
                    Assert.Equal("GenerateValues", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Method, node.Attributes.NodeClass);
                });
        }

        public async Task NodeBrowseStaticScalarVariablesTestWithFilter5Async(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10159" },
                NodeClassFilter = new List<NodeClass>
                {
                    NodeClass.Method,
                    NodeClass.Object
                }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.Equal(2481, results.Count);
        }

        public async Task NodeBrowseStaticArrayVariablesTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                NodeIds = new[] { "http://test.org/UA/Data/#i=10243" },
                Direction = BrowseDirection.Forward,
                NoRecurse = true
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            Assert.Collection(results,
                node =>
                {
                    Assert.NotNull(node.Attributes);
                    Assert.Null(node.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", node.SourceId);

                    Assert.Equal("http://test.org/UA/Data/#i=10243", node.Attributes.NodeId);
                    Assert.Equal("Array", node.Attributes.DisplayName);
                    Assert.Equal(NodeClass.Object, node.Attributes.NodeClass);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10300",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10301",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10302",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10303",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10304",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10305",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10306",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10307",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10308",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10309",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10310",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10311",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10312",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10313",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10314",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10315",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10316",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10317",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10318",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10319",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10320",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10321",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10322",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10323",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10324",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10325",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10326",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10244",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10245",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Reference.Target.NodeId);
                },
                reference =>
                {
                    Assert.Null(reference.Attributes);
                    Assert.NotNull(reference.Reference);
                    Assert.Equal(BrowseDirection.Forward, reference.Reference.Direction);
                    Assert.Equal("http://test.org/UA/Data/#i=10243", reference.SourceId);
                    Assert.Equal("http://test.org/UA/Data/#i=10247",
                        reference.Reference.Target.NodeId);
                });
        }

        public async Task NodeBrowseDiagnosticsNoneTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.None
                    }
                },
                NodeIds = new[] { "http://opcfoundation.org/UA/Boiler/#s=unknown" }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            var result = Assert.Single(results);
            Assert.NotNull(result.ErrorInfo);
            Assert.Null(result.ErrorInfo.NamespaceUri);
            Assert.Null(result.ErrorInfo.Locale);
            Assert.Null(result.ErrorInfo.Inner);
            Assert.Null(result.ErrorInfo.AdditionalInfo);
            Assert.Null(result.ErrorInfo.ErrorMessage);
            Assert.NotNull(result.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
        }

        public async Task NodeBrowseDiagnosticsStatusTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        AuditId = nameof(NodeBrowseDiagnosticsStatusTestAsync),
                        TimeStamp = DateTime.Now,
                        Level = DiagnosticsLevel.Status
                    }
                },
                NodeIds = new[] { "http://opcfoundation.org/UA/Boiler/#s=unknown" }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            var result = Assert.Single(results);
            Assert.NotNull(result.ErrorInfo);
            Assert.Null(result.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", result.ErrorInfo.Locale);
            Assert.Equal("BadNodeIdUnknown", result.ErrorInfo.ErrorMessage);
            Assert.Null(result.ErrorInfo.Inner);
            Assert.Null(result.ErrorInfo.AdditionalInfo);
            Assert.NotNull(result.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
        }

        public async Task NodeBrowseDiagnosticsInfoTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Information
                    }
                },
                NodeIds = new[] { "http://opcfoundation.org/UA/Boiler/#s=unknown" }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            var result = Assert.Single(results);
            Assert.NotNull(result.ErrorInfo);
            Assert.Null(result.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", result.ErrorInfo.Locale);
            Assert.Equal("BadNodeIdUnknown", result.ErrorInfo.ErrorMessage);
            Assert.Null(result.ErrorInfo.Inner);
            Assert.Null(result.ErrorInfo.AdditionalInfo);
            Assert.NotNull(result.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
        }

        public async Task NodeBrowseDiagnosticsVerboseTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var results = await browser.BrowseAsync(_connection, new BrowseStreamRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Verbose
                    }
                },
                NodeIds = new[] { "http://opcfoundation.org/UA/Boiler/#s=unknown" }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            // Assert
            var result = Assert.Single(results);

            // Assert
            Assert.NotNull(result.ErrorInfo);
            Assert.Null(result.ErrorInfo.NamespaceUri);
            Assert.Equal("en-US", result.ErrorInfo.Locale);
            Assert.Equal("BadNodeIdUnknown", result.ErrorInfo.ErrorMessage);
            Assert.Null(result.ErrorInfo.Inner);
            Assert.Null(result.ErrorInfo.AdditionalInfo);
            Assert.NotNull(result.ErrorInfo.SymbolicId);
            Assert.Equal(Opc.Ua.StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
        }

        private readonly T _connection;
        private readonly IJsonSerializer _serializer;
        private readonly Func<INodeServices<T>> _services;
    }
}
