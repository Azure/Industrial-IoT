// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Serializer {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manual job configuration provider
    /// </summary>
    public class KnownJobConfigProvider : IKnownJobConfigProvider {

        /// <summary>
        /// Create with known types
        /// </summary>
        /// <param name="knownTypes"></param>
        public KnownJobConfigProvider(IEnumerable<Type> knownTypes) {
            KnownJobTypes = knownTypes;
        }

        /// <inheritdoc/>
        public IEnumerable<Type> KnownJobTypes { get; }
    }
}