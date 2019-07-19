// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Design.Schema;
    using System.Xml;

    /// <summary>
    /// Resolves node dependencies
    /// </summary>
    public interface INodeResolver {

        /// <summary>
        /// Resolve a symbolic id to a node design
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="symbolicId"></param>
        /// <returns></returns>
        NodeDesign TryResolve(Namespace ns, XmlQualifiedName symbolicId);
    }
}
