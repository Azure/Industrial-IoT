// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class VariantVariants
    {
        public static IEnumerable<Variant> GetValues()
        {
            yield return new Variant(true);
            yield return new Variant(false);
            yield return new Variant((sbyte)1);
            yield return new Variant((sbyte)-1);
            yield return new Variant((sbyte)0);
            yield return new Variant(sbyte.MaxValue);
            yield return new Variant(sbyte.MinValue);
            yield return new Variant((short)1);
            yield return new Variant((short)-1);
            yield return new Variant((short)0);
            yield return new Variant(short.MaxValue);
            yield return new Variant(short.MinValue);
            yield return new Variant(1);
            yield return new Variant(-1);
            yield return new Variant(0);
            yield return new Variant(int.MaxValue);
            yield return new Variant(int.MinValue);
            yield return new Variant(1L);
            yield return new Variant(-1L);
            yield return new Variant(0L);
            yield return new Variant(long.MaxValue);
            yield return new Variant(long.MinValue);
            yield return new Variant(1UL);
            yield return new Variant(0UL);
            yield return new Variant(ulong.MaxValue);
            yield return new Variant(11790719998462990154);
            yield return new Variant(10042278942021613161);
            yield return new Variant(1u);
            yield return new Variant(0u);
            yield return new Variant(uint.MaxValue);
            yield return new Variant((ushort)1);
            yield return new Variant((ushort)0);
            yield return new Variant(ushort.MaxValue);
            yield return new Variant((byte)1);
            yield return new Variant((byte)0);
            yield return new Variant(byte.MaxValue);
            yield return new Variant(1.0);
            yield return new Variant(-1.0);
            yield return new Variant(0.0);
            yield return new Variant(double.MaxValue);
            yield return new Variant(double.MinValue);
            yield return new Variant(double.PositiveInfinity);
            yield return new Variant(double.NegativeInfinity);
            yield return new Variant(1.0f);
            yield return new Variant(-1.0f);
            yield return new Variant(0.0f);
            yield return new Variant(float.MaxValue);
            yield return new Variant(float.MinValue);
            yield return new Variant(float.PositiveInfinity);
            yield return new Variant(float.NegativeInfinity);
            // yield return new Variant((decimal)1.0);
            // yield return new Variant((decimal)-1.0);
            // yield return new Variant((decimal)0.0);
            // yield return new Variant((decimal)1234567);
            // yield return new Variant(decimal.MaxValue);
            // yield return new Variant(decimal.MinValue);
            yield return new Variant(Guid.NewGuid());
            yield return new Variant(Guid.Empty);
            yield return new Variant(DateTime.UtcNow);
            yield return new Variant(DateTime.Now);
            yield return new Variant(DateTime.MaxValue);
            yield return new Variant(DateTime.MinValue);
            yield return new Variant(string.Empty);
            yield return new Variant((string)null);
            yield return new Variant(" str ing");
            yield return new Variant("zeroterminated" + '\0');
            yield return new Variant(Array.Empty<byte>());
            yield return new Variant((byte[])null);
            yield return new Variant(new byte[1000]);
            yield return new Variant(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            yield return new Variant(Encoding.UTF8.GetBytes("utf-8-string"));
            yield return new Variant(new NodeId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0));
            yield return new Variant(new NodeId("test", 0));
            yield return new Variant(new NodeId(1u, 0));
            yield return new Variant(new NodeId(Guid.NewGuid(), 0));
            yield return new Variant(NodeId.Null);
            yield return new Variant((NodeId)null);
            yield return new Variant(new ExpandedNodeId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0));
            yield return new Variant(new ExpandedNodeId("test", 0));
            yield return new Variant(new ExpandedNodeId(1u, 0));
            yield return new Variant(new ExpandedNodeId(Guid.NewGuid(), 0));
            yield return new Variant((ExpandedNodeId)null);
            yield return new Variant(new LocalizedText("en", "text"));
            yield return new Variant(new LocalizedText("text2"));
            yield return new Variant(new LocalizedText(string.Empty));
            yield return new Variant((LocalizedText)null);
            yield return new Variant(new QualifiedName("en", 0));
            yield return new Variant(new QualifiedName(string.Empty, 0));
            yield return new Variant((QualifiedName)null);
        }
    }
}
