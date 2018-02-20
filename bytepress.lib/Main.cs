using bytepress.lib.LZMA;

namespace bytepress.lib
{
    public class Main
    {
        public static byte[] Decompress(byte[] data, int compressionType)
        {
            switch (compressionType)
            {
                case 0:
                    return Gzip.Decompress(data);
                case 1:
                    return QuickLZ.Decompress(data);
                case 2:
                    return Helper.Decompress(data);
            }
            return null;
        }
    }
}
