// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    /// <summary>
    /// Marks exceptions that are transient in nature, i.e.
    /// they can be retried with another outcome.
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface ITransientException;
#pragma warning restore CA1040 // Avoid empty interfaces
}
