// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {

    /// <summary>
    /// A property, i.e. a property instance, is described by a
    /// property description. The description of the property
    /// should follow a standardized schema (realized as template).
    /// </summary>
    public class PropertyDescription : BaseSemanticModel, IHasSemanticId,
        IHasTemplate, IIdentifiable {

        // Has template definition and describes a property
    }
}