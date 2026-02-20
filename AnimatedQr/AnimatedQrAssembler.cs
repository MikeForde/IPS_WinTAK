using System;
using System.Collections.Generic;

namespace ipswintakplugin.AnimatedQr
{
    // Reassembles your “dual chunk packet” format:
    // [0]=total, [1]=i1, [2]=i2, [3]=secondStart, then payloads
    internal sealed class AnimatedQrAssembler
    {
        private readonly Dictionary<int, string> _chunks = new Dictionary<int, string>();
        private readonly HashSet<string> _seenPackets = new HashSet<string>();
        private int? _total;

        public int? Total => _total;
        public int ReceivedCount => _chunks.Count;
        public bool IsComplete => _total.HasValue && _chunks.Count == _total.Value;

        public bool TryAddPacket(string packet, out string reconstructed, out string info)
        {
            reconstructed = null;
            info = null;

            if (string.IsNullOrEmpty(packet) || packet.Length < 4) return false;

            // de-dupe identical QR contents (big speed win)
            if (_seenPackets.Contains(packet)) return false;
            _seenPackets.Add(packet);
            if (_seenPackets.Count > 5000) _seenPackets.Clear();

            int total = packet[0];
            int i1 = packet[1];
            int i2 = packet[2];
            int start2 = packet[3];

            if (total <= 0) return false;
            if (i1 < 0 || i2 < 0) return false;
            if (i1 >= total || i2 >= total) return false;
            if (start2 < 4 || start2 > packet.Length) return false;

            if (_total == null) _total = total;
            if (_total.Value != total) return false;

            string p1 = packet.Substring(4, start2 - 4);
            string p2 = packet.Substring(start2);

            if (!_chunks.ContainsKey(i1)) _chunks[i1] = p1;
            if (!_chunks.ContainsKey(i2)) _chunks[i2] = p2;

            info = $"total={total}, i1={i1}, i2={i2}, start2={start2}, len={packet.Length}, received={_chunks.Count}";

            if (IsComplete)
            {
                // ensure all indices exist
                for (int i = 0; i < _total.Value; i++)
                {
                    if (!_chunks.ContainsKey(i))
                    {
                        return false;
                    }
                }

                var arr = new string[_total.Value];
                for (int i = 0; i < _total.Value; i++) arr[i] = _chunks[i];
                reconstructed = string.Join("", arr);
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _chunks.Clear();
            _seenPackets.Clear();
            _total = null;
        }
    }
}
