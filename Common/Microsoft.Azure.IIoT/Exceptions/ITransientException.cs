// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {

    /// <summary>
    /// Marks exceptions that are transient in nature, i.e.
    /// they can be retried with another outcome.
    /// </summary>
    public interface ITransientException {
    }
}