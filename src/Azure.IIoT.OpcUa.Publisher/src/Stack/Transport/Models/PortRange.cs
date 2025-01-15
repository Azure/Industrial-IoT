// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;

    /// <summary>
    /// A port range
    /// </summary>
    public sealed class PortRange
    {
        /// <summary>
        /// Number of ports in range
        /// </summary>
        public int Count => _upper - _lower + 1;

        /// <summary>
        /// Create port range
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public PortRange(int lower, int upper)
        {
            _lower = Math.Max(IPEndPoint.MinPort, Math.Min(lower, upper));
            _upper = Math.Min(IPEndPoint.MaxPort, Math.Max(lower, upper));
        }

        /// <summary>
        /// Create port range
        /// </summary>
        /// <param name="value"></param>
        public PortRange(int value) :
            this(value, value)
        {
        }

        /// <summary>
        /// Yield endpoints
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public IEnumerable<IPEndPoint> GetEndpoints(IPAddress address)
        {
            for (var port = _lower; port <= _upper; port++)
            {
                yield return new IPEndPoint(address, port);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not PortRange range)
            {
                return false;
            }
            return _lower == range._lower && _upper == range._upper;
        }

        /// <inheritdoc/>
        public static bool operator ==(PortRange range1, PortRange range2) =>
            EqualityComparer<PortRange>.Default.Equals(range1, range2);
        /// <inheritdoc/>
        public static bool operator !=(PortRange range1, PortRange range2) =>
            !(range1 == range2);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(_lower, _upper);
        }

        /// <summary>
        /// Tests contains value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(int value)
        {
            return value >= _lower && value <= _upper;
        }

        /// <summary>
        /// Whether it overlaps with another port range
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Overlaps(PortRange other)
        {
            return
                Contains(other._lower) ||
                Contains(other._upper) ||
                other.Contains(_lower) ||
                other.Contains(_upper);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        private readonly int _lower;
        private readonly int _upper;

        /// <summary>
        /// Parses a series of port ranges
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static bool TryParse(string value,
            [NotNullWhen(true)] out IEnumerable<PortRange>? ranges)
        {
            try
            {
                ranges = Parse(value);
                return true;
            }
            catch
            {
                ranges = null;
                return false;
            }
        }

        /// <summary>
        /// Format a series of address ranges
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static string Format(IEnumerable<PortRange> ranges)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var range in ranges)
            {
                if (!first)
                {
                    sb.Append(';');
                }
                first = false;
                range.AppendTo(sb);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse range
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<PortRange> Parse(string value)
        {
            var parsed = value.Split([';', ','],
                StringSplitOptions.RemoveEmptyEntries).Select(s =>
                {
                    var x = s.Split('-');
                    if (x.Length > 2)
                    {
                        throw new FormatException("Bad range format");
                    }
                    var lows = x[0].Trim();
                    var highs = (x.Length == 2) ? x[1].Trim() : lows;

                    var lowsInt = IPEndPoint.MinPort;
                    var highsInt = IPEndPoint.MaxPort;

                    if (lows != "*")
                    {
                        lowsInt = int.Parse(lows, CultureInfo.InvariantCulture);
                    }
                    if (highs != "*")
                    {
                        highsInt = int.Parse(highs, CultureInfo.InvariantCulture);
                    }

                    if (lowsInt < IPEndPoint.MinPort ||
                        highsInt > IPEndPoint.MaxPort ||
                        lowsInt > highsInt)
                    {
                        throw new ArgumentException("Port numbers are out of the range", nameof(value));
                    }

                    return new PortRange(lowsInt, highsInt);
                });
            return Merge(parsed);
        }

        /// <summary>
        /// Opc ua ports
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PortRange> OpcUa
        {
            get
            {
                yield return new PortRange(4840, 4841);
            }
        }

        /// <summary>
        /// Well known opc ua ports
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PortRange> WellKnown
        {
            get
            {
                yield return new PortRange(4840, 4841);
                yield return new PortRange(48000, 48100);
                yield return new PortRange(49320);
                yield return new PortRange(50000);
                yield return new PortRange(51200, 51300);
                yield return new PortRange(62222);

                // ... add more ports to well known range here
            }
        }

        /// <summary>
        /// All possible ports
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PortRange> All
        {
            get
            {
                yield return new PortRange(IPEndPoint.MinPort, IPEndPoint.MaxPort);
            }
        }

        /// <summary>
        /// All IANA unassigned ports
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PortRange> Unassigned
        {
            get
            {
                yield return new PortRange(4);
                yield return new PortRange(6);
                yield return new PortRange(8);
                yield return new PortRange(10);
                yield return new PortRange(12);
                yield return new PortRange(14);
                yield return new PortRange(15);
                yield return new PortRange(16);
                yield return new PortRange(26);
                yield return new PortRange(28);
                yield return new PortRange(30);
                yield return new PortRange(32);
                yield return new PortRange(34);
                yield return new PortRange(36);
                yield return new PortRange(40);
                yield return new PortRange(60);
                yield return new PortRange(81);
                yield return new PortRange(100);
                yield return new PortRange(114);
                yield return new PortRange(258);
                yield return new PortRange(272, 279);
                yield return new PortRange(285);
                yield return new PortRange(288, 307);
                yield return new PortRange(325, 332);
                yield return new PortRange(334, 343);
                yield return new PortRange(703);
                yield return new PortRange(708);
                yield return new PortRange(717, 728);
                yield return new PortRange(732, 740);
                yield return new PortRange(743);
                yield return new PortRange(745, 746);
                yield return new PortRange(755, 757);
                yield return new PortRange(766);
                yield return new PortRange(768);
                yield return new PortRange(778, 779);
                yield return new PortRange(781, 785);
                yield return new PortRange(786);
                yield return new PortRange(787);
                yield return new PortRange(788, 799);
                yield return new PortRange(803, 809);
                yield return new PortRange(811, 827);
                yield return new PortRange(834, 846);
                yield return new PortRange(849, 852);
                yield return new PortRange(855, 859);
                yield return new PortRange(863, 872);
                yield return new PortRange(874, 885);
                yield return new PortRange(889, 899);
                yield return new PortRange(904, 909);
                yield return new PortRange(914, 952);
                yield return new PortRange(954, 988);
                yield return new PortRange(1002, 1007);
                yield return new PortRange(1009);
                yield return new PortRange(1491);
                yield return new PortRange(1895);
                yield return new PortRange(1895);
                yield return new PortRange(2194, 2196);
                yield return new PortRange(2259);
                yield return new PortRange(2369);
                yield return new PortRange(2378);
                yield return new PortRange(2693);
                yield return new PortRange(2693);
                yield return new PortRange(2794);
                yield return new PortRange(2825);
                yield return new PortRange(2873);
                yield return new PortRange(2925);
                yield return new PortRange(2999);
                yield return new PortRange(2999);
                yield return new PortRange(3092);
                yield return new PortRange(3126);
                yield return new PortRange(3301);
                yield return new PortRange(3546);
                yield return new PortRange(3694);
                yield return new PortRange(3994);
                yield return new PortRange(4048);
                yield return new PortRange(4144);
                yield return new PortRange(4194, 4196);
                yield return new PortRange(4198);
                yield return new PortRange(4315);
                yield return new PortRange(4317, 4319);
                yield return new PortRange(4332);
                yield return new PortRange(4337, 4339);
                yield return new PortRange(4363, 4365);
                yield return new PortRange(4367);
                yield return new PortRange(4380, 4388);
                yield return new PortRange(4397, 4399);
                yield return new PortRange(4424);
                yield return new PortRange(4434, 4440);
                yield return new PortRange(4459, 4483);
                yield return new PortRange(4489, 4499);
                yield return new PortRange(4501);
                yield return new PortRange(4503, 4533);
                yield return new PortRange(4539, 4544);
                yield return new PortRange(4560, 4562);
                yield return new PortRange(4564, 4565);
                yield return new PortRange(4571, 4572);
                yield return new PortRange(4574, 4589);
                yield return new PortRange(4606, 4620);
                yield return new PortRange(4622, 4657);
                yield return new PortRange(4693, 4699);
                yield return new PortRange(4705, 4710);
                yield return new PortRange(4712, 4724);
                yield return new PortRange(4734, 4736);
                yield return new PortRange(4748, 4748);
                yield return new PortRange(4757, 4773);
                yield return new PortRange(4775, 4783);
                yield return new PortRange(4792, 4799);
                yield return new PortRange(4805, 4826);
                yield return new PortRange(4828, 4836);
                yield return new PortRange(4852, 4866);
                yield return new PortRange(4872, 4875);
                yield return new PortRange(4886, 4893);
                yield return new PortRange(4895, 4898);
                yield return new PortRange(4903, 4911);
                yield return new PortRange(4916, 4935);
                yield return new PortRange(4938, 4939);
                yield return new PortRange(4943, 4948);
                yield return new PortRange(4954, 4968);
                yield return new PortRange(4972, 4979);
                yield return new PortRange(4981, 4982);
                yield return new PortRange(4983);
                yield return new PortRange(4992, 4998);
                yield return new PortRange(5016, 5019);
                yield return new PortRange(5035, 5041);
                yield return new PortRange(5076, 5077);
                yield return new PortRange(5088, 5089);
                yield return new PortRange(5095, 5098);
                yield return new PortRange(5108, 5110);
                yield return new PortRange(5113);
                yield return new PortRange(5118, 5119);
                yield return new PortRange(5121, 5132);
                yield return new PortRange(5138, 5144);
                yield return new PortRange(5147, 5149);
                yield return new PortRange(5158, 5160);
                yield return new PortRange(5169, 5171);
                yield return new PortRange(5173, 5189);
                yield return new PortRange(5198, 5199);
                yield return new PortRange(5204, 5208);
                yield return new PortRange(5210, 5214);
                yield return new PortRange(5216, 5220);
                yield return new PortRange(5238, 5244);
                yield return new PortRange(5255, 5263);
                yield return new PortRange(5266, 5268);
                yield return new PortRange(5273, 5279);
                yield return new PortRange(5283, 5297);
                yield return new PortRange(5311);
                yield return new PortRange(5316);
                yield return new PortRange(5319);
                yield return new PortRange(5322, 5342);
                yield return new PortRange(5345, 5348);
                yield return new PortRange(5365, 5396);
                yield return new PortRange(5438, 5442);
                yield return new PortRange(5444);
                yield return new PortRange(5446, 5449);
                yield return new PortRange(5451, 5452);
                yield return new PortRange(5457, 5460);
                yield return new PortRange(5466, 5469);
                yield return new PortRange(5476, 5499);
                yield return new PortRange(5508, 5549);
                yield return new PortRange(5551, 5552);
                yield return new PortRange(5558, 5564);
                yield return new PortRange(5570, 5572);
                yield return new PortRange(5576, 5578);
                yield return new PortRange(5587, 5596);
                yield return new PortRange(5606, 5617);
                yield return new PortRange(5619, 5626);
                yield return new PortRange(5640, 5645);
                yield return new PortRange(5647, 5665);
                yield return new PortRange(5667, 5669);
                yield return new PortRange(5685, 5686);
                yield return new PortRange(5690, 5692);
                yield return new PortRange(5694, 5695);
                yield return new PortRange(5697, 5699);
                yield return new PortRange(5701, 5704);
                yield return new PortRange(5706, 5712);
                yield return new PortRange(5731, 5740);
                yield return new PortRange(5749);
                yield return new PortRange(5751, 5754);
                yield return new PortRange(5756);
                yield return new PortRange(5758, 5765);
                yield return new PortRange(5772, 5776);
                yield return new PortRange(5778, 5779);
                yield return new PortRange(5788, 5792);
                yield return new PortRange(5795, 5812);
                yield return new PortRange(5815, 5840);
                yield return new PortRange(5843, 5858);
                yield return new PortRange(5860, 5862);
                yield return new PortRange(5864, 5867);
                yield return new PortRange(5869, 5882);
                yield return new PortRange(5884, 5899);
                yield return new PortRange(5901, 5909);
                yield return new PortRange(5914, 5962);
                yield return new PortRange(5964, 5967);
                yield return new PortRange(5970, 5983);
                yield return new PortRange(5994, 5998);
                yield return new PortRange(6067);
                yield return new PortRange(6078, 6079);
                yield return new PortRange(6089, 6098);
                yield return new PortRange(6119, 6120);
                yield return new PortRange(6125, 6129);
                yield return new PortRange(6131, 6132);
                yield return new PortRange(6134, 6139);
                yield return new PortRange(6150, 6158);
                yield return new PortRange(6164, 6199);
                yield return new PortRange(6202, 6208);
                yield return new PortRange(6210, 6221);
                yield return new PortRange(6223, 6240);
                yield return new PortRange(6245, 6250);
                yield return new PortRange(6254, 6266);
                yield return new PortRange(6270, 6299);
                yield return new PortRange(6302, 6305);
                yield return new PortRange(6307, 6314);
                yield return new PortRange(6318, 6319);
                yield return new PortRange(6323);
                yield return new PortRange(6327, 6342);
                yield return new PortRange(6345, 6345);
                yield return new PortRange(6348, 6349);
                yield return new PortRange(6351, 6354);
                yield return new PortRange(6356, 6359);
                yield return new PortRange(6361, 6362);
                yield return new PortRange(6364, 6369);
                yield return new PortRange(6371, 6378);
                yield return new PortRange(6380, 6381);
                yield return new PortRange(6383, 6388);
                yield return new PortRange(6391, 6399);
                yield return new PortRange(6411, 6416);
                yield return new PortRange(6422, 6431);
                yield return new PortRange(6433, 6441);
                yield return new PortRange(6447, 6454);
                yield return new PortRange(6457, 6463);
                yield return new PortRange(6465, 6470);
                yield return new PortRange(6472, 6479);
                yield return new PortRange(6490, 6499);
                yield return new PortRange(6504);
                yield return new PortRange(6512, 6512);
                yield return new PortRange(6516, 6542);
                yield return new PortRange(6545, 6546);
                yield return new PortRange(6552, 6557);
                yield return new PortRange(6559, 6565);
                yield return new PortRange(6569, 6578);
                yield return new PortRange(6584, 6587);
                yield return new PortRange(6588);
                yield return new PortRange(6589, 6599);
                yield return new PortRange(6603, 6618);
                yield return new PortRange(6630);
                yield return new PortRange(6631);
                yield return new PortRange(6637, 6639);
                yield return new PortRange(6641, 6652);
                yield return new PortRange(6654);
                yield return new PortRange(6658, 6664);
                yield return new PortRange(6674, 6677);
                yield return new PortRange(6680, 6686);
                yield return new PortRange(6691, 6695);
                yield return new PortRange(6698, 6699);
                yield return new PortRange(6700);
                yield return new PortRange(6701);
                yield return new PortRange(6702);
                yield return new PortRange(6707, 6713);
                yield return new PortRange(6717, 6766);
                yield return new PortRange(6772, 6776);
                yield return new PortRange(6779, 6783);
                yield return new PortRange(6792, 6800);
                yield return new PortRange(6802, 6816);
                yield return new PortRange(6818, 6830);
                yield return new PortRange(6832, 6840);
                yield return new PortRange(6843, 6849);
                yield return new PortRange(6851, 6867);
                yield return new PortRange(6869, 6887);
                yield return new PortRange(6889);
                yield return new PortRange(6902, 6934);
                yield return new PortRange(6937, 6945);
                yield return new PortRange(6947, 6950);
                yield return new PortRange(6952, 6960);
                yield return new PortRange(6967, 6968);
                yield return new PortRange(6971, 6996);
                yield return new PortRange(7027, 7029);
                yield return new PortRange(7032, 7039);
                yield return new PortRange(7041, 7069);
                yield return new PortRange(7074, 7079);
                yield return new PortRange(7081, 7087);
                yield return new PortRange(7089, 7094);
                yield return new PortRange(7096, 7098);
                yield return new PortRange(7102, 7106);
                yield return new PortRange(7108, 7116);
                yield return new PortRange(7118, 7120);
                yield return new PortRange(7122, 7127);
                yield return new PortRange(7130, 7160);
                yield return new PortRange(7175, 7180);
                yield return new PortRange(7182, 7199);
                yield return new PortRange(7203, 7214);
                yield return new PortRange(7217, 7226);
                yield return new PortRange(7230, 7234);
                yield return new PortRange(7238, 7243);
                yield return new PortRange(7245, 7261);
                yield return new PortRange(7263, 7271);
                yield return new PortRange(7284, 7299);
                yield return new PortRange(7360, 7364);
                yield return new PortRange(7366, 7390);
                yield return new PortRange(7396);
                yield return new PortRange(7398, 7399);
                yield return new PortRange(7403, 7409);
                yield return new PortRange(7412, 7419);
                yield return new PortRange(7422, 7425);
                yield return new PortRange(7432, 7436);
                yield return new PortRange(7438, 7442);
                yield return new PortRange(7444, 7470);
                yield return new PortRange(7472);
                yield return new PortRange(7475, 7477);
                yield return new PortRange(7479, 7490);
                yield return new PortRange(7492, 7499);
                yield return new PortRange(7502, 7507);
                yield return new PortRange(7512, 7541);
                yield return new PortRange(7552, 7559);
                yield return new PortRange(7561, 7562);
                yield return new PortRange(7564, 7565);
                yield return new PortRange(7567, 7568);
                yield return new PortRange(7571, 7573);
                yield return new PortRange(7575, 7587);
                yield return new PortRange(7589, 7605);
                yield return new PortRange(7607, 7623);
                yield return new PortRange(7625);
                yield return new PortRange(7632);
                yield return new PortRange(7634, 7647);
                yield return new PortRange(7649, 7662);
                yield return new PortRange(7664, 7671);
                yield return new PortRange(7678, 7679);
                yield return new PortRange(7681, 7682);
                yield return new PortRange(7684, 7686);
                yield return new PortRange(7688);
                yield return new PortRange(7690, 7696);
                yield return new PortRange(7698, 7699);
                yield return new PortRange(7702, 7706);
                yield return new PortRange(7709, 7719);
                yield return new PortRange(7721, 7723);
                yield return new PortRange(7729, 7733);
                yield return new PortRange(7735, 7737);
                yield return new PortRange(7739, 7740);
                yield return new PortRange(7745, 7746);
                yield return new PortRange(7748, 7776);
                yield return new PortRange(7776);
                yield return new PortRange(7780);
                yield return new PortRange(7782, 7783);
                yield return new PortRange(7785);
                yield return new PortRange(7788);
                yield return new PortRange(7790, 7793);
                yield return new PortRange(7795, 7796);
                yield return new PortRange(7803, 7809);
                yield return new PortRange(7811, 7844);
                yield return new PortRange(7848, 7868);
                yield return new PortRange(7873, 7877);
                yield return new PortRange(7879);
                yield return new PortRange(7881, 7886);
                yield return new PortRange(7888, 7899);
                yield return new PortRange(7904, 7912);
                yield return new PortRange(7914, 7931);
                yield return new PortRange(7934, 7961);
                yield return new PortRange(7963, 7966);
                yield return new PortRange(7968, 7978);
                yield return new PortRange(7983, 7997);
                yield return new PortRange(8009, 8018);
                yield return new PortRange(8023, 8024);
                yield return new PortRange(8027, 8031);
                yield return new PortRange(8035, 8039);
                yield return new PortRange(8045, 8050);
                yield return new PortRange(8061, 8065);
                yield return new PortRange(8068, 8069);
                yield return new PortRange(8071, 8073);
                yield return new PortRange(8075, 8076);
                yield return new PortRange(8078, 8079);
                yield return new PortRange(8084, 8085);
                yield return new PortRange(8089);
                yield return new PortRange(8092, 8096);
                yield return new PortRange(8098, 8099);
                yield return new PortRange(8103, 8114);
                yield return new PortRange(8119, 8120);
                yield return new PortRange(8123, 8127);
                yield return new PortRange(8133, 8139);
                yield return new PortRange(8141, 8147);
                yield return new PortRange(8150, 8152);
                yield return new PortRange(8154, 8159);
                yield return new PortRange(8163, 8180);
                yield return new PortRange(8185, 8189);
                yield return new PortRange(8193);
                yield return new PortRange(8196, 8198);
                yield return new PortRange(8203, 8203);
                yield return new PortRange(8209, 8229);
                yield return new PortRange(8233, 8242);
                yield return new PortRange(8244, 8269);
                yield return new PortRange(8271, 8275);
                yield return new PortRange(8277, 8279);
                yield return new PortRange(8281);
                yield return new PortRange(8283, 8291);
                yield return new PortRange(8295, 8299);
                yield return new PortRange(8302, 8312);
                yield return new PortRange(8314, 8319);
                yield return new PortRange(8323, 8350);
                yield return new PortRange(8352, 8375);
                yield return new PortRange(8381, 8382);
                yield return new PortRange(8385, 8399);
                yield return new PortRange(8406, 8414);
                yield return new PortRange(8418, 8422);
                yield return new PortRange(8424, 8441);
                yield return new PortRange(8446, 8449);
                yield return new PortRange(8451, 8456);
                yield return new PortRange(8458, 8469);
                yield return new PortRange(8475, 8499);
                yield return new PortRange(8504, 8553);
                yield return new PortRange(8556, 8566);
                yield return new PortRange(8568, 8599);
                yield return new PortRange(8601, 8608);
                yield return new PortRange(8616, 8664);
                yield return new PortRange(8667, 8674);
                yield return new PortRange(8676, 8685);
                yield return new PortRange(8687);
                yield return new PortRange(8689, 8698);
                yield return new PortRange(8700, 8710);
                yield return new PortRange(8712, 8731);
                yield return new PortRange(8734, 8749);
                yield return new PortRange(8751, 8762);
                yield return new PortRange(8767, 8769);
                yield return new PortRange(8771, 8777);
                yield return new PortRange(8779, 8785);
                yield return new PortRange(8788, 8792);
                yield return new PortRange(8794, 8799);
                yield return new PortRange(8801, 8803);
                yield return new PortRange(8806, 8807);
                yield return new PortRange(8809, 8872);
                yield return new PortRange(8874, 8879);
                yield return new PortRange(8882);
                yield return new PortRange(8884, 8887);
                yield return new PortRange(8895, 8898);
                yield return new PortRange(8902, 8909);
                yield return new PortRange(8914, 8936);
                yield return new PortRange(8938, 8952);
                yield return new PortRange(8955, 8979);
                yield return new PortRange(8982, 8988);
                yield return new PortRange(8992, 8996);
                yield return new PortRange(9003, 9004);
                yield return new PortRange(9011, 9019);
                yield return new PortRange(9027, 9049);
                yield return new PortRange(9052, 9059);
                yield return new PortRange(9061, 9079);
                yield return new PortRange(9094, 9099);
                yield return new PortRange(9108, 9118);
                yield return new PortRange(9120, 9121);
                yield return new PortRange(9124, 9130);
                yield return new PortRange(9132, 9159);
                yield return new PortRange(9165, 9190);
                yield return new PortRange(9192, 9199);
                yield return new PortRange(9218, 9221);
                yield return new PortRange(9223, 9254);
                yield return new PortRange(9256, 9276);
                yield return new PortRange(9288, 9291);
                yield return new PortRange(9296, 9299);
                yield return new PortRange(9301, 9305);
                yield return new PortRange(9307, 9311);
                yield return new PortRange(9313, 9317);
                yield return new PortRange(9319, 9320);
                yield return new PortRange(9322, 9342);
                yield return new PortRange(9347, 9373);
                yield return new PortRange(9375, 9379);
                yield return new PortRange(9381, 9386);
                yield return new PortRange(9391, 9395);
                yield return new PortRange(9398, 9399);
                yield return new PortRange(9403, 9417);
                yield return new PortRange(9419, 9442);
                yield return new PortRange(9446, 9449);
                yield return new PortRange(9451, 9499);
                yield return new PortRange(9501, 9521);
                yield return new PortRange(9523, 9534);
                yield return new PortRange(9537, 9554);
                yield return new PortRange(9556, 9591);
                yield return new PortRange(9601, 9611);
                yield return new PortRange(9613);
                yield return new PortRange(9615);
                yield return new PortRange(9619, 9627);
                yield return new PortRange(9633, 9639);
                yield return new PortRange(9641, 9665);
                yield return new PortRange(9669, 9693);
                yield return new PortRange(9696, 9699);
                yield return new PortRange(9701, 9746);
                yield return new PortRange(9748, 9749);
                yield return new PortRange(9751, 9752);
                yield return new PortRange(9754, 9761);
                yield return new PortRange(9763, 9799);
                yield return new PortRange(9803, 9874);
                yield return new PortRange(9877);
                yield return new PortRange(9879, 9887);
                yield return new PortRange(9890, 9897);
                yield return new PortRange(9904, 9908);
                yield return new PortRange(9910);
                yield return new PortRange(9912, 9924);
                yield return new PortRange(9926, 9949);
                yield return new PortRange(9957, 9965);
                yield return new PortRange(9967, 9977);
                yield return new PortRange(9980);
                yield return new PortRange(9982, 9986);
                yield return new PortRange(9989, 9989);
                yield return new PortRange(10011, 10019);
                yield return new PortRange(10021, 10049);
                yield return new PortRange(10052, 10054);
                yield return new PortRange(10056, 10079);
                yield return new PortRange(10082, 10099);
                yield return new PortRange(10105, 10106);
                yield return new PortRange(10108, 10109);
                yield return new PortRange(10112);
                yield return new PortRange(10118, 10124);
                yield return new PortRange(10126, 10127);
                yield return new PortRange(10130, 10159);
                yield return new PortRange(10163, 10199);
                yield return new PortRange(10202, 10251);
                yield return new PortRange(10254, 10259);
                yield return new PortRange(10262, 10287);
                yield return new PortRange(10289, 10320);
                yield return new PortRange(10322, 10438);
                yield return new PortRange(10440, 10499);
                yield return new PortRange(10501, 10539);
                yield return new PortRange(10545, 10547);
                yield return new PortRange(10549, 10630);
                yield return new PortRange(10632, 10799);
                yield return new PortRange(10801, 10804);
                yield return new PortRange(10806, 10808);
                yield return new PortRange(10811, 10859);
                yield return new PortRange(10861, 10879);
                yield return new PortRange(10881, 10932);
                yield return new PortRange(10934, 10989);
                yield return new PortRange(10991, 10999);
                yield return new PortRange(11002, 11022);
                yield return new PortRange(11024, 11094);
                yield return new PortRange(11096, 11102);
                yield return new PortRange(11107);
                yield return new PortRange(11113, 11160);
                yield return new PortRange(11166, 11170);
                yield return new PortRange(11176, 11200);
                yield return new PortRange(11203, 11207);
                yield return new PortRange(11209, 11210);
                yield return new PortRange(11212, 11318);
                yield return new PortRange(11322, 11366);
                yield return new PortRange(11368, 11370);
                yield return new PortRange(11372, 11429);
                yield return new PortRange(11431, 11488);
                yield return new PortRange(11490, 11599);
                yield return new PortRange(11601, 11622);
                yield return new PortRange(11624, 11719);
                yield return new PortRange(11721, 11722);
                yield return new PortRange(11724, 11750);
                yield return new PortRange(11752, 11795);
                yield return new PortRange(11797, 11875);
                yield return new PortRange(11878, 11966);
                yield return new PortRange(11968, 11996);
                yield return new PortRange(12011);
                yield return new PortRange(12014, 12108);
                yield return new PortRange(12110, 12120);
                yield return new PortRange(12122, 12167);
                yield return new PortRange(12169, 12171);
                yield return new PortRange(12173, 12299);
                yield return new PortRange(12301);
                yield return new PortRange(12303, 12320);
                yield return new PortRange(12323, 12344);
                yield return new PortRange(12346, 12752);
                yield return new PortRange(12754, 12864);
                yield return new PortRange(12866, 13159);
                yield return new PortRange(13161, 13215);
                yield return new PortRange(13219, 13222);
                yield return new PortRange(13225, 13399);
                yield return new PortRange(13401, 13719);
                yield return new PortRange(13723);
                yield return new PortRange(13725, 13781);
                yield return new PortRange(13784);
                yield return new PortRange(13787, 13817);
                yield return new PortRange(13824, 13893);
                yield return new PortRange(13895, 13928);
                yield return new PortRange(13931, 13999);
                yield return new PortRange(14003, 14032);
                yield return new PortRange(14035, 14140);
                yield return new PortRange(14144);
                yield return new PortRange(14146, 14148);
                yield return new PortRange(14151, 14153);
                yield return new PortRange(14155, 14249);
                yield return new PortRange(14251, 14413);
                yield return new PortRange(14415, 14499);
                yield return new PortRange(14501, 14935);
                yield return new PortRange(14938, 14999);
                yield return new PortRange(15001);
                yield return new PortRange(15003, 15117);
                yield return new PortRange(15119, 15344);
                yield return new PortRange(15346, 15362);
                yield return new PortRange(15364, 15554);
                yield return new PortRange(15556, 15659);
                yield return new PortRange(15661, 15739);
                yield return new PortRange(15741, 15997);
                yield return new PortRange(16004, 16019);
                yield return new PortRange(16022, 16160);
                yield return new PortRange(16163, 16308);
                yield return new PortRange(16312, 16359);
                yield return new PortRange(16362, 16366);
                yield return new PortRange(16369, 16383);
                yield return new PortRange(16386, 16618);
                yield return new PortRange(16620, 16664);
                yield return new PortRange(16667, 16788);
                yield return new PortRange(16790, 16899);
                yield return new PortRange(16901, 16949);
                yield return new PortRange(16951, 16990);
                yield return new PortRange(16996, 17006);
                yield return new PortRange(17008, 17183);
                yield return new PortRange(17186, 17218);
                yield return new PortRange(17226, 17233);
                yield return new PortRange(17236, 17499);
                yield return new PortRange(17501, 17554);
                yield return new PortRange(17556, 17728);
                yield return new PortRange(17730, 17753);
                yield return new PortRange(17757, 17776);
                yield return new PortRange(17778, 17999);
                yield return new PortRange(18001, 18103);
                yield return new PortRange(18105, 18135);
                yield return new PortRange(18137, 18180);
                yield return new PortRange(18188, 18240);
                yield return new PortRange(18244, 18261);
                yield return new PortRange(18263, 18462);
                yield return new PortRange(18464, 18633);
                yield return new PortRange(18636, 18667);
                yield return new PortRange(18669, 18768);
                yield return new PortRange(18770, 18880);
                yield return new PortRange(18882, 18887);
                yield return new PortRange(18889, 18999);
                yield return new PortRange(19001, 19006);
                yield return new PortRange(19008, 19019);
                yield return new PortRange(19021, 19190);
                yield return new PortRange(19192, 19193);
                yield return new PortRange(19195, 19219);
                yield return new PortRange(19221, 19282);
                yield return new PortRange(19284, 19314);
                yield return new PortRange(19316, 19397);
                yield return new PortRange(19399, 19409);
                yield return new PortRange(19413, 19538);
                yield return new PortRange(19542, 19787);
                yield return new PortRange(19789, 19997);
                yield return new PortRange(20004);
                yield return new PortRange(20006, 20011);
                yield return new PortRange(20015, 20033);
                yield return new PortRange(20035, 20045);
                yield return new PortRange(20047, 20047);
                yield return new PortRange(20050, 20056);
                yield return new PortRange(20058, 20166);
                yield return new PortRange(20168, 20201);
                yield return new PortRange(20203, 20221);
                yield return new PortRange(20223, 20479);
                yield return new PortRange(20481, 20669);
                yield return new PortRange(20671, 20998);
                yield return new PortRange(21001, 21009);
                yield return new PortRange(21011, 21211);
                yield return new PortRange(21213, 21220);
                yield return new PortRange(21222, 21552);
                yield return new PortRange(21555, 21589);
                yield return new PortRange(21591, 21799);
                yield return new PortRange(21801, 21844);
                yield return new PortRange(21850, 21999);
                yield return new PortRange(22006, 22124);
                yield return new PortRange(22126, 22127);
                yield return new PortRange(22129, 22221);
                yield return new PortRange(22223, 22272);
                yield return new PortRange(22274, 22304);
                yield return new PortRange(22306, 22334);
                yield return new PortRange(22336, 22342);
                yield return new PortRange(22344, 22346);
                yield return new PortRange(22348, 22349);
                yield return new PortRange(22352, 22536);
                yield return new PortRange(22538, 22554);
                yield return new PortRange(22556, 22762);
                yield return new PortRange(22764, 22799);
                yield return new PortRange(22801, 22950);
                yield return new PortRange(22952, 22999);
                yield return new PortRange(23006, 23052);
                yield return new PortRange(23054, 23271);
                yield return new PortRange(23273, 23293);
                yield return new PortRange(23295, 23332);
                yield return new PortRange(23334, 23399);
                yield return new PortRange(23403, 23455);
                yield return new PortRange(23458, 23545);
                yield return new PortRange(23547, 23999);
                yield return new PortRange(24007, 24241);
                yield return new PortRange(24243, 24248);
                yield return new PortRange(24250, 24320);
                yield return new PortRange(24323, 24385);
                yield return new PortRange(24387, 24464);
                yield return new PortRange(24466, 24553);
                yield return new PortRange(24555, 24576);
                yield return new PortRange(24578, 24665);
                yield return new PortRange(24667, 24675);
                yield return new PortRange(24679);
                yield return new PortRange(24681, 24753);
                yield return new PortRange(24755, 24849);
                yield return new PortRange(24851, 24921);
                yield return new PortRange(24923, 24999);
                yield return new PortRange(25010, 25470);
                yield return new PortRange(25472, 25575);
                yield return new PortRange(25577, 25603);
                yield return new PortRange(25605, 25792);
                yield return new PortRange(25794, 25899);
                yield return new PortRange(25904, 25953);
                yield return new PortRange(25956, 25999);
                yield return new PortRange(26001, 26132);
                yield return new PortRange(26134, 26207);
                yield return new PortRange(26209, 26256);
                yield return new PortRange(26258, 26259);
                yield return new PortRange(26265, 26485);
                yield return new PortRange(26488);
                yield return new PortRange(26490, 26999);
                yield return new PortRange(27010, 27344);
                yield return new PortRange(27346, 27441);
                yield return new PortRange(27443, 27503);
                yield return new PortRange(27505, 27781);
                yield return new PortRange(27783, 27875);
                yield return new PortRange(27877, 27998);
                yield return new PortRange(28002, 28118);
                yield return new PortRange(28120, 28199);
                yield return new PortRange(28201, 28239);
                yield return new PortRange(28241, 28588);
                yield return new PortRange(28590, 29117);
                yield return new PortRange(29119, 29166);
                yield return new PortRange(29170, 29998);
                yield return new PortRange(30005, 30099);
                yield return new PortRange(30101, 30259);
                yield return new PortRange(30261, 30399);
                yield return new PortRange(30401, 30831);
                yield return new PortRange(30833, 30998);
                yield return new PortRange(31000, 31015);
                yield return new PortRange(31017, 31019);
                yield return new PortRange(31021, 31028);
                yield return new PortRange(31030, 31399);
                yield return new PortRange(31401, 31415);
                yield return new PortRange(31417, 31456);
                yield return new PortRange(31458, 31619);
                yield return new PortRange(31621, 31684);
                yield return new PortRange(31686, 31764);
                yield return new PortRange(31766, 31947);
                yield return new PortRange(31950, 32033);
                yield return new PortRange(32035, 32248);
                yield return new PortRange(32250, 32399);
                yield return new PortRange(32401, 32482);
                yield return new PortRange(32484, 32634);
                yield return new PortRange(32637, 32766);
                yield return new PortRange(32778, 32800);
                yield return new PortRange(32802, 32810);
                yield return new PortRange(32812, 32895);
                yield return new PortRange(32897, 33059);
                yield return new PortRange(33061, 33122);
                yield return new PortRange(33124, 33330);
                yield return new PortRange(33332);
                yield return new PortRange(33335, 33433);
                yield return new PortRange(33436, 33655);
                yield return new PortRange(33657, 34248);
                yield return new PortRange(34250, 34377);
                yield return new PortRange(34380, 34566);
                yield return new PortRange(34568, 34961);
                yield return new PortRange(34965, 34979);
                yield return new PortRange(34981, 34999);
                yield return new PortRange(35007, 35099);
                yield return new PortRange(35101, 35353);
                yield return new PortRange(35358, 36000);
                yield return new PortRange(36002, 36410);
                yield return new PortRange(36413, 36421);
                yield return new PortRange(36425, 36442);
                yield return new PortRange(36445, 36461);
                yield return new PortRange(36463, 36523);
                yield return new PortRange(36525, 36601);
                yield return new PortRange(36603, 36699);
                yield return new PortRange(36701, 36864);
                yield return new PortRange(36866, 37474);
                yield return new PortRange(37476, 37482);
                yield return new PortRange(37484, 37600);
                yield return new PortRange(37602, 37653);
                yield return new PortRange(37655, 37999);
                yield return new PortRange(38003, 38200);
                yield return new PortRange(38204, 38411);
                yield return new PortRange(38413, 38421);
                yield return new PortRange(38423, 38471);
                yield return new PortRange(38473, 38799);
                yield return new PortRange(38801, 38864);
                yield return new PortRange(38866, 39680);
                yield return new PortRange(39682, 39999);
                yield return new PortRange(40001, 40022);
                yield return new PortRange(40024, 40403);
                yield return new PortRange(40405, 40840);
                yield return new PortRange(40844, 40852);
                yield return new PortRange(40854, 41110);
                yield return new PortRange(41112, 41120);
                yield return new PortRange(41122, 41229);
                yield return new PortRange(41231, 41793);
                yield return new PortRange(41798, 42507);
                yield return new PortRange(42511, 42999);
                yield return new PortRange(43001, 43187);
                yield return new PortRange(43192, 43209);
                yield return new PortRange(43211, 43437);
                yield return new PortRange(43442, 44122);
                yield return new PortRange(43124, 44320);
                yield return new PortRange(44323);
                yield return new PortRange(44324, 44443);
                yield return new PortRange(44445, 44543);
                yield return new PortRange(44545, 44552);
                yield return new PortRange(44554, 44599);
                yield return new PortRange(44601, 44817);
                yield return new PortRange(44819, 44899);
                yield return new PortRange(44901, 44999);
                yield return new PortRange(45003, 45044);
                yield return new PortRange(45046, 45053);
                yield return new PortRange(45055, 45513);
                yield return new PortRange(45515, 45677);
                yield return new PortRange(45679, 45823);
                yield return new PortRange(45826, 45965);
                yield return new PortRange(45967, 46335);
                yield return new PortRange(46337, 46997);
                yield return new PortRange(47002, 47099);
                yield return new PortRange(47101, 47556);
                yield return new PortRange(47558, 47623);
                yield return new PortRange(47625, 47805);
                yield return new PortRange(47807);
                yield return new PortRange(47810, 47999);
                yield return new PortRange(48006, 48047);
                yield return new PortRange(48051, 48127);
                yield return new PortRange(48130, 48555);
                yield return new PortRange(48557, 48618);
                yield return new PortRange(48620, 48652);
                yield return new PortRange(48654, 48999);
                yield return new PortRange(49002, IPEndPoint.MaxPort);
            }
        }

        /// <summary>
        /// Append to builder
        /// </summary>
        /// <param name="sb"></param>
        private void AppendTo(StringBuilder sb)
        {
            if (IPEndPoint.MinPort == _lower)
            {
                if (_upper == IPEndPoint.MaxPort)
                {
                    sb.Append('*');
                    return;
                }
                sb.Append('0');
            }
            else
            {
                sb.Append(_lower);
            }
            if (_lower == _upper)
            {
                return;
            }
            sb.Append('-');
            if (IPEndPoint.MaxPort == _upper)
            {
                sb.Append('*');
            }
            else
            {
                sb.Append(_upper);
            }
        }

        /// <summary>
        /// Merge overlapping ranges
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        private static IEnumerable<PortRange> Merge(IEnumerable<PortRange> ranges)
        {
            var results = new Stack<PortRange>();
            if (ranges != null)
            {
                foreach (var range in ranges.OrderBy(k => k._lower))
                {
                    if (results.Count == 0)
                    {
                        results.Push(range);
                    }
                    else
                    {
                        var top = results.Peek();
                        if (top.Overlaps(range))
                        {
                            var union = new PortRange(
                                top._lower < range._lower ? top._lower : range._lower,
                                top._upper > range._upper ? top._upper : range._upper);
                            results.Pop();
                            results.Push(union);
                        }
                        else
                        {
                            results.Push(range);
                        }
                    }
                }
            }
            return results.Reverse();
        }
    }
}
