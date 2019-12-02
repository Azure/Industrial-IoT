// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Cli {

    using System;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    public static class RegionTypeExtension {
        public static Region ToRegion(this RegionType region) {
            return region switch
            {
                RegionType.USEast2 => Region.USEast2,
                RegionType.USWest2 => Region.USWest2,
                RegionType.EuropeNorth => Region.EuropeNorth,
                RegionType.EuropeWest => Region.EuropeWest,
                RegionType.AsiaSouthEast => Region.AsiaSouthEast,
                _ => throw new ArgumentException($"Unrecognized RegionType: {region.ToString()}")
            };
        }
    }
}
