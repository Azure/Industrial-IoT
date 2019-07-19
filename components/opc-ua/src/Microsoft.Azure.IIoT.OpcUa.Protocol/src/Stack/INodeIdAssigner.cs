// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Design.Schema;
    using System.Xml;

    /// <summary>
    /// Assign node ids
    /// </summary>
    public interface INodeIdAssigner {

        /// <summary>
        /// Try to get a predefined node id for a symbolic link
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="symbolicId"></param>
        /// <returns></returns>
        object TryAssignId(Namespace ns, XmlQualifiedName symbolicId);
    }
}
