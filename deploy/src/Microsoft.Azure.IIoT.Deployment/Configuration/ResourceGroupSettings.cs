// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    class ResourceGroupSettings {
        public string Name { get; set; }
        public bool? UseExisting { get; set; }
        public RegionType? Region { get; set; }
    }
}
