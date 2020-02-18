// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Collections.Generic;

    /// <summary>
    /// HeartbeatResponse Api Model
    /// </summary>
    public class HeartbeatResponseApiModel : List<HeartbeatResponseEntryApiModel> {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HeartbeatResponseApiModel() {

        }

        /// <summary>
        /// Default constructor with initializer.
        /// </summary>
        /// <param name="collection"></param>
        public HeartbeatResponseApiModel(IEnumerable<HeartbeatResponseEntryApiModel> collection) : base(collection) {

        }
    }
}