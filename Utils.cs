using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CaptchaBuster
{
    internal class Utils
    {
        public static byte[] ConvertBinaryStringToBytes(string binaryString)
        {
            // Calculate the number of padding zeros needed
            int paddingZeros = binaryString.Length % 8 == 0 ? 0 : 8 - (binaryString.Length % 8);

            // Pad the binary string with leading zeros if necessary
            binaryString = binaryString.PadLeft(binaryString.Length + paddingZeros, '0');

            // Create byte array to hold converted bytes
            byte[] bytes = new byte[binaryString.Length / 8];

            // Convert each 8 bits of binary string to a byte
            for (int i = 0; i < binaryString.Length; i += 8)
            {
                bytes[i / 8] = Convert.ToByte(binaryString.Substring(i, 8), 2);
            }

            return bytes;
        }

        public static byte[] CompressBytes(byte[] input)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(input, 0, input.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] ConvertPythonByteStringToBytes(string pythonByteString)
        {
            // Use regex to match hex values and convert them to bytes
            MatchCollection matches = Regex.Matches(pythonByteString, @"\\x[0-9A-Fa-f]{2}");
            byte[] bytes = new byte[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                bytes[i] = Convert.ToByte(matches[i].Value.Substring(2), 16);
            }

            return bytes;
        }

        public static byte[] DecompressZlibBytes(byte[] input)
        {
            using (MemoryStream memoryStream = new MemoryStream(input))
            {
                using (MemoryStream decompressedMemoryStream = new MemoryStream())
                {
                    // Skip the first two bytes (zlib header)
                    memoryStream.Seek(2, SeekOrigin.Begin);

                    // Decompress the rest of the stream
                    using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(decompressedMemoryStream);
                    }

                    return decompressedMemoryStream.ToArray();
                }
            }
        }

        public static bool SequenceContains(byte[] bytes, byte[] sequence)
        {
            if (sequence.Length > bytes.Length)
                return false;

            for (int i = 0; i <= bytes.Length - sequence.Length; i++)
            {
                var found = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (bytes[i + j] != sequence[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return true;
            }

            return false;
        }
    }
}
