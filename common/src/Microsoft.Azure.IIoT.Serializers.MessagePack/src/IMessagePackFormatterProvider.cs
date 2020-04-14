// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using System.Collections.Generic;
    using global::MessagePack;

    /// <summary>
    /// Formtter provider
    /// </summary>
    public interface IMessagePackFormatterResolverProvider {

        /// <summary>
        /// Get Resolvers
        /// </summary>
        /// <returns></returns>
        IEnumerable<IFormatterResolver> GetResolvers();
    }
}
