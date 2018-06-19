// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models.Users {
    using I40.DIN91345.Models.Access;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Group of users
    /// </summary>
    public class UserGroup {

        /// <summary>
        /// User
        /// </summary>
        [JsonProperty(PropertyName = "users",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<User> User { get; set; }

        /// <summary>
        /// Role of group
        /// </summary>
        [JsonProperty(PropertyName = "has",
            NullValueHandling = NullValueHandling.Ignore)]
        public Role Has { get; set; }
    }
}