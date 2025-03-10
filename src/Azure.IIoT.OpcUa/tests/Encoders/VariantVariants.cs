// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;

    public sealed record class VariantsHolder(
        IReadOnlyList<Variant> Variants, TypeInfo TypeInfo)
    {
        public override string ToString()
        {
            return TypeInfo.ToString();
        }
    }

    public sealed record class VariantHolder(Variant Variant)
    {
        public override string ToString()
        {
            return Variant.TypeInfo.ToString();
        }
    }

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
            yield return new Variant(new NodeId([1, 2, 3, 4, 5, 6, 7, 8], 0));
            yield return new Variant(new NodeId("test", 0));
            yield return new Variant(new NodeId(1u, 0));
            yield return new Variant(new NodeId(Guid.NewGuid(), 0));
            yield return new Variant(NodeId.Null);
            yield return new Variant((NodeId)null);
            yield return new Variant(new ExpandedNodeId([1, 2, 3, 4, 5, 6, 7, 8], 0));
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
            yield return new Variant((StatusCode)StatusCodes.Bad);
            yield return new Variant((StatusCode)StatusCodes.UncertainDependentValueChanged);
            yield return new Variant(new DataValue(StatusCodes.BadNoCommunication));
            yield return new Variant(new DataValue(new Variant(123)));
            yield return new Variant(XmlElement);
        }

        public static XmlElement XmlElement
        {
            get
            {
                var doc = new XmlDocument();
                doc.LoadXml(
              """
<?xml version="1.0" encoding="UTF-8"?>
            <note>
                <to>Tove</to>
                <from>Jani</from>
                <heading test="1.0">Reminder</heading>
                <author><nothing/></author>
                <body>Don't forget me this weekend!</body>
            </note>
"""
                );
                return doc.DocumentElement;
            }
        }

        public static readonly ProgramDiagnostic2DataType Complex = new()
        {
            CreateClientName = "Testname",
            CreateSessionId = new NodeId(Guid.NewGuid()),
            InvocationCreationTime = DateTime.UtcNow,
            LastMethodCall = "swappido",
            LastMethodCallTime = DateTime.UtcNow,
            LastMethodInputArguments = [
                    new Argument("something1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something2",
                        new NodeId(23), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something3",
                        new NodeId(44), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("something4",
                        new NodeId(23), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                ],
            LastMethodInputValues = [
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(["1", "2", "3", "4", "5"])
                ],
            LastMethodOutputArguments = [
                    new Argument("foo1",
                        new NodeId(2354), -1, "somedesciroeioi") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("foo2",
                        new NodeId(33), -1, "fdsadfsdaf") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("adfsdafsdsdsafdsfa",
                        new NodeId("absc", 0), 1, "fsadf  sadfsdfsadfsd") { ArrayDimensions = Array.Empty<uint>() },
                    new Argument("ddddd",
                        new NodeId(25), 1, "dfad  sdafdfdf  fasdf") { ArrayDimensions = Array.Empty<uint>() }
                ],
            LastMethodOutputValues = [
                    new Variant(4L),
                    new Variant("test"),
                    new Variant(new long[] {1, 2, 3, 4, 5 }),
                    new Variant(["1", "2", "3", "4", "5"])
                ],
            LastMethodReturnStatus =
                    StatusCodes.BadAggregateConfigurationRejected,
            LastMethodSessionId = new NodeId(RandomNumberGenerator.GetBytes(32)),
            LastTransitionTime = DateTime.UtcNow - TimeSpan.FromDays(23)
        };
    }
}
