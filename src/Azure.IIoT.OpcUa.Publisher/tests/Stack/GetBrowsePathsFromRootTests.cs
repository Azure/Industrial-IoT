// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class GetBrowsePathsFromRootTests : IClassFixture<PlcServer>
    {
        public GetBrowsePathsFromRootTests(PlcServer server)
        {
            _server = server;
        }

        private readonly PlcServer _server;

        [Fact]
        public async Task GetBrowsePathsFromRootTest1Async()
        {
            var nodes = new[]
            {
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            };
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    var results = await context.Session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        nodes.Select(n => n.ToNodeId(context.Session.MessageContext)).ToList(),
                        context.Ct);

                    return results.Select(r => r.Path.Elements
                        .Select(e => e.TargetName.AsString(context.Session.MessageContext, NamespaceFormat.Index))
                        .ToList());
                });

            var result = Assert.Single(results);
            Assert.Equal("Objects/2:OpcPlc/2:Telemetry/2:Fast/2:FastUIntScalar1", result.Aggregate((a, b) => $"{a}/{b}"));
        }

        [Fact]
        public async Task GetBrowsePathsFromRootTest2Async()
        {
            var nodes = new[]
            {
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar10"
            };
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    return await context.Session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        nodes.Select(n => n.ToNodeId(context.Session.MessageContext)).ToList(),
                        context.Ct);
                });

            var result = Assert.Single(results);
            Assert.Empty(result.Path.Elements);
            Assert.NotNull(result.ErrorInfo);
            Assert.Equal(StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
        }

        [Fact]
        public async Task GetBrowsePathsFromRootTest3Async()
        {
            var nodes = new[]
            {
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1",
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar2",
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar3"
            };
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    var results = await context.Session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        nodes.Select(n => n.ToNodeId(context.Session.MessageContext)).ToList(),
                        context.Ct);

                    return results.Select(r => r.Path.Elements
                        .Select(e => e.TargetName.AsString(context.Session.MessageContext, NamespaceFormat.Index))
                        .ToList());
                });

            Assert.Collection(results,
                result => Assert.Equal("Objects/2:OpcPlc/2:Telemetry/2:Fast/2:FastUIntScalar1", result.Aggregate((a, b) => $"{a}/{b}")),
                result => Assert.Equal("Objects/2:OpcPlc/2:Telemetry/2:Fast/2:FastUIntScalar2", result.Aggregate((a, b) => $"{a}/{b}")),
                result => Assert.Equal("Objects/2:OpcPlc/2:Telemetry/2:Fast/2:FastUIntScalar3", result.Aggregate((a, b) => $"{a}/{b}"))
            );
        }

        [Fact]
        public async Task GetBrowsePathsFromRootTest4Async()
        {
            var nodes = new[]
            {
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1",
                Plc.Namespaces.PlcApplications + "#s=FastUIntScalar10"
            };
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    return await context.Session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        nodes.Select(n => n.ToNodeId(context.Session.MessageContext)).ToList(),
                        context.Ct);
                });

            Assert.Collection(results,
                result =>
                {
                    Assert.NotEmpty(result.Path.Elements);
                    Assert.Null(result.ErrorInfo);
                    Assert.Equal(5, result.Path.Elements.Count);
                },
                result =>
                {
                    Assert.Empty(result.Path.Elements);
                    Assert.NotNull(result.ErrorInfo);
                    Assert.Equal(StatusCodes.BadNodeIdUnknown, result.ErrorInfo.StatusCode);
                });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(146)]
        [InlineData(13523)]
        [InlineData(50000)]
        public async Task GetBrowsePathsFromRootTest5Async(int count)
        {
            var nodes = Enumerable.Range(0, count).Select(n => Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1").ToList();
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    var results = await context.Session.GetBrowsePathsFromRootAsync(new RequestHeader(),
                        nodes.ConvertAll(n => n.ToNodeId(context.Session.MessageContext)),
                        context.Ct);

                    return results.Select(r => r.Path.Elements
                        .Select(e => e.TargetName.AsString(context.Session.MessageContext, NamespaceFormat.Index))
                        .ToList());
                });

            Assert.All(results, result => Assert.Equal(
                "Objects/2:OpcPlc/2:Telemetry/2:Fast/2:FastUIntScalar1", result.Aggregate((a, b) => $"{a}/{b}")));
        }

        [Fact]
        public async Task GetBrowsePathsFromRootTest7Async()
        {
            var nodes = new[]
            {
                ObjectIds.ObjectsFolder,
                ObjectIds.TypesFolder,
                ObjectIds.EventTypesFolder,
                ObjectTypeIds.BaseConditionClassType,
                DataTypeIds.AliasNameDataType,
                DataTypeIds.XVType,
                VariableIds.AlarmConditionType_ActiveState,
                VariableIds.Server_ServerCapabilities_MaxSelectClauseParameters,
                VariableIds.Server_LocalTime,
                VariableIds.AcknowledgeableConditionType_AckedState,
                VariableIds.Server_ServerStatus_CurrentTime,
                ObjectIds.Server
            };
            var results = await _server.Client.ExecuteAsync(_server.GetConnection(),
                async context =>
                {
                    var results = await context.Session.GetBrowsePathsFromRootAsync(
                        new RequestHeader(), nodes, context.Ct);

                    return results
                        .Select(r => r.Path.Elements
                            .Select(e => e.TargetName.AsString(context.Session.MessageContext, NamespaceFormat.Index))
                            .Aggregate((a, b) => $"{a}/{b}"))
                        .ToList();
                });

            Assert.Collection(results,
                result => Assert.Equal("Objects", result),
                result => Assert.Equal("Types", result),
                result => Assert.Equal("Types/EventTypes", result),
                result => Assert.Equal("Types/ObjectTypes/BaseObjectType/BaseConditionClassType", result),
                result => Assert.Equal("Types/DataTypes/BaseDataType/Structure/AliasNameDataType", result),
                result => Assert.Equal("Types/DataTypes/BaseDataType/Structure/XVType", result),
                result => Assert.Equal("Types/EventTypes/BaseEventType/ConditionType/AcknowledgeableConditionType/AlarmConditionType/ActiveState", result),
                result => Assert.Equal("Objects/Server/ServerCapabilities/MaxSelectClauseParameters", result),
                result => Assert.Equal("Objects/Server/LocalTime", result),
                result => Assert.Equal("Types/EventTypes/BaseEventType/ConditionType/AcknowledgeableConditionType/AckedState", result),
                result => Assert.Equal("Objects/Server/ServerStatus/CurrentTime", result),
                result => Assert.Equal("Objects/Server", result)
            );
        }
    }
}
