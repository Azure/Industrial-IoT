// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;

    /// <summary>
    /// Variant codec factory
    /// </summary>
    public interface IVariantEncoderFactory {

        /// <summary>
        /// Default encoder
        /// </summary>
        IVariantEncoder Default { get; }

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        IVariantEncoder Create(IServiceMessageContext context);
    }
}
