// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System.Runtime.Serialization;

    /// <summary>
    /// A typed base class for all data variable nodes.
    /// </summary>
    [DataContract(Name = "DataVariable")]
    public class DataVariableNodeModel : VariableNodeModel {

        /// <summary>
        /// Create data variable node
        /// </summary>
        public DataVariableNodeModel(BaseNodeModel parent = null) :
            base(parent) {
        }
    }
}
