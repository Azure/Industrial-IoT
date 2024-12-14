// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

/// <summary>
/// <para>
/// This is an object that works around the limits of the encoder
/// decoder api today which uses the .net Enum type to represent
/// enumerations. EnumObject is a enum value with both symbol and
/// value which can be decoded and encoded using the enum description.
/// </para>
/// <para>
/// There are only 2 cases where a custom enum can occur:
/// Inside a custom structure and inside a Variant.
/// </para>
/// <para>
/// The first case is covered by the custom structure encoder/decoder
/// where we have special casing for this type.
/// </para>
/// <para>
/// The second case is handled by the encoder/decoder in that any
/// enumeration value in a Variant is encoded as a 32 bit integer
/// and thus will not even hit us here. This needs to be validated!!
/// </para>
/// </summary>
/// <param name="Symbol"></param>
/// <param name="Value"></param>
internal sealed record class EnumValue(string Symbol, long Value);
