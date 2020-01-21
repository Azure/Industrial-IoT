// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides job types to deserialize
    /// </summary>
    public interface IKnownJobConfigProvider {

        /// <summary>
        /// Returns the known job types
        /// </summary>
        IEnumerable<Type> KnownJobTypes { get; }
    }
}