// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.MessagePack {
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Xunit;
    using System.Runtime.Serialization;

    public class MessagePackInteropTests {

        public ISerializer Json => new NewtonSoftJsonSerializer();
        public ISerializer MsgPack => new MessagePackSerializer();

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
            yield return (11790719998462990154, 11790719998462990154);
            yield return (10042278942021613161, 10042278942021613161);
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
        public void SerializerArrayVariant(object o, Type type) {
            var t = type.MakeArrayType();
            var expected = MsgPack.FromArray(o, o, o);
            var actual = Json.Parse(Json.SerializeToString(expected));
            Assert.True(actual.IsArray);
            Assert.True(actual.Count == 3);
            Assert.Equal(expected.GetTypeCode(), actual.GetTypeCode());
            Assert.Equal(expected.Count, actual.Count);
            Assert.True(expected.Equals(actual));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializerByteArrayVariantToObject() {
            var expected = MsgPack.FromArray((byte)1, (byte)2, (byte)3);
            var o1 = expected.ConvertTo<byte[]>();
            var json = Json.SerializeToString(o1);
            var actual = Json.Parse(json);

            Assert.True(actual.IsBytes);
            Assert.True(actual.Count == 3);
            Assert.True(expected.IsArray);
            Assert.Equal(expected.Count, actual.Count);

            Assert.True(expected.Equals(actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerArrayVariantToObject(object o, Type type) {
            var t = type.MakeArrayType();
            var expected = MsgPack.FromArray(o, o, o);
            var o1 = expected.ConvertTo(type.MakeArrayType());
            var json = Json.SerializeToString(o1);
            var actual = Json.Parse(json);

            Assert.True(actual.IsArray);
            Assert.True(actual.Count == 3);
            Assert.Equal(expected.GetTypeCode(), actual.GetTypeCode());
            Assert.Equal(expected.Count, actual.Count);
            if (!actual.Equals(expected)) {
                Console.WriteLine("");
            }
            if (0 != actual.CompareTo(expected)) {
                Console.WriteLine("");
            }
            Assert.True(expected.Equals(actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetScalars))]
        [MemberData(nameof(GetEmptyArrays))]
        [MemberData(nameof(GetFilledArrays))]
        public void SerializerVariant(object o, Type type) {
            var expected = MsgPack.FromObject(o).ConvertTo(type);
            var actual = Json.FromObject(o).ConvertTo(type);
            Assert.NotNull(expected);
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
            Assert.Equal(o, actual);
            Assert.Equal(o.GetType(), actual.GetType());
            Assert.Equal(o.GetType(), expected.GetType());
        }

        [Theory]
        [MemberData(nameof(GetVariantValues))]
        public void SerializerFromObject(VariantValue v) {
            var expected = MsgPack.FromObject(v);
            var actual = Json.FromObject(v);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent0() {
            var expected = MsgPack.FromObject(new {
                Test = "Text",
                Locale = "de"
            });
            var actual = Json.FromObject(new {
                Test = "Text",
                Locale = "de"
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent1() {
            var expected = MsgPack.FromObject(new {
                Test = "Text",
                Locale = "de"
            });
            var actual = Json.FromObject(new {
                Locale = "de",
                Test = "Text"
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent2() {
            var dt = DateTime.UtcNow;
            var expected = MsgPack.FromObject(new {
                Test = 1,
                LoCale = "de",
                TimeStamp = dt
            });
            var actual = Json.FromObject(new {
                TimeStamp = dt,
                Locale = "de",
                TeSt = 1
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializeFromObjectsWithSameContent3() {
            var dt = DateTime.UtcNow;
            var expected = MsgPack.FromObject(new {
                Test = 1,
                LoCale = "de",
                Inner = new {
                    TimeStamp = dt,
                    Test = 1
                }
            });
            var actual = Json.FromObject(new {
                Locale = "de",
                Inner = new {
                    TimeStamp = dt,
                    Test = 1
                },
                TeSt = 1
            });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SerializerFromObjectContainerToContainerWithObject() {
            var expected = new TestContainer {
                Value = MsgPack.FromObject(new {
                    Test = "Text",
                    Locale = "de"
                })
            };
            var tmp = Json.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetVariantValues))]
        public void SerializerFromObjectContainerToContainer(object v) {
            var expected = new TestContainer {
                Value = MsgPack.FromObject(v)
            };
            var tmp = Json.FromObject(expected);
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
                Value = MsgPack.FromObject(o)
            };
            var tmp = Json.FromObject(expected);
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
                Value = MsgPack.FromArray(o, o, o)
            };
            var tmp = Json.FromObject(expected);
            var actual = tmp.ConvertTo<TestContainer>();
            Assert.Equal(expected, actual);
            Assert.NotNull(actual.Value);
        }

        [Fact]
        public void SerializerFromObjectContainerToContainerWithStringArray() {
            var expected = new TestContainer {
                Value = MsgPack.FromArray("", "", "")
            };
            var tmp = Json.FromObject(expected);
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
                    return VariantValue.DeepEquals(c.Value, Value);
                }
                return false;
            }

            public override int GetHashCode() {
                return -1937169414 + EqualityComparer<VariantValue>.Default.GetHashCode(Value);
            }
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
