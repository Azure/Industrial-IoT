// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Alarms
{
    using Opc.Ua;

    /// <summary>
    /// The OPC UA stack stores condition branches in a plain, non thread-safe
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> that is
    /// both enumerated (<c>ConditionState.GetRetainState</c>,
    /// <c>ConditionState.ConditionRefresh</c>) and mutated
    /// (<c>CreateBranch</c>, <c>ReplaceBranchEvent</c>, <c>RemoveBranchEvent</c>)
    /// without any synchronization. When the alarm simulation drives state
    /// changes on background threads while the dictionary is concurrently read,
    /// the enumeration can observe a torn entry and dereference it, crashing the
    /// process with a native access violation in <c>ConditionState.IsBranch</c>.
    /// The guarded alarm states below serialize all branch access on the owning
    /// node manager lock - the same lock every alarm mutation in this server
    /// already takes - and propagate that lock to branches created by the stack.
    /// </summary>
    internal interface IBranchGuarded
    {
        /// <summary>
        /// Lock guarding access to the condition branch dictionary.
        /// </summary>
        object BranchLock { get; }
    }

    /// <summary>
    /// Helpers for the guarded alarm states.
    /// </summary>
    internal static class BranchGuard
    {
        /// <summary>
        /// The stack's retain-state evaluation dereferences BranchId (via IsBranch)
        /// and EnabledState. Under heavy CI load these have been observed to be null
        /// on a condition that is otherwise in use, which crashes the test host with
        /// a native access violation. Skip the evaluation in that case - an
        /// uninitialized condition is treated as not retained - rather than crash.
        /// In normal operation BranchId/EnabledState are always present, so behavior
        /// is unchanged.
        /// </summary>
        /// <param name="condition"></param>
        public static bool IsInitialized(ConditionState condition)
        {
            return condition.BranchId != null && condition.EnabledState?.Id != null;
        }
    }

    /// <summary>
    /// Guarded <see cref="AlarmConditionState"/>.
    /// </summary>
    internal sealed class GuardedAlarmConditionState : AlarmConditionState, IBranchGuarded
    {
        /// <inheritdoc/>
        public object BranchLock { get; }

        /// <summary>
        /// Create the original condition with the owning node manager lock.
        /// </summary>
        public GuardedAlarmConditionState(NodeState parent, object branchLock)
            : base(parent)
        {
            BranchLock = branchLock ?? new object();
        }

        /// <summary>
        /// Used by the stack when creating branches via reflection - inherit the
        /// lock from the originating condition passed as the parent.
        /// </summary>
        public GuardedAlarmConditionState(NodeState parent)
            : this(parent, (parent as IBranchGuarded)?.BranchLock)
        {
        }

        /// <inheritdoc/>
        protected override bool GetRetainState()
        {
            lock (BranchLock)
            {
                return BranchGuard.IsInitialized(this) && base.GetRetainState();
            }
        }

        /// <inheritdoc/>
        public override ConditionState CreateBranch(ISystemContext context, NodeId branchId)
        {
            lock (BranchLock)
            {
                return base.CreateBranch(context, branchId);
            }
        }
    }

    /// <summary>
    /// Guarded <see cref="ExclusiveDeviationAlarmState"/>.
    /// </summary>
    internal sealed class GuardedExclusiveDeviationAlarmState : ExclusiveDeviationAlarmState, IBranchGuarded
    {
        /// <inheritdoc/>
        public object BranchLock { get; }

        /// <summary>
        /// Create the original condition with the owning node manager lock.
        /// </summary>
        public GuardedExclusiveDeviationAlarmState(NodeState parent, object branchLock)
            : base(parent)
        {
            BranchLock = branchLock ?? new object();
        }

        /// <summary>
        /// Used by the stack when creating branches via reflection - inherit the
        /// lock from the originating condition passed as the parent.
        /// </summary>
        public GuardedExclusiveDeviationAlarmState(NodeState parent)
            : this(parent, (parent as IBranchGuarded)?.BranchLock)
        {
        }

        /// <inheritdoc/>
        protected override bool GetRetainState()
        {
            lock (BranchLock)
            {
                return BranchGuard.IsInitialized(this) && base.GetRetainState();
            }
        }

        /// <inheritdoc/>
        public override ConditionState CreateBranch(ISystemContext context, NodeId branchId)
        {
            lock (BranchLock)
            {
                return base.CreateBranch(context, branchId);
            }
        }
    }

    /// <summary>
    /// Guarded <see cref="NonExclusiveLevelAlarmState"/>.
    /// </summary>
    internal sealed class GuardedNonExclusiveLevelAlarmState : NonExclusiveLevelAlarmState, IBranchGuarded
    {
        /// <inheritdoc/>
        public object BranchLock { get; }

        /// <summary>
        /// Create the original condition with the owning node manager lock.
        /// </summary>
        public GuardedNonExclusiveLevelAlarmState(NodeState parent, object branchLock)
            : base(parent)
        {
            BranchLock = branchLock ?? new object();
        }

        /// <summary>
        /// Used by the stack when creating branches via reflection - inherit the
        /// lock from the originating condition passed as the parent.
        /// </summary>
        public GuardedNonExclusiveLevelAlarmState(NodeState parent)
            : this(parent, (parent as IBranchGuarded)?.BranchLock)
        {
        }

        /// <inheritdoc/>
        protected override bool GetRetainState()
        {
            lock (BranchLock)
            {
                return BranchGuard.IsInitialized(this) && base.GetRetainState();
            }
        }

        /// <inheritdoc/>
        public override ConditionState CreateBranch(ISystemContext context, NodeId branchId)
        {
            lock (BranchLock)
            {
                return base.CreateBranch(context, branchId);
            }
        }
    }

    /// <summary>
    /// Guarded <see cref="TripAlarmState"/>.
    /// </summary>
    internal sealed class GuardedTripAlarmState : TripAlarmState, IBranchGuarded
    {
        /// <inheritdoc/>
        public object BranchLock { get; }

        /// <summary>
        /// Create the original condition with the owning node manager lock.
        /// </summary>
        public GuardedTripAlarmState(NodeState parent, object branchLock)
            : base(parent)
        {
            BranchLock = branchLock ?? new object();
        }

        /// <summary>
        /// Used by the stack when creating branches via reflection - inherit the
        /// lock from the originating condition passed as the parent.
        /// </summary>
        public GuardedTripAlarmState(NodeState parent)
            : this(parent, (parent as IBranchGuarded)?.BranchLock)
        {
        }

        /// <inheritdoc/>
        protected override bool GetRetainState()
        {
            lock (BranchLock)
            {
                return BranchGuard.IsInitialized(this) && base.GetRetainState();
            }
        }

        /// <inheritdoc/>
        public override ConditionState CreateBranch(ISystemContext context, NodeId branchId)
        {
            lock (BranchLock)
            {
                return base.CreateBranch(context, branchId);
            }
        }
    }
}
