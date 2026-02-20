using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ipswintakplugin.AnimatedQr
{
    internal sealed class DecodeResult
    {
        public string RawReconstructed { get; set; }
        public string MimeType { get; set; }
        public int Base64ByteLength { get; set; }
        public bool MimeIndicatesGzip { get; set; }
        public bool LooksGzippedMagic { get; set; }
        public bool Decompressed { get; set; }
        public string DecodedUtf8 { get; set; }
        public string DecodeError { get; set; }
        public JObject Envelope { get; set; }
    }

    internal static class IpsEnvelopeDecoder
    {
        public static DecodeResult Decode(string rawReconstructed)
        {
            var report = new DecodeResult
            {
                RawReconstructed = rawReconstructed
            };

            try
            {
                JObject envelope;
                try
                {
                    envelope = JObject.Parse(rawReconstructed);
                    report.Envelope = envelope;
                }
                catch (Exception ex)
                {
                    report.DecodeError = $"Envelope JSON parse failed: {ex.Message}";
                    return report;
                }

                var data = envelope["data"]?.ToString();
                var mime = envelope["mimeType"]?.ToString();

                if (string.IsNullOrWhiteSpace(data))
                {
                    report.DecodeError = "Envelope missing 'data' string";
                    return report;
                }
                if (string.IsNullOrWhiteSpace(mime))
                {
                    report.DecodeError = "Envelope missing 'mimeType' string";
                    return report;
                }

                report.MimeType = mime;

                // NOTE: AES path can be added later (your webapp does a server-side decode)
                if (mime.IndexOf("aes256", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    report.DecodeError = "Encrypted (aes256) payload not supported in WinTAK yet (add local decrypt or call a service).";
                    return report;
                }

                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(data);
                    report.Base64ByteLength = bytes.Length;
                }
                catch (Exception ex)
                {
                    report.DecodeError = $"Base64 decode failed: {ex.Message}";
                    return report;
                }

                report.MimeIndicatesGzip = mime.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0;
                report.LooksGzippedMagic = LooksGzipped(bytes);

                if (report.MimeIndicatesGzip || report.LooksGzippedMagic)
                {
                    try
                    {
                        bytes = Ungzip(bytes);
                        report.Decompressed = true;
                    }
                    catch (Exception ex)
                    {
                        report.DecodeError = $"Gzip decompress failed: {ex.Message}";
                        return report;
                    }
                }

                report.DecodedUtf8 = Encoding.UTF8.GetString(bytes);
                return report;
            }
            catch (Exception ex)
            {
                report.DecodeError = $"Decode failed: {ex.Message}";
                return report;
            }
        }

        private static bool LooksGzipped(byte[] bytes)
            => bytes != null && bytes.Length >= 3 && bytes[0] == 0x1f && bytes[1] == 0x8b && bytes[2] == 0x08;

        private static byte[] Ungzip(byte[] gzipBytes)
        {
            using var input = new MemoryStream(gzipBytes);
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gz.CopyTo(output);
            return output.ToArray();
        }
    }
}
