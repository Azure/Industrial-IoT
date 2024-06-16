// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using UaStructureType = Opc.Ua.StructureType;
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Pub sub related opc ua extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert structure type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UaStructureType ToStackType(this StructureType? type)
        {
            switch (type)
            {
                case StructureType.StructureWithOptionalFields:
                    return UaStructureType.StructureWithOptionalFields;
                case StructureType.Union:
                    return UaStructureType.Union;
                case StructureType.StructureWithSubtypedValues:
                    return UaStructureType.StructureWithSubtypedValues;
                case StructureType.UnionWithSubtypedValues:
                    return UaStructureType.UnionWithSubtypedValues;
                default:
                    return UaStructureType.Structure;
            }
        }

        /// <summary>
        /// Convert structure type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static StructureType ToServiceType(this UaStructureType type)
        {
            switch (type)
            {
                case UaStructureType.StructureWithOptionalFields:
                    return StructureType.StructureWithOptionalFields;
                case UaStructureType.Union:
                    return StructureType.Union;
                case UaStructureType.StructureWithSubtypedValues:
                    return StructureType.StructureWithSubtypedValues;
                case UaStructureType.UnionWithSubtypedValues:
                    return StructureType.UnionWithSubtypedValues;
                default:
                    return StructureType.Structure;
            }
        }
    }
}
