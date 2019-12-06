// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Services {
    using System.Collections.Generic;
    public class PublishedNode {
        public string NodeId { get; set; }
        public int? SampligInterval { get; set; }
        public int? PublishingInterval { get; set; }
    }
}
