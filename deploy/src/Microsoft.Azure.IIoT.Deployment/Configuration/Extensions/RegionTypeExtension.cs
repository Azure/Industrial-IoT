﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration.Extension {

    using System;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    public static class RegionTypeExtension {
        public static Region ToRegion(this RegionType region) {
            return region switch
            {
                RegionType.USEast => Region.USEast,
                RegionType.USEast2 => Region.USEast2,
                RegionType.USWest => Region.USWest,
                RegionType.USWest2 => Region.USWest2,
                RegionType.USCentral => Region.USCentral,
                RegionType.EuropeNorth => Region.EuropeNorth,
                RegionType.EuropeWest => Region.EuropeWest,
                RegionType.AsiaSouthEast => Region.AsiaSouthEast,
                RegionType.AustraliaEast => Region.AustraliaEast,
                _ => throw new Exception($"Unrecognized RegionType: {region}")
            };
        }
    }
}
