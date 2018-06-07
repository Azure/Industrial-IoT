// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Admin information. In v1 it only contains version
    /// information as defined by IEC 61360.
    /// </summary>
    public class AdministrativeInformation : BaseTemplated {

        /// <summary>
        /// [0..1] Revision. Status of revision of the class
        /// definition with citing of main and auxiliary number.
        /// Sole obligatory attribute across all revisions of the class.
        /// </summary>
        [JsonProperty(PropertyName = "revision",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Revision { get; set; }

        /// <summary>
        /// [0..1] Version of element
        /// </summary>
        [JsonProperty(PropertyName = "version",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
    }
}