// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.MessagePack {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Xunit;
    using System.Runtime.Serialization;

    public class SerializerTests {

        public virtual ISerializer Serializer => new MessagePackSerializer();

        public static IEnumerable<(VariantValue, object)> GetStrings() {
            yield return ("", "");
            yield return ("str ing", "str ing");
            yield return ("{}", "{}");
            yield return (Array.Empty<byte>(), Array.Empty<byte>());
            yield return (new byte[1000], new byte[1000]);
            yield return (new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            yield return (Encoding.UTF8.GetBytes("utf-8-string"), Encoding.UTF8.GetBytes("utf-8-string"));
        }

        public static IEnumerable<(VariantValue, object)> GetValues() {
            yield return (true, true);
            yield return (false, false);
            yield return ((bool?)null, (bool?)null);
            yield return ((sbyte)1, (sbyte)1);
            yield return ((sbyte)-1, (sbyte)-1);
            yield return ((sbyte)0, (sbyte)0);
            yield return (sbyte.MaxValue, sbyte.MaxValue);
            yield return (sbyte.MinValue, sbyte.MinValue);
            yield return ((sbyte?)null, (sbyte?)null);
            yield return ((short)1, (short)1);
            yield return ((short)-1, (short)-1);
            yield return ((short)0, (short)0);
            yield return (short.MaxValue, short.MaxValue);
            yield return (short.MinValue, short.MinValue);
            yield return ((short?)null, (short?)null);
            yield return (1, 1);
            yield return (-1, -1);
            yield return (0, 0);
            yield return (int.MaxValue, int.MaxValue);
            yield return (int.MinValue, int.MinValue);
            yield return ((int?)null, (int?)null);
            yield return (1L, 1L);
            yield return (-1L, -1L);
            yield return (0L, 0L);
            yield return (long.MaxValue, long.MaxValue);
            yield return (long.MinValue, long.MinValue);
            yield return ((long?)null, (long?)null);
            yield return (1UL, 1UL);
            yield return (0UL, 0UL);
            yield return (ulong.MaxValue, ulong.MaxValue);
            yield return ((ulong?)null, (ulong?)null);
            yield return (1u, 1u);
            yield return (0u, 0u);
            yield return (uint.MaxValue, uint.MaxValue);
            yield return ((uint?)null, (uint?)null);
            yield return ((ushort)1, (ushort)1);
            yield return ((ushort)0, (ushort)0);
            yield return (ushort.MaxValue, ushort.MaxValue);
            yield return ((ushort?)null, (ushort?)null);
            yield return ((byte)1, (byte)1);
            yield return ((byte)0, (byte)0);
            yield return (1.0, 1.0);
            yield return (-1.0, -1.0);
            yield return (0.0, 0.0);
            yield return (byte.MaxValue, byte.MaxValue);
            yield return ((byte?)null, (byte?)null);
            yield return (double.MaxValue, double.MaxValue);
            yield return (double.MinValue, double.MinValue);
            yield return (double.PositiveInfinity, double.PositiveInfinity);
            yield return (double.NegativeInfinity, double.NegativeInfinity);
            yield return ((double?)null, (double?)null);
            yield return (1.0f, 1.0f);
            yield return (-1.0f, -1.0f);
            yield return (0.0f, 0.0f);
            yield return (float.MaxValue, float.MaxValue);
            yield return (float.MinValue, float.MinValue);
            yield return (float.PositiveInfinity, float.PositiveInfinity);
            yield return (float.NegativeInfinity, float.NegativeInfinity);
            yield return ((float?)null, (float?)null);
            yield return ((decimal)1.0, (decimal)1.0);
            yield return ((decimal)-1.0, (decimal)-1.0);
            yield return ((decimal)0.0, (decimal)0.0);
            yield return ((decimal)1234567, (decimal)1234567);
            yield return ((decimal?)null, (decimal?)null);
            //  yield return (decimal.MaxValue, decimal.MaxValue);
            //  yield return (decimal.MinValue, decimal.MinValue);
            var guid = Guid.NewGuid();
            yield return (guid, guid);
            yield return (Guid.Empty, Guid.Empty);
            var now1 = DateTime.UtcNow;
            yield return (now1, now1);
            yield return (DateTime.MaxValue, DateTime.MaxValue);
            yield return (DateTime.MinValue, DateTime.MinValue);
            yield return ((DateTime?)null, (DateTime?)null);
            var now2 = DateTimeOffset.UtcNow;
            yield return (now2, now2);
            // TODO FIX yield return (DateTimeOffset.MaxValue, DateTimeOffset.MaxValue);
            // TODO FIX yield return (DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            yield return ((DateTimeOffset?)null, (DateTimeOffset?)null);
            yield return (TimeSpan.Zero, TimeSpan.Zero);
            yield return (TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            yield return (TimeSpan.FromDays(5555), TimeSpan.FromDays(5555));
            yield return (TimeSpan.MaxValue, TimeSpan.MaxValue);
            yield return (TimeSpan.MinValue, TimeSpan.MinValue);
            yield return ((TimeSpan?)null, (TimeSpan?)null);
        }


        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerDeserializer(object o, Type type) {
            var result = Serializer.Deserialize(Serializer.SerializeToString(o), type);
            Assert.NotNull(result);
            Assert.Equal(o, result);
            Assert.Equal(o.GetType(), result.GetType());
        }

        [Theory]
        [MemberData(nameof(GetNulls))]
        public void SerializerDeserializerNullable(Type type) {
            var result = Serializer.Deserialize(Serializer.SerializeToString(null), type);
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerArrayVariant3(object o, Type type) {
            var expected = type.MakeArrayType();
            var result = Serializer.FromArray(o, o, o);
            Assert.NotNull(result);
            Assert.True(result.IsListOfValues);
            Assert.True(result.Count == 3);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerArrayVariant2(object o, Type type) {
            var expected = type.MakeArrayType();
            var result = Serializer.FromArray(o, o);
            Assert.NotNull(result);
            Assert.True(result.IsListOfValues);
            Assert.True(result.Count == 2);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerArrayVariantToObject(object o, Type type) {
            var expected = type.MakeArrayType();
            var array = Serializer.FromArray(o, o, o).ConvertTo(expected);

            Assert.NotNull(array);
            Assert.Equal(expected, array.GetType());
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerVariant(object o, Type type) {
            var result = Serializer.FromObject(o).ConvertTo(type);
            Assert.NotNull(result);
            Assert.Equal(o, result);
            Assert.Equal(o.GetType(), result.GetType());
        }

        [Theory]
        [MemberData(nameof(GetNulls))]
        public void SerializerVariantNullable(Type type) {
            var result = Serializer.FromObject(null).ConvertTo(type);
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(GetVariantValueAndValue))]
        public void SerializerSerializeValueToStringAndCompare(VariantValue v, object o) {
            var actual = Serializer.SerializeToString(v);
            var expected = Serializer.SerializeToString(o);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetVariantValues))]
        public void SerializerStringParse(VariantValue v) {
            var expected = v;
            var encstr = Serializer.SerializeToString(v);
            var actual = Serializer.Parse(encstr);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetVariantValues))]
        public void SerializerFromObject(VariantValue v) {
            var expected = v;
            var actual = Serializer.FromObject(v);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDataContractDefaultValues() {
            var str = Serializer.SerializeToBytes(new DataContractModelWithDefaultValues());
            var result = Serializer.Deserialize<DataContractModelWithDefaultValues>(str.ToArray());
            Assert.Equal(0, result.Test1);
            Assert.Null(result.Test2);
            Assert.Null(result.Test3);
            Assert.Equal(4, result.Test4);
        }

        [DataContract]
        public class DataContractModelWithDefaultValues {

            [DataMember(EmitDefaultValue = false)]
            public int Test1 { get; set; } = 0;

            [DataMember(EmitDefaultValue = false)]
            public string Test2 { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public DataContractEnum? Test3 { get; set; }

            public int Test4 { get; set; } = 4;
        }

        [Fact]
        public void TestDataContract1() {
            var str = Serializer.SerializeToBytes(new DataContractModel1());
            Assert.True(str.SequenceEqual(new byte[] { 130, 161, 97, 8, 161, 98, 192 }));
        }

        [DataContract]
        public class DataContractModel1 {

            [DataMember(Name = "a", EmitDefaultValue = false)]
            public int Test1 { get; set; } = 8;

            [DataMember(Name = "b", EmitDefaultValue = false)]
            public string Test2 { get; set; } = null;
        }

        [Fact]
        public void TestDataContract2() {
            var str = Serializer.SerializeToBytes(new DataContractModel2());
            Assert.True(str.SequenceEqual(new byte[] { 146, 8, 192 }));
        }

        [DataContract]
        public class DataContractModel2 {

            [DataMember(Name = "a", Order = 0, EmitDefaultValue = false)]
            public int Test1 { get; set; } = 8;

            [DataMember(Name = "b", Order = 1, EmitDefaultValue = false)]
            public string Test2 { get; set; } = null;
        }

        [Fact]
        public void TestDataContractDefaultValuesAndVariantValueAsNull() {
            var str = Serializer.SerializeToBytes(new DataContractModelWithVariantNullValue {
                Test1 = 5,
                Test3 = DataContractEnum.All,
                Test4 = 8,
                TestStr = "T"
            });
            var result = Serializer.Deserialize<DataContractModelWithVariantNullValue>(str.ToArray());
            Assert.Equal(5, result.Test1);
            Assert.Equal(4, result.Test4);
            Assert.Equal("T", result.TestStr);
            Assert.Equal(DataContractEnum.All, result.Test3);
            Assert.True(str.SequenceEqual(new byte[] { 148, 5, 192, 161, 84, 7 }));
        }

        [DataContract]
        public class DataContractModelWithVariantNullValue {

            [DataMember(EmitDefaultValue = false, Order = 0)]
            public int Test1 { get; set; } = 4;

            [DataMember(EmitDefaultValue = false, Order = 1)]
            public VariantValue Test2 { get; set; }

            [DataMember(EmitDefaultValue = false, Order = 2)]
            public string TestStr { get; set; } = "Test1";

            [DataMember(EmitDefaultValue = false, Order = 3)]
            public DataContractEnum? Test3 { get; set; } = DataContractEnum.Test1;

            public int Test4 { get; set; } = 4;
        }

        [Fact]
        public void TestDataContractEnum1() {
            var str = Serializer.SerializeToBytes(DataContractEnum.Test1 | DataContractEnum.Test2).ToArray();
            Assert.True(str.SequenceEqual(new byte[] { 210, 0, 0, 0, 3 }));
            var result = Serializer.Deserialize<DataContractEnum>(str.ToArray());
            Assert.Equal(DataContractEnum.Test1 | DataContractEnum.Test2, result);
        }

        [Fact]
        public void TestDataContractEnum2() {
            var str = Serializer.SerializeToBytes(DataContractEnum.All).ToArray();
            Assert.True(str.SequenceEqual(new byte[] { 210, 0, 0, 0, 7 }));
            var result = Serializer.Deserialize<DataContractEnum>(str.ToArray());
            Assert.Equal(DataContractEnum.All, result);
        }

        [Flags]
        [DataContract]
        public enum DataContractEnum {
            [EnumMember(Value = "tst1")]
            Test1 = 1,
            [EnumMember]
            Test2 = 2,
            [EnumMember]
            Test3 = 4,
            [EnumMember]
            All = 7
        }

        [Fact]
        public void SerializerFromObjectContainerToContainerWithObject() {
            var expected = new TestContainer {
                Value = Serializer.FromObject(new {
                    Test = "Text",
                    Locale = "de"
                })
            };
            var tmp = Serializer.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetVariantValues))]
        public void SerializerFromObjectContainerToContainer(VariantValue v) {
            var expected = new TestContainer {
                Value = v
            };
            var tmp = Serializer.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerFromObjectContainerToContainerWithSerializedVariant(object o, Type type) {
            var t = type.MakeArrayType();
            var expected = new TestContainer {
                Value = Serializer.FromObject(o)
            };
            var tmp = Serializer.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
            Assert.NotNull(actual.Value);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerFromObjectContainerToContainerWithArray(object o, Type type) {
            var t = type.MakeArrayType();
            var expected = new TestContainer {
                Value = Serializer.FromArray(o, o, o)
            };
            var tmp = Serializer.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
            Assert.NotNull(actual.Value);
        }

        [Fact]
        public void SerializerFromObjectContainerToContainerWithStringArray() {
            var expected = new TestContainer {
                Value = Serializer.FromArray("", "", "")
            };
            var tmp = Serializer.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
            Assert.NotNull(actual.Value);
        }

        [DataContract]
        public class TestContainer {
            [DataMember]
            public VariantValue Value { get; set; }

            public override bool Equals(object obj) {
                if (obj is TestContainer c) {
                    if (VariantValue.DeepEquals(c.Value, Value)) {
                        return true;
                    }
                    return false;
                }
                return false;
            }

            public override int GetHashCode() {
                return -1937169414 + EqualityComparer<VariantValue>.Default.GetHashCode(Value);
            }
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent1() {
            var expected = Serializer.FromObject(new {
                Test = "Text",
                Locale = "de"
            });
            var actual = Serializer.FromObject(new {
                Locale = "de",
                Test = "Text"
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent2() {
            var expected = Serializer.FromObject(new {
                Test = 1,
                LoCale = "de"
            });
            var actual = Serializer.FromObject(new {
                Locale = "de",
                TeSt = 1
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromComplexObjectAndGetByPath() {
            var o = Serializer.FromObject(new {
                Test = 0,
                Path1 = new {
                    Test = 1,
                    a = new[] { 1, 2, 3, 4, 5 },
                    Path2 = new {
                        Test = 2,
                        a = new[] { 1, 2, 3, 4, 5 },
                        Path3 = new {
                            Test = 3,
                            a = new[] { 1, 2, 3, 4, 5 },
                            Path4 = new {
                                Test = 4,
                                a = new[] { 1, 2, 3, 4, 5 }
                            }
                        }
                    }
                },
                LoCale = "de"
            });
            VariantValue value;

            value = o.GetByPath("Path1.Test");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.Path2.Test");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.Path2.Path3.Test");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.Test");
            Assert.Equal(4, value);

            value = o.GetByPath("path1.Test");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.path2.Test");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.PAth2.PaTh3.TEST");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.test");
            Assert.Equal(4, value);

            value = o.GetByPath("Path1.a");
            Assert.True(value.IsListOfValues);
            Assert.Equal(5, value.Count);

            value = o.GetByPath("Path1.a[0]");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.path2.a[1]");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.PAth2.PaTh3.a[2]");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.a[3]");
            Assert.Equal(4, value);
        }


        [Fact]
        public void SerializeFromComplexObjectAndGetByPath1() {
            var o = Serializer.FromObject(new {
                Test = 0,
                Path1 = new {
                    Test = 1,
                    Path2 = new {
                        Test = 2,
                        Path3 = new {
                            Test = 3,
                            Path4 = new {
                                Test = 4,
                            }
                        }
                    }
                },
                LoCale = "de"
            });
            VariantValue value;

            value = o.GetByPath("Path1.Test");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.Path2.Test");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.Path2.Path3.Test");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.Test");
            Assert.Equal(4, value);
            value = o.GetByPath("Path1.Test", StringComparison.InvariantCulture);
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.Path2.Test", StringComparison.InvariantCulture);
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.Path2.Path3.Test", StringComparison.InvariantCulture);
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.Test", StringComparison.InvariantCulture);
            Assert.Equal(4, value);

            value = o.GetByPath("path1.Test");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.path2.Test");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.PAth2.PaTh3.TEST");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.Path4.test");
            Assert.Equal(4, value);

            value = o.GetByPath("path1.Test", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.path2.Test", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.PAth2.PaTh3.TEST", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.Path2.Path3.Path4.test", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
        }

        [Fact]
        public void SerializeFromComplexObjectAndGetByPath2() {
            var o = Serializer.FromObject(new {
                Test = 0,
                Path1 = new {
                    Test = 1,
                    a = new[] { 1, 2, 3, 4, 5 },
                    Path2 = new {
                        Test = 2,
                        a = new[] { 1, 2, 3, 4, 5 },
                        Path3 = new {
                            Test = 3,
                            a = new[] { 1, 2, 3, 4, 5 },
                            Path4 = new {
                                Test = 4,
                                a = new[] { 1, 2, 3, 4, 5 }
                            }
                        }
                    }
                },
                LoCale = "de"
            });
            VariantValue value;

            value = o.GetByPath("Path1.a");
            Assert.True(value.IsListOfValues);
            Assert.Equal(5, value.Count);

            value = o.GetByPath("Path1.A[0]");
            Assert.Equal(1, value);
            value = o.GetByPath("Path1.path2.a[1]");
            Assert.Equal(2, value);
            value = o.GetByPath("Path1.PAth2.PaTh3.a[2]");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.Path2.Path3.PATH4.a[3]");
            Assert.Equal(4, value);

            value = o.GetByPath("Path1.A[0]", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.path2.a[1]", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.PAth2.PaTh3.a[2]", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.Path2.Path3.PATH4.a[3]", StringComparison.InvariantCulture);
            Assert.True(value.IsNull());
        }

        [Fact]
        public void SerializeFromComplexObjectAndGetByPath3() {
            var o = Serializer.FromObject(new {
                Test = 0,
                Path1 = new {
                    a = new object[] {
                        new {
                            Test = 3,
                            a = new[] { 1, 2, 3, 4, 5 },
                            Path2 = new {
                                Test = 2,
                                a = new[] { 1, 2, 3, 4, 5 }
                            }
                        },
                        new {
                            Test = 3,
                            a = new[] { 1, 2, 3, 4, 5 },
                            Path3 = new {
                                Test = 3,
                                a = new[] { 1, 2, 3, 4, 5 }
                            }
                        },
                        new {
                            Test = 3,
                            a = new[] { 1, 2, 3, 4, 5 },
                            Path4 = new {
                                Test = 4,
                                a = new[] { 1, 2, 3, 4, 5 }
                            }
                        }
                    }
                },
                LoCale = "de"
            });
            VariantValue value;

            value = o.GetByPath("Path1.a");
            Assert.True(value.IsListOfValues);
            Assert.Equal(3, value.Count);

            value = o.GetByPath("Path1.a[0]");
            Assert.True(value.IsObject);

            value = o.GetByPath("Path1.a[1].Test");
            Assert.Equal(3, value);
            value = o.GetByPath("Path1.a[1].Path3.Test");
            Assert.Equal(3, value);

            value = o.GetByPath("Path1.a[2].Path4.a");
            Assert.True(value.IsListOfValues);
            Assert.Equal(5, value.Count);
            value = o.GetByPath("Path1.a[2].Path4.a[2]");
            Assert.Equal(3, value);

            value = o.GetByPath("Path1.a[4]");
            Assert.True(value.IsNull());
            value = o.GetByPath("Path1.a[4].Test");
            Assert.True(value.IsNull());
        }

        [Fact]
        public void SerializeEndpointString1() {
            var expected = "Endpoint";
            var json = Serializer.SerializeToString(expected);
            var actual = Serializer.Parse(json);
            VariantValue expected1 = "Endpoint";

            Assert.True(actual == expected);
            Assert.Equal(expected, actual);
            Assert.Equal(expected1, actual);
            Assert.Equal(expected, actual.ConvertTo<string>());
            Assert.Equal(expected, expected1.ConvertTo<string>());
        }

        [Fact]
        public void SerializeEndpointString2() {
            VariantValue expected = "Endpoint";
            var json = Serializer.SerializeToString(expected);
            var actual = Serializer.Parse(json);
            var expected1 = "Endpoint";

            Assert.True(actual.Equals(expected));
            Assert.Equal(expected, actual);
            Assert.Equal(expected1, actual);
            Assert.Equal(expected, actual.ConvertTo<string>());
        }

        [Fact]
        public void NullCompareTests() {
            VariantValue i1 = null;
            VariantValue i2 = null;
            VariantValue i3 = "test";
            VariantValue i4 = 0;
            VariantValue i5 = TimeSpan.FromSeconds(1);

            Assert.True(i1 is null);
            Assert.True(i1 == null);
            Assert.True(null == i1);
            Assert.True(i1 == i2);
            Assert.True(i1 != i3);
            Assert.True(i3 != i1);
            Assert.True(i1 != i4);
            Assert.True(i4 != i1);
            Assert.True(i4 != null);
            Assert.False(i4 == null);
            Assert.True(i3 != null);
            Assert.False(i3 == null);
            Assert.True(i5 != null);
            Assert.False(i5 == null);
        }

        [Fact]
        public void IntCompareTests() {
            VariantValue i1 = 1;
            VariantValue i2 = 2;
            VariantValue i3 = 2;

            Assert.True(i1 < i2);
            Assert.True(i1 <= i2);
            Assert.True(i2 > i1);
            Assert.True(i2 >= i1);
            Assert.True(i2 < 3);
            Assert.True(i2 <= 3);
            Assert.True(i2 <= 2);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= 2);
            Assert.True(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == 1);
            Assert.True(i2 == i3);
            Assert.True(i1 != 2);
            Assert.False(i2 == i1);
            Assert.False(i1 == 2);
        }

        [Fact]
        public void TimeSpanCompareTests() {
            VariantValue i1 = TimeSpan.FromSeconds(1);
            VariantValue i2 = TimeSpan.FromSeconds(2);
            VariantValue i3 = TimeSpan.FromSeconds(2);

            Assert.True(i1 < i2);
            Assert.True(i1 <= i2);
            Assert.True(i2 > i1);
            Assert.True(i2 >= i1);
            Assert.True(i2 < TimeSpan.FromSeconds(3));
            Assert.True(i2 <= TimeSpan.FromSeconds(3));
            Assert.True(i2 <= TimeSpan.FromSeconds(2));
            Assert.True(i2 <= i3);
            Assert.True(i2 >= TimeSpan.FromSeconds(2));
            Assert.True(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == TimeSpan.FromSeconds(1));
            Assert.True(i2 == i3);
            Assert.True(i1 != TimeSpan.FromSeconds(2));
            Assert.False(i2 == i1);
            Assert.False(i1 == TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void DateCompareTests() {
            VariantValue i1 = DateTime.MinValue;
            VariantValue i2 = DateTime.UtcNow;
            var i2a = i2.Copy();
            VariantValue i3 = DateTime.MaxValue;

            Assert.True(i1 < i2);
            Assert.True(i1 <= i2);
            Assert.True(i2 > i1);
            Assert.True(i2 >= i1);
            Assert.True(i2 < DateTime.MaxValue);
            Assert.True(i2 <= DateTime.MaxValue);
            Assert.True(i2 <= DateTime.UtcNow);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= i2a);
            Assert.True(i2 == i2a);
            Assert.True(i2 >= DateTime.MinValue);
            Assert.False(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == DateTime.MinValue);
            Assert.False(i2 == i3);
            Assert.True(i2 != i3);
            Assert.True(i1 != DateTime.UtcNow);
            Assert.False(i2 == i1);
            Assert.False(i1 == DateTime.UtcNow);
        }

        [Fact]
        public void FloatCompareTests() {
            VariantValue i1 = -0.123f;
            VariantValue i2 = 0.0f;
            VariantValue i2a = 0.0f;
            VariantValue i3 = 0.123f;

            Assert.True(i1 < i2);
            Assert.True(i1 <= i2);
            Assert.True(i2 > i1);
            Assert.True(i2 >= i1);
            Assert.True(i2 < 0.123f);
            Assert.True(i2 <= 0.123f);
            Assert.True(i2 <= 0.0f);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= i2a);
            Assert.True(i2 == i2a);
            Assert.True(i2 >= -0.123f);
            Assert.False(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == -0.123f);
            Assert.False(i2 == i3);
            Assert.True(i2 != i3);
            Assert.True(i1 != 0.0f);
            Assert.False(i2 == i1);
            Assert.False(i1 == 0.0f);
        }

        [Fact]
        public void DecimalCompareTests() {
            VariantValue i1 = -0.123m;
            VariantValue i2 = 0.0m;
            VariantValue i2a = 0.0m;
            VariantValue i3 = 0.123m;

            Assert.True(i1 < i2);
            Assert.True(i1 <= i2);
            Assert.True(i2 > i1);
            Assert.True(i2 >= i1);
            Assert.True(i2 < 0.123m);
            Assert.True(i2 < 0.123f);
            Assert.True(i2 <= 0.123m);
            Assert.True(i2 <= 0.123f);
            Assert.True(i2 <= 0.0m);
            Assert.True(i2 <= 0.0f);
            Assert.True(i2 <= 0.0);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= i2a);
            Assert.True(i2 == i2a);
            Assert.True(i2 >= -0.123m);
            Assert.False(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == -0.123m);
            Assert.True(i1 == -0.123f);
            Assert.False(i2 == i3);
            Assert.True(i2 != i3);
            Assert.True(i1 != 0.0m);
            Assert.True(i1 != 0.0f);
            Assert.False(i2 == i1);
            Assert.False(i1 == 0.0m);
        }

        [Fact]
        public void UlongCompareTests() {
            VariantValue i1 = 1ul;
            VariantValue i2 = 2ul;
            VariantValue i3 = 2ul;

            Assert.True(i1 < i2);
            Assert.True(i2 > i1);
            Assert.True(i2 < 3);
            Assert.True(i2 <= 2);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= 2);
            Assert.True(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 == 1);
            Assert.True(i1 >= 1);
            Assert.True(i1 <= 1);
            Assert.True(i2 == i3);
            Assert.True(i1 != 2);
            Assert.True(i1 <= 2);
            Assert.False(i2 == i1);
            Assert.False(i1 == 2);
        }

        [Fact]
        public void UlongAndIntGreaterThanTests() {
            VariantValue i1 = -1;
            VariantValue i2 = 2ul;
            VariantValue i3 = 2;

            Assert.True(i1 < i2);
            Assert.True(i2 > i1);
            Assert.True(i2 < 3);
            Assert.True(i2 <= 2);
            Assert.True(i2 >= 2);
            Assert.True(i2 <= i3);
            Assert.True(i2 >= i3);
            Assert.True(i2 != i1);
            Assert.True(i1 < 0);
            Assert.True(i1 <= 0);
            Assert.True(i1 == -1);
            Assert.True(i1 >= -1);
            Assert.True(i1 <= -1);
            Assert.True(i2 == i3);
            Assert.True(i1 != 2);
            Assert.False(i2 == i1);
            Assert.False(i1 == 2);
        }

        public static IEnumerable<object[]> GetNulls() {
            return GetStrings()
                .Select(v => new object[] { v.Item2.GetType() })
                .Concat(GetValues()
                .Where(v => v.Item2 != null)
                .Select(v => new object[] {
                    typeof(Nullable<>).MakeGenericType(v.Item2.GetType()) }));
        }

        public static IEnumerable<object[]> GetScalars() {
            return GetStrings()
                .Select(v => new object[] { v.Item2, v.Item2.GetType() })
                .Concat(GetValues()
                .Where(v => v.Item2 != null)
                .Select(v => new object[] { v.Item2, v.Item2.GetType() })
                .Concat(GetValues()
                .Where(v => v.Item2 != null)
                .Select(v => new object[] { v.Item2,
                    typeof(Nullable<>).MakeGenericType(v.Item2.GetType()) })));
        }

        public static IEnumerable<object[]> GetFilledArrays() {
            return GetStrings()
                .Select(v => new object[] { CreateArray(v.Item2, v.Item2.GetType(), 10),
                    v.Item2.GetType().MakeArrayType()})
                .Concat(GetValues()
                .Where(v => v.Item2 != null)
                .Select(v => new object[] { CreateArray(v.Item2, v.Item2.GetType(), 10),
                    v.Item2.GetType().MakeArrayType() }));
        }

        public static IEnumerable<object[]> GetEmptyArrays() {
            return GetStrings()
                .Select(v => new object[] { CreateArray(null, v.Item2.GetType(), 10),
                    v.Item2.GetType().MakeArrayType()})
                .Concat(GetValues()
                .Where(v => v.Item2 != null)
                .Select(v => new object[] { CreateArray(null, v.Item2.GetType(), 10),
                    v.Item2.GetType().MakeArrayType() }));
        }

        public static IEnumerable<object[]> GetVariantValues() {
            return GetValues()
                .Select(v => new object[] { v.Item1 })
                .Concat(GetStrings()
                .Select(v => new object[] { v.Item1 }));
        }

        public static IEnumerable<object[]> GetVariantValueAndValue() {
            return GetStrings()
                .Select(v => new object[] { v.Item1, v.Item2 })
                .Concat(GetValues()
                .Select(v => new object[] { v.Item1, v.Item2 }));
        }

        private static object CreateArray(object value, Type type, int size) {
            var array = Array.CreateInstance(type, size);
            if (value != null) {
                for (var i = 0; i < size; i++) {
                    array.SetValue(value, i);
                }
            }
            return array;
        }
    }
}
