// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {
    using Microsoft.Azure.IIoT.Net.Bootp;
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using Newtonsoft.Json;

    /// <summary>
    /// Dhcp message poco supporting serialization and deserialization
    /// to and from byte buffer.
    /// </summary>
    public class DhcpMessage : BootpMessage {

        /// <summary>
        /// Options
        /// </summary>
        public Dictionary<DhcpOption, byte[]> Options { get; } =
            new Dictionary<DhcpOption, byte[]>();

        /// <summary>
        /// Options Accessor
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte[] this[DhcpOption index] {
            get => Options.ContainsKey(index) ? Options[index] : null;
            set {
                if (index != DhcpOption.Pad && index != DhcpOption.End) {
                    Options.AddOrUpdate(index, value);
                }
            }
        }

        /// <summary>
        /// Get Client identifier from message
        /// </summary>
        internal PhysicalAddress ClientIdentifier {
            get {
                var address = this[DhcpOption.ClientIdentifier] != null ?
                    new PhysicalAddress(this[DhcpOption.ClientIdentifier]) : Chaddr;
                if ((address?.GetAddressBytes().Length ?? 0) == 0) {
                    return null;
                }
                return address;
            }
        }

        /// <summary>
        /// Get server identifier from message
        /// </summary>
        internal IPAddress ServerIdentifier {
            get {
                var address = Siaddr;
                var id = this[DhcpOption.ServerIdentifier];
                if (id != null && id.Length == 4) {
                    address = new IPAddress(id);
                }
                if (IPAddress.Any.Equals(address)) {
                    return null;
                }
                return address;
            }
            set => this[DhcpOption.ServerIdentifier] = value.ToBytes();
        }

        /// <summary>
        /// Helper to set or get type
        /// </summary>
        internal DhcpMessageType? MessageType {
            set => this[DhcpOption.DhcpMessageType] = new byte[] {
                (byte)value.Value
            };
            get {
                var messageTypeData = this[DhcpOption.DhcpMessageType];
                if (messageTypeData != null && messageTypeData.Length == 1) {
                    return (DhcpMessageType)messageTypeData[0];
                }
                return null;
            }
        }

        /// <summary>
        /// Host Name
        /// </summary>
        internal string HostName {
            get => this[DhcpOption.HostName].ToEncodedString();
            set => this[DhcpOption.HostName] = value.ToBytes();
        }

        /// <summary>
        /// Max message size
        /// </summary>
        internal ushort MaximumDHCPMessageSize {
            get => this[DhcpOption.MaximumDHCPMessageSize].ToUInt16(true);
            set => this[DhcpOption.MaximumDHCPMessageSize] = value.ToBytes(true);
        }

        /// <summary>
        /// Defined list of requested options
        /// </summary>
        internal byte[] ParameterRequestList {
            get => this[DhcpOption.ParameterRequestList];
            set => this[DhcpOption.ParameterRequestList] = value;
        }

        /// <summary>
        /// Lease time
        /// </summary>
        internal TimeSpan? IPAddressLeaseTime {
            get => this[DhcpOption.IPAddressLeaseTime].ToSecondsDuration();
            set => this[DhcpOption.IPAddressLeaseTime] = value.ToBytes();
        }

        /// <summary>
        /// Request ip address
        /// </summary>
        internal IPAddress RequestedIPAddress {
            get => this[DhcpOption.RequestedIPAddress].ToIPAddress(Ciaddr);
            set => this[DhcpOption.RequestedIPAddress] = value.ToBytes();
        }

        /// <summary>
        /// Subnet mask
        /// </summary>
        internal IPAddress SubnetMask {
            get => this[DhcpOption.SubnetMask].ToIPAddress();
            set => this[DhcpOption.SubnetMask] = value.ToBytes();
        }

        /// <summary>
        /// Domain name
        /// </summary>
        internal string DomainName {
            get => this[DhcpOption.DomainName].ToEncodedString();
            set => this[DhcpOption.DomainName] = value.ToBytes();
        }

        /// <summary>
        /// Router
        /// </summary>
        internal IPAddress Router {
            get => this[DhcpOption.Router].ToIPAddress();
            set => this[DhcpOption.Router] = value.ToBytes();
        }

        /// <summary>
        /// Error message to send
        /// </summary>
        internal string Message {
            get => this[DhcpOption.Message].ToEncodedString();
            set => this[DhcpOption.Message] = value.ToBytes();
        }

        /// <summary>
        /// Domain name servers to use
        /// </summary>
        internal List<IPAddress> DomainNameServers {
            get => this[DhcpOption.DomainNameServers].ToIPAddresses();
            set => this[DhcpOption.DomainNameServers] = value.ToBytes();
        }

        /// <summary>
        /// Create message from packet
        /// </summary>
        /// <param name="packet"></param>
        public static new DhcpMessage Parse(byte[] packet, int offset, int len) {
            var msg = new DhcpMessage();
            msg.Read(packet, ref offset);
            if (offset > len) {
                throw new IndexOutOfRangeException(nameof(packet));
            }
            return msg;
        }

        /// <summary>
        /// Read packet
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="offset"></param>
        protected override void Read(byte[] packet, ref int offset) {
            base.Read(packet, ref offset);

            if (offset + 4 >= packet.Length) {
                // No options, done.
                return;
            }
            var magic = packet.ReadUInt32(ref offset);
            if (magic != kDhcpOptionsMagicNumber &&
                magic != kDhcpOptionsMagicNumberEx) {
                return;
            }
            while (offset < packet.Length) {
                var optionData = packet.ReadOption(out var option, ref offset);
                if (option == DhcpOption.End) {
                    // Done
                    break;
                }
                if (optionData != null) {
                    Options.Add(option, optionData);
                }
            }
        }

        /// <summary>
        /// Encode message  to send
        /// </summary>
        /// <returns></returns>
        protected override void Write(Stream buffer) {
            base.Write(buffer);
            if (Options.Count == 0) {
                return;
            }
            buffer.Write(kDhcpOptionsMagicNumber);
            if (ParameterRequestList != null) {
                // Write from ordered query first
                foreach (var optionId in ParameterRequestList) {
                    var option = (DhcpOption)optionId;
                    if (Options.ContainsKey(option)) {
                        buffer.Write(option, Options[option]);
                    }
                }
                // Write only what is not already written
                foreach (var option in Options.Keys) {
                    if (Array.IndexOf(ParameterRequestList, (byte)option) == -1) {
                        buffer.Write(option, Options[option]);
                    }
                }
            }
            else {
                foreach (var option in Options.Keys) {
                    buffer.Write(option, Options[option]);
                }
            }
            buffer.Write((byte)DhcpOption.End);
        }

        //
        // The first four octets of the 'options' field of the DHCP message
        // contain the (decimal) values 99, 130, 83 and 99, respectively
        // (this is the same magic cookie as is defined in RFC 1497).
        //
        private const uint kDhcpOptionsMagicNumber = 1669485411;
        private const uint kDhcpOptionsMagicNumberEx = 1666417251;

        /// <inheritdoc/>
        public override string ToString() => JsonConvertEx.SerializeObject(this);

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                if (obj is DhcpMessage message) {
                    if (Options.DictionaryEqualsSafe(message.Options,
                        (o1, o2) => o1.SequenceEqualsSafe(o2))) {
                        return true;
                    }
                }
                if (obj is BootpMessage) {
                    if (Options.Count == 0) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
    }
}