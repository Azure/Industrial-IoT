/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Isa95Jobs
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UAModel.ISA95_JOBCONTROL_V2;

    /// <summary>
    /// Hosts the ISA-95 Job Control NodeSet2 model.
    /// </summary>
    public sealed partial class Isa95JobControlNodeManager : AsyncCustomNodeManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Isa95JobControlNodeManager"/> class.
        /// </summary>
        /// <param name="server">
        /// The server that owns the node manager.
        /// </param>
        /// <param name="configuration">
        /// The server configuration.
        /// </param>
        public Isa95JobControlNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration)
            : base(server, configuration, kModelUri)
        {
        }

        /// <inheritdoc/>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        /// <inheritdoc/>
        public override async ValueTask CreateAddressSpaceAsync(
            IDictionary<NodeId, IList<IReference>> externalReferences,
            CancellationToken cancellationToken = default)
        {
            await base.CreateAddressSpaceAsync(externalReferences, cancellationToken)
                .ConfigureAwait(false);

            _simulationCancellation = new CancellationTokenSource();
            _ = PublishEventsAsync(_simulationCancellation.Token);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _simulationCancellation?.Cancel();
                _simulationCancellation?.Dispose();
                _simulationCancellation = null;
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        protected override ValueTask<NodeStateCollection> LoadPredefinedNodesAsync(
            ISystemContext context,
            CancellationToken cancellationToken = default)
        {
            var nodes = new NodeStateCollection();
            return new ValueTask<NodeStateCollection>(
                nodes.AddUAModelISA95_JOBCONTROL_V2(context));
        }

        private async Task PublishEventsAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(kEventInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    var jobId = Interlocked.Increment(ref _jobId);
                    var eventState = new ISA95JobOrderStatusEventState(null);
                    eventState.Initialize(
                        SystemContext,
                        null,
                        EventSeverity.Low,
                        new LocalizedText("en-US", $"The job '{jobId}' has completed."));
                    eventState.SetChildValue(
                        SystemContext,
                        Opc.Ua.BrowseNames.SourceName,
                        "ISA95 Job Control",
                        false);
                    eventState.SetChildValue(
                        SystemContext,
                        Opc.Ua.BrowseNames.SourceNode,
                        Opc.Ua.ObjectIds.Server,
                        false);

                    var now = DateTimeUtc.Now;
                    var response = new ISA95JobResponseDataType
                    {
                        EncodingMask = (uint)(
                            ISA95JobResponseDataTypeFields.StartTime |
                            ISA95JobResponseDataTypeFields.EndTime |
                            ISA95JobResponseDataTypeFields.EquipmentActuals |
                            ISA95JobResponseDataTypeFields.MaterialActuals),
                        JobOrderID = jobId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        JobResponseID = Guid.NewGuid().ToString(),
                        StartTime = now.AddMilliseconds(-TimeSpan.FromMinutes(3).TotalMilliseconds),
                        EndTime = now,
                        EquipmentActuals =
                        [
                            new ISA95EquipmentDataType
                            {
                                EncodingMask = (uint)(
                                    ISA95EquipmentDataTypeFields.EquipmentUse |
                                    ISA95EquipmentDataTypeFields.EngineeringUnits |
                                    ISA95EquipmentDataTypeFields.Quantity),
                                EquipmentUse = "consumable",
                                EngineeringUnits = new EUInformation("rpm", "RPM"),
                                Quantity = "500"
                            },
                            new ISA95EquipmentDataType
                            {
                                EncodingMask = (uint)(
                                    ISA95EquipmentDataTypeFields.EquipmentUse |
                                    ISA95EquipmentDataTypeFields.EngineeringUnits |
                                    ISA95EquipmentDataTypeFields.Quantity),
                                EquipmentUse = "consumable",
                                EngineeringUnits = new EUInformation("C", "Celsius"),
                                Quantity = "3"
                            }
                        ],
                        MaterialActuals =
                        [
                            new ISA95MaterialDataType
                            {
                                EncodingMask = (uint)(
                                    ISA95MaterialDataTypeFields.MaterialClassID |
                                    ISA95MaterialDataTypeFields.MaterialUse |
                                    ISA95MaterialDataTypeFields.Quantity),
                                MaterialClassID = Guid.NewGuid().ToString(),
                                MaterialUse = "consumable",
                                Quantity = "1"
                            },
                            new ISA95MaterialDataType
                            {
                                EncodingMask = (uint)(
                                    ISA95MaterialDataTypeFields.MaterialClassID |
                                    ISA95MaterialDataTypeFields.MaterialUse |
                                    ISA95MaterialDataTypeFields.Quantity),
                                MaterialClassID = Guid.NewGuid().ToString(),
                                MaterialUse = "consumable",
                                Quantity = "2"
                            }
                        ]
                    };
                    eventState.SetChildValue(
                        SystemContext,
                        new QualifiedName(
                            UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobResponse,
                            NamespaceIndex),
                        response,
                        false);

                    var jobOrder = new ISA95JobOrderDataType
                    {
                        JobOrderID = jobId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        EquipmentRequirements =
                        [
                            new ISA95EquipmentDataType
                            {
                                EncodingMask = (uint)(
                                    ISA95EquipmentDataTypeFields.EquipmentUse |
                                    ISA95EquipmentDataTypeFields.EngineeringUnits |
                                    ISA95EquipmentDataTypeFields.Quantity),
                                EquipmentUse = "free",
                                EngineeringUnits = new EUInformation("rpm", "RPM"),
                                Quantity = "1000"
                            }
                        ]
                    };
                    eventState.SetChildValue(
                        SystemContext,
                        new QualifiedName(
                            UAModel.ISA95_JOBCONTROL_V2.BrowseNames.JobOrder,
                            NamespaceIndex),
                        jobOrder,
                        false);
                    Server.ReportEvent(eventState);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                SimulationFailed(
                    Server.Telemetry.CreateLogger<Isa95JobControlNodeManager>(),
                    ex);
            }
        }

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Error,
            Message = "ISA-95 job-control event simulation failed.")]
        private static partial void SimulationFailed(ILogger logger, Exception exception);

        internal const string kModelUri =
            UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2;

        private static readonly TimeSpan kEventInterval = TimeSpan.FromSeconds(1);
        private CancellationTokenSource? _simulationCancellation;
        private int _jobId;
    }
}
