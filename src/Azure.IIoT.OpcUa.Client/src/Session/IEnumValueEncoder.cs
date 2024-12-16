// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;

/// <summary>
/// Extens encoders to encode enumerated values
/// </summary>
public interface IEnumValueEncoder
{
    /// <summary>
    /// Read enum value
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="enumValue"></param>
    /// <param name="enumDefinition"></param>
    /// <returns></returns>
    void WriteEnumerated(string fieldName, EnumValue enumValue, EnumDefinition enumDefinition);
}
