// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Request service calls in batch
    /// </summary>
    public class BatchServiceCallRequestModel :
        List<ServiceCallRequestModel> {
    }
}
