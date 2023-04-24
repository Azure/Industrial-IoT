// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Alarms;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using System.Threading;

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
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Namespaces.AlarmCondition + "#Green",
                        Namespaces.AlarmCondition + "#East",
                        Namespaces.AlarmCondition + "#Blue"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal("http://opcfoundation.org/AlarmCondition#s=0%3aEast%2fBlue", target.Target.NodeId);
        }

        public async Task BrowseMetalsSouthMotorTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Namespaces.AlarmCondition + "#Green",
                        Namespaces.AlarmCondition + "#East",
                        Namespaces.AlarmCondition + "#Blue",
                        Namespaces.AlarmCondition + "#SouthMotor"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal("http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fSouthMotor", target.Target.NodeId);
        }

        public async Task BrowseColoursEastTankTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Namespaces.AlarmCondition + "#Yellow",
                        Namespaces.AlarmCondition + "#West",
                        Namespaces.AlarmCondition + "#Blue",
                        Namespaces.AlarmCondition + "#EastTank"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal("http://opcfoundation.org/AlarmCondition#s=1%3aColours%2fEastTank", target.Target.NodeId);
        }

#if UNUSED
        public async Task CompileSimpleBaseEventQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();


            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from BaseEventType"
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventType",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Time",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/ReceiveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/LocalTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Message",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    }
                }
            });
        }

        public async Task CompileSimpleTripAlarmQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();


            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from TripAlarmType"
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventType",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Time",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/ReceiveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/LocalTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Message",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionClassId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionClassName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionSubClassId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionSubClassName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/BranchId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/BranchId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Retain",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Retain.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/EnabledState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/EnabledState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/EffectiveDisplayName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveDisplayName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/EffectiveTransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveTransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Quality",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Quality/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/LastSeverity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/LastSeverity/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Comment",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Comment/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ClientUserId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ClientUserId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Disable",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Disable.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Enable",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Enable.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/AddComment",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AddComment.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/AddComment/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AddComment/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Acknowledge",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Acknowledge.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Acknowledge/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Acknowledge/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Confirm",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Confirm.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Confirm/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Confirm/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/EffectiveDisplayName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveDisplayName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/EffectiveTransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveTransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/InputNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/InputNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/CurrentState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/CurrentState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/UnshelveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/UnshelveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/TimedShelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/TimedShelve/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/Unshelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/Unshelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/OneShotShelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/OneShotShelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedOrShelved",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedOrShelved.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/MaxTimeShelved",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/MaxTimeShelved.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/AudibleEnabled",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleEnabled.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/AudibleSound",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleSound.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OnDelay",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OnDelay.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OffDelay",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OffDelay.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/FirstInGroupFlag",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroupFlag.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/FirstInGroup",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroup.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/%3cAlarmGroup%3e",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/<AlarmGroup>.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ReAlarmTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ReAlarmRepeatCount",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmRepeatCount.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Silence",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Silence.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Suppress",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Suppress.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Unsuppress",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Unsuppress.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/RemoveFromService",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/RemoveFromService.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/PlaceInService",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/PlaceInService.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Reset",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Reset.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = "/NormalState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/NormalState.Value"
                    }
                }
            });
        }

        public async Task CompileAlarmQueryTest1Async(CancellationToken ct = default)
        {
            var services = _services();


            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = @"
                    PREFIX ac <http://opcfoundation.org/AlarmCondition>
                    SELECT /Comment, /Severity, /SourceNode FROM TripAlarmType, BaseEventType
                    WHERE
                        OFTYPE TripAlarmType AND
                        /SourceNode IN ('ac:s=1%3aMetals%2fSouthMotor'^^NodeId)
                "
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);

            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Comment",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new ContentFilterElementModel
                        {
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
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new FilterOperandModel
                                {
                                    NodeId = "i=2041",
                                    BrowsePath = "/SourceNode",
                                    AttributeId = NodeAttribute.Value
                                },
                                new FilterOperandModel
                                {
                                    Value = "http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fSouthMotor",
                                    DataType = "NodeId"
                                }
                            }
                        },
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new FilterOperandModel
                                {
                                    Value = "i=10751",
                                    DataType = "NodeId"
                                }
                            }
                        }
                    }
                }
            });
        }

        public async Task CompileAlarmQueryTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = @"
                    PREFIX ac <http://opcfoundation.org/AlarmCondition>
                    SELECT * FROM BaseEventType, TripAlarmType
                    WHERE
                        OFTYPE TripAlarmType AND
                        /SourceNode IN ('ac:s=1%3aMetals%2fSouthMotor'^^NodeId)
                "
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/EventType",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/SourceName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Time",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/ReceiveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/LocalTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Message",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionClassId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionClassName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionSubClassId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionSubClassName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ConditionName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/BranchId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/BranchId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Retain",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Retain.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/EnabledState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/EnabledState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/EffectiveDisplayName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveDisplayName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/EffectiveTransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/EffectiveTransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/EnabledState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EnabledState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Quality",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Quality/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Quality/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/LastSeverity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/LastSeverity/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LastSeverity/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Comment",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Comment/SourceTimestamp",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Comment/SourceTimestamp.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/ClientUserId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ClientUserId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Disable",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Disable.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/Enable",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Enable.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/AddComment",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AddComment.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2782",
                        BrowsePath = "/AddComment/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AddComment/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/AckedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AckedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/ConfirmedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConfirmedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Acknowledge",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Acknowledge.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Acknowledge/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Acknowledge/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Confirm",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Confirm.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2881",
                        BrowsePath = "/Confirm/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Confirm/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/EffectiveDisplayName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveDisplayName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/EffectiveTransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/EffectiveTransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ActiveState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ActiveState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/InputNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/InputNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OutOfServiceState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OutOfServiceState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/CurrentState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/CurrentState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/CurrentState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/LastTransition/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/LastTransition/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/UnshelveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/UnshelveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/TimedShelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/TimedShelve/InputArguments",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/TimedShelve/InputArguments.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/Unshelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/Unshelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ShelvingState/OneShotShelve",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ShelvingState/OneShotShelve.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SuppressedOrShelved",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SuppressedOrShelved.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/MaxTimeShelved",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/MaxTimeShelved.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/AudibleEnabled",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleEnabled.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/AudibleSound",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/AudibleSound.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/SilenceState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SilenceState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OnDelay",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OnDelay.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/OffDelay",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/OffDelay.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/FirstInGroupFlag",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroupFlag.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/FirstInGroup",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/FirstInGroup.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/Id",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/Id.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/TransitionTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TransitionTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/TrueState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/TrueState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/LatchedState/FalseState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LatchedState/FalseState.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/%3cAlarmGroup%3e",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/<AlarmGroup>.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ReAlarmTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/ReAlarmRepeatCount",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReAlarmRepeatCount.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Silence",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Silence.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Suppress",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Suppress.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Unsuppress",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Unsuppress.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/RemoveFromService",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/RemoveFromService.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/PlaceInService",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/PlaceInService.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2915",
                        BrowsePath = "/Reset",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Reset.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=10637",
                        BrowsePath = "/NormalState",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/NormalState.Value"
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new ContentFilterElementModel
                        {
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
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    TypeDefinitionId = "i=2041",
                                    BrowsePath = "/SourceNode",
                                    AttributeId = NodeAttribute.Value
                                },
                                new FilterOperandModel
                                {
                                    Value = "http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fSouthMotor",
                                    DataType = "NodeId"
                                }
                            }
                        },
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new []
                            {
                                new FilterOperandModel
                                {
                                    Value = "i=10751",
                                    DataType = "NodeId"
                                }
                            }
                        }
                    }
                }
            });
        }
#endif
        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
