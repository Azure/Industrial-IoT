// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Counter
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// A node manager exposing a configurable number of variables that all
    /// count up from zero in lockstep. One increment is produced every
    /// update interval, so the value of a variable is at the same time its
    /// sequence number: a consumer can detect a lost value as a gap and a
    /// reordered value as a decrease, without any additional bookkeeping.
    /// </para>
    /// <para>
    /// Variables declare a minimum sampling interval of zero which makes the
    /// stack monitor them change based instead of through a sampling group.
    /// Every increment is therefore queued on every monitored item, and with
    /// a sufficiently large queue the expected telemetry stream is complete
    /// and gap free no matter which sampling interval a client requests.
    /// </para>
    /// </summary>
    public sealed class CounterNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Highest counter value that has been produced so far. Every
        /// variable has been set to every value between zero and this
        /// value inclusive.
        /// </summary>
        public long CurrentValue => Interlocked.Read(ref _counter);

        /// <summary>
        /// Time at which the counter value zero was produced. Source
        /// timestamps are derived from this instant when scheduled
        /// timestamps are enabled.
        /// </summary>
        public DateTime Epoch { get; private set; }

        /// <summary>
        /// Options in use
        /// </summary>
        public CounterServerOptions Options { get; }

        /// <summary>
        /// The source timestamp the server actually stamped on the given
        /// counter value, which is the ground truth a consumer's telemetry
        /// must reproduce exactly.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timestamp"></param>
        public bool TryGetEmitted(long value, out DateTime timestamp)
        {
            lock (Lock)
            {
                return _emitted.TryGetValue(value, out timestamp);
            }
        }

        /// <summary>
        /// Create node manager
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public CounterNodeManager(IServerInternal server,
            ApplicationConfiguration configuration, CounterServerOptions options)
            : base(server, configuration, Namespaces.Counter)
        {
            Options = options ?? new CounterServerOptions();
            SystemContext.NodeIdFactory = this;
        }

        /// <summary>
        /// Browse name of the counter variable with the given index.
        /// </summary>
        /// <param name="index"></param>
        public static string GetBrowseName(int index)
        {
            return string.Create(CultureInfo.InvariantCulture, $"Counter_{index}");
        }

        /// <summary>
        /// Node id of the counter variable with the given index in the
        /// namespace uri form understood by the published nodes
        /// configuration.
        /// </summary>
        /// <param name="index"></param>
        public static string GetNodeId(int index)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"nsu={Namespaces.Counter};s={GetBrowseName(index)}");
        }

        /// <inheritdoc/>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        /// <inheritdoc/>
        public override void CreateAddressSpace(
            IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder,
                    out var references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = [];
                }

                Epoch = DateTime.UtcNow;

                var root = new FolderState(null)
                {
                    SymbolicName = kRootName,
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    TypeDefinitionId = ObjectTypeIds.FolderType,
                    NodeId = new NodeId(kRootName, NamespaceIndex),
                    BrowseName = new QualifiedName(kRootName, NamespaceIndex),
                    DisplayName = new LocalizedText("en", kRootName),
                    WriteMask = AttributeWriteMask.None,
                    UserWriteMask = AttributeWriteMask.None,
                    EventNotifier = EventNotifiers.None
                };
                root.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypeIds.Organizes,
                    false, root.NodeId));

                _variables = new BaseDataVariableState[Options.NodeCount];
                for (var index = 0; index < Options.NodeCount; index++)
                {
                    var name = GetBrowseName(index);
                    var variable = new BaseDataVariableState(root)
                    {
                        SymbolicName = name,
                        ReferenceTypeId = ReferenceTypeIds.Organizes,
                        TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                        NodeId = new NodeId(name, NamespaceIndex),
                        BrowseName = new QualifiedName(name, NamespaceIndex),
                        DisplayName = new LocalizedText("en", name),
                        WriteMask = AttributeWriteMask.None,
                        UserWriteMask = AttributeWriteMask.None,
                        DataType = DataTypeIds.UInt64,
                        ValueRank = ValueRanks.Scalar,
                        AccessLevel = AccessLevels.CurrentRead,
                        UserAccessLevel = AccessLevels.CurrentRead,
                        Historizing = false,
                        Value = (ulong)0,
                        StatusCode = StatusCodes.Good,
                        Timestamp = Epoch,
                        //
                        // Zero means the stack reports every change instead of
                        // polling the node through a sampling group. Without
                        // this, values are sampled at the client's requested
                        // interval and increments are legitimately lost, which
                        // would make gap detection meaningless.
                        //
                        MinimumSamplingInterval = 0
                    };
                    root.AddChild(variable);
                    _variables[index] = variable;
                }

                AddPredefinedNode(SystemContext, root);
            }

            _updates = Task.Factory.StartNew(() => RunAsync(_cts.Token),
                _cts.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                _variables = null;
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _cts.Cancel();
                    _updates?.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }
                catch (AggregateException) { }
                finally
                {
                    _updates = null;
                    _cts.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Produce one increment on every counter variable.
        /// </summary>
        private async Task RunAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(Options.UpdateInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    var value = Interlocked.Increment(ref _counter);
                    var timestamp = Options.UseScheduledTimestamps
                        ? Epoch.AddTicks(value * Options.UpdateInterval.Ticks)
                        : DateTime.UtcNow;
                    if (Options.IsSlipped(value))
                    {
                        timestamp = timestamp.Add(Options.SlotSlip);
                    }
                    lock (Lock)
                    {
                        var variables = _variables;
                        if (variables == null)
                        {
                            continue;
                        }
                        //
                        // Record what was actually stamped so a test can
                        // compare the telemetry it receives against the
                        // ground truth rather than against a recomputation
                        // of the same rule.
                        //
                        _emitted[value] = timestamp;
                        foreach (var variable in variables)
                        {
                            variable.Value = (ulong)value;
                            variable.Timestamp = timestamp;
                            variable.ClearChangeMasks(SystemContext, false);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private const string kRootName = "Counters";
        private readonly CancellationTokenSource _cts = new();
        private readonly Dictionary<long, DateTime> _emitted = [];
        private BaseDataVariableState[] _variables;
        private Task _updates;
        private long _counter;
    }
}
