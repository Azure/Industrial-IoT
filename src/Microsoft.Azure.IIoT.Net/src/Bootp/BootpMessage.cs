// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {
    using Microsoft.Azure.IIoT.Net;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Bootp message poco supporting serialization and
    /// deserialization to and from byte buffer.
    /// </summary>
    public class BootpMessage {

        //
        //   FIELD   BYTES   DESCRIPTION
        //   -----   -----   -----------
        //

        /// <summary>
        ///  op      1       packet op code / message type.
        ///                  1 = BOOTREQUEST, 2 = BOOTREPLY
        /// </summary>
        public BootpOpCode Op { get; set; } =
            BootpOpCode.BootRequest;

        /// <summary>
        ///  htype   1       hardware address type,
        ///                  see ARP section in "Assigned Numbers" RFC.
        ///                  '1' = 10mb ethernet
        /// </summary>
        public HardwareType Htype { get; set; } =
            HardwareType.Ethernet;

        /// <summary>
        ///  hlen    1       hardware address length
        ///                  (eg '6' for 10mb ethernet).
        /// </summary>
        public int Hlen {
            get => Chaddr?.GetAddressBytes().Length ?? _hlen;
            set => _hlen = value;
        }

        /// <summary>
        ///  hops    1       client sets to zero,
        ///                  optionally used by gateways
        ///                  in cross-gateway booting.
        /// </summary>
        public int Hops { get; set; }

        /// <summary>
        ///  xid     4       transaction ID, a random number,
        ///                  used to match this boot request with the
        ///                  responses it generates.
        /// </summary>
        public uint Xid { get; set; }

        /// <summary>
        ///  secs    2       filled in by client, seconds elapsed since
        ///                  client started trying to boot.
        /// </summary>
        public TimeSpan Secs { get; set; }

        /// <summary>
        ///  flags   2       Used by dhcp for broadcast indicator.
        /// </summary>
        public ushort Flags { get; set; }

        /// <summary>
        ///  ciaddr  4       client IP address;
        ///                  filled in by client in bootrequest if known.
        /// </summary>
        public IPAddress Ciaddr { get; set; }

        /// <summary>
        ///  yiaddr  4       'your' (client) IP address;
        ///                  filled by server if client doesn't
        ///                  know its own address (ciaddr was 0).
        /// </summary>
        public IPAddress Yiaddr { get; set; }

        /// <summary>
        ///  siaddr  4       server IP address;
        ///                  returned in bootreply by server.
        /// </summary>
        public IPAddress Siaddr { get; set; }

        /// <summary>
        ///  giaddr  4       gateway IP address,
        ///                  used in optional cross-gateway booting.
        /// </summary>
        public IPAddress Giaddr { get; set; }

        /// <summary>
        ///  chaddr  16      client hardware address,
        ///                  filled in by client.
        /// </summary>
        public PhysicalAddress Chaddr { get; set; }

        /// <summary>
        ///  sname   64      optional server host name,
        ///                  null terminated string.
        /// </summary>
        public string Sname {
            get => _sname.ToEncodedString();
            set => _sname = value.ToBytes();
        }

        /// <summary>
        ///  file    128     boot file name, null terminated string;
        ///                  'generic' name or null in bootrequest,
        ///                  fully qualified directory-path
        ///                  name in bootreply.
        /// </summary>
        public string File {
            get => _file.ToEncodedString();
            set => _file = value.ToBytes();
        }


        /// <summary>
        /// Create message from packet
        /// </summary>
        /// <param name="packet"></param>
        public static BootpMessage Parse(byte[] packet, int offset, int len) {
            var msg = new BootpMessage();
            msg.Read(packet, ref offset);
            if (offset > len) {
                throw new IndexOutOfRangeException(nameof(packet));
            }
            return msg;
        }

        /// <summary>
        /// Encode message as bytes to send
        /// </summary>
        /// <returns></returns>
        public byte[] AsPacket() {
            using (var stream = new MemoryStream()) {
                Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Stringify as json
        /// </summary>
        /// <returns></returns>
        public override string ToString() => JsonConvertEx.SerializeObject(this);

        /// <summary>
        /// Encode bootp packet as bytes to send
        /// </summary>
        /// <returns></returns>
        protected virtual void Write(Stream buffer) {
            buffer.Write((byte)Op);
            buffer.Write((byte)Htype);
            buffer.Write((byte)Hlen);
            buffer.Write((byte)Hops);
            buffer.Write(Xid);
            buffer.WriteUInt16Seconds(Secs);
            buffer.Write(Flags, true);
            buffer.Write(Ciaddr);
            buffer.Write(Yiaddr);
            buffer.Write(Siaddr);
            buffer.Write(Giaddr);
            buffer.Write(Chaddr, 16);
            buffer.Write(_sname, 64);
            buffer.Write(_file, 128);
        }

        /// <summary>
        /// Read bootp packet (see RFC 951)
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void Read(byte[] packet, ref int offset) {
            Op = (BootpOpCode)packet.ReadUInt8(ref offset);
            Htype = (HardwareType)packet.ReadUInt8(ref offset);
            Hlen = packet.ReadUInt8(ref offset);
            Hops = packet.ReadUInt8(ref offset);
            Xid = packet.ReadUInt32(ref offset);
            Secs = packet.ReadUInt16Seconds(ref offset);
            Flags = packet.ReadUInt16(ref offset, true);
            Ciaddr = packet.ReadAddress(ref offset);
            Yiaddr = packet.ReadAddress(ref offset);
            Siaddr = packet.ReadAddress(ref offset);
            Giaddr = packet.ReadAddress(ref offset);
            Chaddr = packet.ReadPhysical(ref offset, Hlen, 16);
            _sname = packet.Read(ref offset, 64);
            _file = packet.Read(ref offset, 128);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is BootpMessage message &&
                Op == message.Op && Htype == message.Htype &&
                Hlen == message.Hlen && Hops == message.Hops &&
                Xid == message.Xid && Flags == message.Flags &&
                EqualityComparer<TimeSpan>.Default.Equals(Secs,
                    message.Secs) &&
                EqualityComparer<IPAddress>.Default.Equals(Ciaddr,
                    message.Ciaddr) &&
                EqualityComparer<IPAddress>.Default.Equals(Yiaddr,
                    message.Yiaddr) &&
                EqualityComparer<IPAddress>.Default.Equals(Siaddr,
                    message.Siaddr) &&
                EqualityComparer<IPAddress>.Default.Equals(Giaddr,
                    message.Giaddr) &&
                EqualityComparer<PhysicalAddress>.Default.Equals(Chaddr,
                    message.Chaddr) &&
                Sname.Equals(message.Sname) &&
                File.Equals(message.File);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = -463507744;
            hashCode = hashCode * -1521134295 +
                Op.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Htype.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Hlen.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Hops.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Xid.GetHashCode();
            hashCode = hashCode * -1521134295 +
                Flags.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<TimeSpan>.Default.GetHashCode(Secs);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<IPAddress>.Default.GetHashCode(Ciaddr);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<IPAddress>.Default.GetHashCode(Yiaddr);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<IPAddress>.Default.GetHashCode(Siaddr);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<IPAddress>.Default.GetHashCode(Giaddr);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<PhysicalAddress>.Default.GetHashCode(Chaddr);
            return hashCode;
        }

        protected int _hlen;
        protected byte[] _sname;
        protected byte[] _file;
    }
}