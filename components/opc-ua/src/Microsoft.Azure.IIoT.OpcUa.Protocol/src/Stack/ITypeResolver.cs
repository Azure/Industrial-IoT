// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using Opc.Ua.Types.Schema;
    using System.Xml;

    /// <summary>
    /// Resolve data type dependencies
    /// </summary>
    public interface ITypeResolver {

        /// <summary>
        /// Resolve a data type using an import directive.
        /// A resolver can delegate to a child resolver,
        /// but otherwise should only resolve if the import
        /// directive matches its namespace.
        /// </summary>
        /// <param name="import"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        DataType TryResolve(ImportDirective import,
            XmlQualifiedName typeName);
    }
}
