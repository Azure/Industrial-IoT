// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Azure.IIoT.App.Models {
    public class CredentialModel {
        [Required]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "UserName should be between 3 and 25 characters.")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "Password should be between 3 and 25 characters.")]
        public string Password { get; set; }
    }
}
