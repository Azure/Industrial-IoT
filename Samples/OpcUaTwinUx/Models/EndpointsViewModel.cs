// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.Azure.IIoT.Browser.Models {
    public class EndpointsViewModel {
        public int EndpointId { get; set; }
        public List<SelectListItem> PrepopulatedEndpoints { get; set; }
    }
}