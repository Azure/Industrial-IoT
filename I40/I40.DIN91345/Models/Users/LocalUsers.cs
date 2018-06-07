// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace I40.DIN91345.Models.Users {

    /// <summary>
    /// Local users
    /// </summary>
    public class LocalUsers : Users {

        /// <summary>
        /// Local Users
        /// </summary>
        [JsonProperty(PropertyName = "users",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<User> Users { get; set; }

        /// <summary>
        /// Local User groups
        /// </summary>
        [JsonProperty(PropertyName = "groups",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<UserGroup> Groups { get; set; }
    }
}