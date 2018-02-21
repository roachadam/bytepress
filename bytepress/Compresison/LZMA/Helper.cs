using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace bytepress.Compresison.LZMA
{
    public class LZMAez : ICompressor
    {
        string ICompressor.Name => "LZMA";
        #region Settings

        /// <summary>
        /// 2 MB of memory will be reserved for dictionary.
        /// </summary>
        static Int32 dictionary = 1 << 21; // 2 MB

        static Int32 posStateBits = 2;
        static Int32 litContextBits = 3;
        static Int32 litPosBits = 0;
        static Int32 algorithm = 2;

        /// <summary>
        /// Incerease numFastBytes to get better compression ratios.
        /// 32 - moderate compression
        /// 128 - extreme compression
        /// </summary>
        static Int32 numFastBytes = 32;

        static bool eos = false;

        static CoderPropID[] propIDs =
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        static object[] properties =
        {
            (Int32) (dictionary),
            (Int32) (posStateBits),
            (Int32) (litContextBits),
            (Int32) (litPosBits),
            (Int32) (algorithm),
            (Int32) (numFastBytes),
            "bt4",
            eos
        };

        #endregion

        /// <summary>
        /// Compress input stream into output stream using LZMA.
        /// Remeber to set desirable positions input and output streams.
        /// Input reading starts at inStream.Position and ends at inStream.Length.
        /// Output writing starts at outStream.Position.
        /// </summary>
        /// <param name="inStream">Input stream</param>
        /// <param name="outStream">Output stream</param>
        public byte[] Compress(byte[] data)
        {
            LZMA.Compress.LZMA.Encoder encoder = new LZMA.Compress.LZMA.Encoder();

            encoder.SetCoderProperties(propIDs, properties);
            using (MemoryStream inStream = new MemoryStream(data))
            using (MemoryStream outStream = new MemoryStream())
            {
                encoder.WriteCoderProperties(outStream);
                var writer = new BinaryWriter(outStream, Encoding.UTF8);
                // Write original size.
                writer.Write(inStream.Length - inStream.Position);

                // Save position with compressed size.
                long positionForCompressedSize = outStream.Position;
                // Leave placeholder for size after compression.
                writer.Write((Int64)0);

                long positionForCompressedDataStart = outStream.Position;
                encoder.Code(inStream, outStream, -1, -1, null);

                long positionAfterCompression = outStream.Position;

                // Seek back to the placeholder for compressed size.
                outStream.Position = positionForCompressedSize;

                // Write size after compression.
                writer.Write((Int64)(positionAfterCompression - positionForCompressedDataStart));

                // Restore position.
                outStream.Position = positionAfterCompression;
                return outStream.ToArray();
            }

        }

        public byte[] Decompress(byte[] data)
        {
            using (MemoryStream inStream = new MemoryStream(data))
            using (MemoryStream outStream = new MemoryStream())
            {
                byte[] properties = new byte[5];
                if (inStream.Read(properties, 0, 5) != 5)
                    throw (new Exception("Input stream is too short."));

                Compress.LZMA.Decoder decoder = new Compress.LZMA.Decoder();
                decoder.SetDecoderProperties(properties);

                var br = new BinaryReader(inStream, Encoding.UTF8);
                long decompressedSize = br.ReadInt64();
                long compressedSize = br.ReadInt64();
                decoder.Code(inStream, outStream, compressedSize, decompressedSize, null);
                return outStream.ToArray();
            }
        }

    }
}