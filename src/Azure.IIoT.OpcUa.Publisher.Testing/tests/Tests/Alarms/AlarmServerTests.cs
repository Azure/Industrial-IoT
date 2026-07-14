// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

extern alias Quickstarts;

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Alarms = Quickstarts::Alarms;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Alarms server node tests
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AlarmServerTests<T>
    {
        public AlarmServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task BrowseAreaPathTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms", target.Target.NodeId);
        }

        public async Task BrowseMetalsSouthMotorTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms",
                        Alarms.Namespaces.Alarms + "#Alarms.Start"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms.Start", target.Target.NodeId);
        }

        public async Task BrowseColoursEastTankTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms",
                        Alarms.Namespaces.Alarms + "#Alarms.StartBranch"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms.StartBranch", target.Target.NodeId);
        }

        public async Task CompileSimpleBaseEventQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from BaseEventType",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    }
                }
            };
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileSimpleTripAlarmQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from OffNormalAlarmType",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/BranchId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/BranchId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Retain" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Retain.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/SupportsFilteredRetain" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SupportsFilteredRetain.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/EnabledState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/EnabledState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/EffectiveDisplayName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveDisplayName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/EffectiveTransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveTransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Quality" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Quality", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/LastSeverity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/LastSeverity", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Comment" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Comment", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/EffectiveDisplayName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveDisplayName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/EffectiveTransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveTransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/InputNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/InputNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/CurrentState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/CurrentState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/UnshelveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/UnshelveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/TimedShelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/TimedShelve", "/InputArguments" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve/InputArguments.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/Unshelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/Unshelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/OneShotShelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/OneShotShelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedOrShelved" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedOrShelved.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/MaxTimeShelved" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/MaxTimeShelved.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/AudibleEnabled" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleEnabled.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/AudibleSound" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleSound.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OnDelay" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OnDelay.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OffDelay" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OffDelay.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/FirstInGroupFlag" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroupFlag.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/FirstInGroup" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroup.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/%3cAlarmGroup%3e" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/<AlarmGroup>.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ReAlarmTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ReAlarmRepeatCount" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmRepeatCount.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = new[] { "/NormalState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/NormalState.Value"
                    }
                }
            };
            expected = CreateOffNormalEventFilter(includeLocalTime: true);
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileAlarmQueryTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = $@"
                    PREFIX alarms <{Alarms.Namespaces.Alarms}>
                    SELECT /Comment, /Severity, /SourceNode FROM OffNormalAlarmType, BaseEventType
                    WHERE
                        OFTYPE OffNormalAlarmType AND
                        /SourceNode IN ('alarms:s=Alarms.BooleanSource'^^NodeId)
                ",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);

            var expected = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Comment" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new() {
                            FilterOperator = FilterOperatorType.And,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    Index = 1
                                },
                                new FilterOperandModel
                                {
                                    Index = 2
                                }
                            }
                        },
                        new() {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new() {
                                    NodeId = "i=10637",
                                    BrowsePath = new[] { "/SourceNode" },
                                    AttributeId = NodeAttribute.Value
                                },
                                new() {
                                    Value = Alarms.Namespaces.Alarms + "#s=Alarms.BooleanSource",
                                    DataType = "NodeId"
                                }
                            }
                        },
                        new() {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new() {
                                    Value = "i=10637",
                                    DataType = "NodeId"
                                }
                            }
                        }
                    }
                }
            };

            expected = CreateOffNormalEventFilter(
            [
                "i=10637|/Comment|/Comment.Value",
                "i=10637|/Severity|/Severity.Value",
                "i=10637|/SourceNode|/SourceNode.Value"
            ], includeWhere: true, sourceNodeTypeId: "i=10637");
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileAlarmQueryTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = $@"
                    PREFIX alarms <{Alarms.Namespaces.Alarms}>
                    SELECT * FROM BaseEventType, OffNormalAlarmType
                    WHERE
                        OFTYPE OffNormalAlarmType AND
                        /SourceNode IN ('alarms:s=Alarms.BooleanSource'^^NodeId)
                ",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/ConditionName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/BranchId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/BranchId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Retain" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Retain.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/SupportsFilteredRetain" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SupportsFilteredRetain.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/EnabledState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/EnabledState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/EffectiveDisplayName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveDisplayName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/EnabledState", "/EffectiveTransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveTransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Quality" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Quality", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/LastSeverity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/LastSeverity", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Comment" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = new[] { "/Comment", "/SourceTimestamp" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment/SourceTimestamp.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/AckedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = new[] { "/ConfirmedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/EffectiveDisplayName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveDisplayName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ActiveState", "/EffectiveTransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveTransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/InputNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/InputNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OutOfServiceState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/CurrentState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/CurrentState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/LastTransition", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/UnshelveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/UnshelveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/TimedShelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/TimedShelve", "/InputArguments" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve/InputArguments.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/Unshelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/Unshelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ShelvingState", "/OneShotShelve" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/OneShotShelve.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SuppressedOrShelved" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedOrShelved.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/MaxTimeShelved" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/MaxTimeShelved.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/AudibleEnabled" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleEnabled.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/AudibleSound" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleSound.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/SilenceState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OnDelay" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OnDelay.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/OffDelay" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OffDelay.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/FirstInGroupFlag" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroupFlag.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/FirstInGroup" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroup.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState", "/Id" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/Id.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/LatchedState", "/TransitionTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TransitionTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/%3cAlarmGroup%3e" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/<AlarmGroup>.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ReAlarmTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = new[] { "/ReAlarmRepeatCount" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmRepeatCount.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = new[] { "/NormalState" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/NormalState.Value"
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new() {
                            FilterOperator = FilterOperatorType.And,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    Index = 1u
                                },
                                new FilterOperandModel
                                {
                                    Index = 2u
                                }
                            }
                        },
                        new() {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    NodeId = "i=10637",
                                    BrowsePath = new[] { "/SourceNode" },
                                    AttributeId = NodeAttribute.Value
                                },
                                new FilterOperandModel
                                {
                                    Value = Alarms.Namespaces.Alarms + "#s=Alarms.BooleanSource",
                                    DataType = "NodeId"
                                }
                            }
                        },
                        new() {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    Value = "i=10637",
                                    DataType = "NodeId"
                                }
                            }
                        }
                    }
                }
            };
            expected = CreateOffNormalEventFilter(includeBaseEventFields: true,
                includeWhere: true, sourceNodeTypeId: "i=2041");
            AssertEventFilter(result.EventFilter, expected);
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;

        private static EventFilterModel CreateOffNormalEventFilter(
            IReadOnlyList<string>? selectClauses = null,
            bool includeBaseEventFields = false,
            bool includeLocalTime = false,
            bool includeWhere = false,
            string? sourceNodeTypeId = null)
        {
            selectClauses ??= kOffNormalSelectClauses;
            if (includeLocalTime)
            {
                selectClauses = selectClauses
                    .Take(6)
                    .Append(kBaseEventSelectClauses[6])
                    .Concat(selectClauses.Skip(6))
                    .ToArray();
            }
            var clauses = includeBaseEventFields
                ? kBaseEventSelectClauses.Concat(selectClauses)
                : selectClauses;
            return new EventFilterModel
            {
                SelectClauses = clauses.Select(CreateSelectClause).ToList(),
                WhereClause = includeWhere ? new ContentFilterModel
                {
                    Elements =
                    [
                        new()
                        {
                            FilterOperator = FilterOperatorType.And,
                            FilterOperands =
                            [
                                new FilterOperandModel { Index = 1u },
                                new FilterOperandModel { Index = 2u }
                            ]
                        },
                        new()
                        {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands =
                            [
                                new FilterOperandModel
                                {
                                    NodeId = sourceNodeTypeId,
                                    BrowsePath = ["/SourceNode"],
                                    AttributeId = NodeAttribute.Value
                                },
                                new FilterOperandModel
                                {
                                    Value = Alarms.Namespaces.Alarms +
                                        "#s=Alarms.BooleanSource",
                                    DataType = "NodeId"
                                }
                            ]
                        },
                        new()
                        {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands =
                            [
                                new FilterOperandModel
                                {
                                    Value = "i=10637",
                                    DataType = "NodeId"
                                }
                            ]
                        }
                    ]
                } : null
            };
        }

        private static SimpleAttributeOperandModel CreateSelectClause(string clause)
        {
            var parts = clause.Split('|');
            return new SimpleAttributeOperandModel
            {
                TypeDefinitionId = parts[0],
                BrowsePath = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries),
                AttributeId = NodeAttribute.Value,
                DisplayName = parts[2]
            };
        }

        private static void AssertEventFilter(EventFilterModel? actual,
            EventFilterModel expected)
        {
            var actualFilter = Assert.IsType<EventFilterModel>(actual);
            Assert.Equal(expected.SelectClauses!.Count, actualFilter.SelectClauses!.Count);
            for (var index = 0; index < expected.SelectClauses.Count; index++)
            {
                var expectedClause = expected.SelectClauses[index];
                var actualClause = actualFilter.SelectClauses[index];
                Assert.True(expectedClause.TypeDefinitionId == actualClause.TypeDefinitionId,
                    $"Select clause {index}: {expectedClause.TypeDefinitionId} != " +
                    $"{actualClause.TypeDefinitionId}");
                Assert.Equal(expectedClause.BrowsePath, actualClause.BrowsePath);
                Assert.Equal(expectedClause.AttributeId, actualClause.AttributeId);
                Assert.Equal(expectedClause.DisplayName, actualClause.DisplayName);
            }

            if (expected.WhereClause is null)
            {
                Assert.Null(actualFilter.WhereClause);
                return;
            }
            var expectedElements = expected.WhereClause.Elements!;
            var actualElements = Assert.IsType<ContentFilterModel>(
                actualFilter.WhereClause).Elements!;
            Assert.Equal(expectedElements.Count, actualElements.Count);
            for (var index = 0; index < expectedElements.Count; index++)
            {
                var expectedElement = expectedElements[index];
                var actualElement = actualElements[index];
                Assert.Equal(expectedElement.FilterOperator, actualElement.FilterOperator);
                Assert.Equal(expectedElement.FilterOperands!.Count,
                    actualElement.FilterOperands!.Count);
                for (var operandIndex = 0; operandIndex < expectedElement.FilterOperands.Count;
                    operandIndex++)
                {
                    var expectedOperand = expectedElement.FilterOperands[operandIndex];
                    var actualOperand = actualElement.FilterOperands[operandIndex];
                    Assert.Equal(expectedOperand.Index, actualOperand.Index);
                    Assert.True(expectedOperand.NodeId == actualOperand.NodeId,
                        $"Where element {index}, operand {operandIndex}: " +
                        $"{expectedOperand.NodeId} != {actualOperand.NodeId}");
                    Assert.Equal(expectedOperand.BrowsePath, actualOperand.BrowsePath);
                    Assert.Equal(expectedOperand.AttributeId, actualOperand.AttributeId);
                    Assert.Equal(expectedOperand.DataType, actualOperand.DataType);
                    Assert.True(JsonNode.DeepEquals(expectedOperand.Value, actualOperand.Value),
                        $"{expectedOperand.Value} != {actualOperand.Value}");
                }
            }
        }

        private static readonly string[] kBaseEventSelectClauses =
        [
            "i=2041|/EventId|/EventId.Value",
            "i=2041|/EventType|/EventType.Value",
            "i=2041|/SourceNode|/SourceNode.Value",
            "i=2041|/SourceName|/SourceName.Value",
            "i=2041|/Time|/Time.Value",
            "i=2041|/ReceiveTime|/ReceiveTime.Value",
            "i=2041|/LocalTime|/LocalTime.Value",
            "i=2041|/Message|/Message.Value",
            "i=2041|/Severity|/Severity.Value",
            "i=2041|/ConditionClassId|/ConditionClassId.Value",
            "i=2041|/ConditionClassName|/ConditionClassName.Value",
            "i=2041|/ConditionSubClassId|/ConditionSubClassId.Value",
            "i=2041|/ConditionSubClassName|/ConditionSubClassName.Value"
        ];

        private static readonly string[] kOffNormalSelectClauses =
        [
            "i=10637|/EventId|/EventId.Value",
            "i=10637|/EventType|/EventType.Value",
            "i=10637|/SourceNode|/SourceNode.Value",
            "i=10637|/SourceName|/SourceName.Value",
            "i=10637|/Time|/Time.Value",
            "i=10637|/ReceiveTime|/ReceiveTime.Value",
            "i=10637|/Message|/Message.Value",
            "i=10637|/Severity|/Severity.Value",
            "i=10637|/ConditionClassId|/ConditionClassId.Value",
            "i=10637|/ConditionClassName|/ConditionClassName.Value",
            "i=2782|/ConditionSubClassId|/ConditionSubClassId.Value",
            "i=2782|/ConditionSubClassName|/ConditionSubClassName.Value",
            "i=10637|/ConditionName|/ConditionName.Value",
            "i=10637|/BranchId|/BranchId.Value",
            "i=10637|/Retain|/Retain.Value",
            "i=2782|/SupportsFilteredRetain|/SupportsFilteredRetain.Value",
            "i=10637|/EnabledState|/EnabledState.Value",
            "i=2782|/EnabledState,/TransitionTime|/EnabledState/TransitionTime.Value",
            "i=2782|/EnabledState,/EffectiveTransitionTime|/EnabledState/EffectiveTransitionTime.Value",
            "i=10637|/EnabledState,/Id|/EnabledState/Id.Value",
            "i=2782|/EnabledState,/EffectiveDisplayName|/EnabledState/EffectiveDisplayName.Value",
            "i=10637|/Quality|/Quality.Value",
            "i=10637|/Quality,/SourceTimestamp|/Quality/SourceTimestamp.Value",
            "i=10637|/LastSeverity|/LastSeverity.Value",
            "i=10637|/LastSeverity,/SourceTimestamp|/LastSeverity/SourceTimestamp.Value",
            "i=10637|/Comment|/Comment.Value",
            "i=10637|/Comment,/SourceTimestamp|/Comment/SourceTimestamp.Value",
            "i=10637|/ClientUserId|/ClientUserId.Value",
            "i=10637|/AckedState|/AckedState.Value",
            "i=2881|/AckedState,/TransitionTime|/AckedState/TransitionTime.Value",
            "i=10637|/AckedState,/Id|/AckedState/Id.Value",
            "i=2881|/ConfirmedState|/ConfirmedState.Value",
            "i=2881|/ConfirmedState,/TransitionTime|/ConfirmedState/TransitionTime.Value",
            "i=2881|/ConfirmedState,/Id|/ConfirmedState/Id.Value",
            "i=10637|/ActiveState|/ActiveState.Value",
            "i=2915|/ActiveState,/TransitionTime|/ActiveState/TransitionTime.Value",
            "i=2915|/ActiveState,/EffectiveTransitionTime|/ActiveState/EffectiveTransitionTime.Value",
            "i=10637|/ActiveState,/Id|/ActiveState/Id.Value",
            "i=2915|/ActiveState,/EffectiveDisplayName|/ActiveState/EffectiveDisplayName.Value",
            "i=10637|/InputNode|/InputNode.Value",
            "i=2915|/SuppressedState|/SuppressedState.Value",
            "i=2915|/SuppressedState,/TransitionTime|/SuppressedState/TransitionTime.Value",
            "i=2915|/SuppressedState,/Id|/SuppressedState/Id.Value",
            "i=2915|/OutOfServiceState|/OutOfServiceState.Value",
            "i=2915|/OutOfServiceState,/TransitionTime|/OutOfServiceState/TransitionTime.Value",
            "i=2915|/OutOfServiceState,/Id|/OutOfServiceState/Id.Value",
            "i=2915|/ShelvingState|/ShelvingState.Value",
            "i=2915|/ShelvingState,/UnshelveTime|/ShelvingState/UnshelveTime.Value",
            "i=2915|/ShelvingState,/TimedShelve|/ShelvingState/TimedShelve.Value",
            "i=2915|/ShelvingState,/TimedShelve,/InputArguments|/ShelvingState/TimedShelve/InputArguments.Value",
            "i=2915|/ShelvingState,/Unshelve|/ShelvingState/Unshelve.Value",
            "i=2915|/ShelvingState,/OneShotShelve|/ShelvingState/OneShotShelve.Value",
            "i=2915|/ShelvingState,/CurrentState|/ShelvingState/CurrentState.Value",
            "i=2915|/ShelvingState,/CurrentState,/Id|/ShelvingState/CurrentState/Id.Value",
            "i=2915|/ShelvingState,/LastTransition|/ShelvingState/LastTransition.Value",
            "i=2915|/ShelvingState,/LastTransition,/Id|/ShelvingState/LastTransition/Id.Value",
            "i=2915|/ShelvingState,/LastTransition,/TransitionTime|/ShelvingState/LastTransition/TransitionTime.Value",
            "i=10637|/SuppressedOrShelved|/SuppressedOrShelved.Value",
            "i=2915|/MaxTimeShelved|/MaxTimeShelved.Value",
            "i=2915|/AudibleEnabled|/AudibleEnabled.Value",
            "i=2915|/AudibleSound|/AudibleSound.Value",
            "i=2915|/SilenceState|/SilenceState.Value",
            "i=2915|/SilenceState,/TransitionTime|/SilenceState/TransitionTime.Value",
            "i=2915|/SilenceState,/Id|/SilenceState/Id.Value",
            "i=2915|/OnDelay|/OnDelay.Value",
            "i=2915|/OffDelay|/OffDelay.Value",
            "i=2915|/FirstInGroupFlag|/FirstInGroupFlag.Value",
            "i=2915|/FirstInGroup|/FirstInGroup.Value",
            "i=2915|/LatchedState|/LatchedState.Value",
            "i=2915|/LatchedState,/TransitionTime|/LatchedState/TransitionTime.Value",
            "i=2915|/LatchedState,/Id|/LatchedState/Id.Value",
            "i=2915|/%3CAlarmGroup%3E|/<AlarmGroup>.Value",
            "i=2915|/ReAlarmTime|/ReAlarmTime.Value",
            "i=2915|/ReAlarmRepeatCount|/ReAlarmRepeatCount.Value",
            "i=10637|/NormalState|/NormalState.Value"
        ];
    }
}
