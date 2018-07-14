// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MsgPack {

    /// <summary>
    /// Header values for reader/writer
    /// </summary>
    enum Types : byte {

        PositiveFixNum = 0x00,  // 0x00 - 0x7f
        NegativeFixNum = 0xe0,  // 0xe0 - 0xff

        Nil = 0xc0,
        False = 0xc2,
        True = 0xc3,
        Float = 0xca,
        Double = 0xcb,
        Uint8 = 0xcc,
        UInt16 = 0xcd,
        UInt32 = 0xce,
        UInt64 = 0xcf,
        Int8 = 0xd0,
        Int16 = 0xd1,
        Int32 = 0xd2,
        Int64 = 0xd3,
        Str8 = 0xd9,
        Str16 = 0xda,
        Str32 = 0xdb,
        Array16 = 0xdc,
        Array32 = 0xdd,
        Map16 = 0xde,
        Map32 = 0xdf,
        Bin8 = 0xc4,
        Bin16 = 0xc5,
        Bin32 = 0xc6,
        Ext8 = 0xc7,
        Ext16 = 0xc8,
        Ext32 = 0xc9,

        FixStr = 0xa0,          // 0xa0 - 0xbf
        FixArray = 0x90,        // 0x90 - 0x9f
        FixMap = 0x80,          // 0x80 - 0x8f
        FixExt1 = 0xd4,
        FixExt2 = 0xd5,
        FixExt4 = 0xd6,
        FixExt8 = 0xd7,
        FixExt16 = 0xd8
    }
}