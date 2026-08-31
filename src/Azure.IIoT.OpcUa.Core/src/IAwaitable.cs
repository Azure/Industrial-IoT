// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core
{
    /// <summary>
    /// <para>
    /// Sometimes it is convenient to fire and forget asynchronous
    /// tasks, e.g., the creation of long running operations or
    /// the initialization of external resources in the constructor.
    /// </para>
    /// <para>
    /// This interface can be implemented to allow a user of the
    /// class to ensure the initialization has been completed
    /// before using it the first time, e.g., in test scenarios.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">The awaited result.</typeparam>
    public interface IAwaitable<out TResult> : IAwaitable
    {
        /// <summary>
        /// Get the awaiter
        /// </summary>
        IAwaiter<TResult> GetAwaiter();
    }

    /// <summary>
    /// Awaitable without result
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IAwaitable;
#pragma warning restore CA1040 // Avoid empty interfaces
}
