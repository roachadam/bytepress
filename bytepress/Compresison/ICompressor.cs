using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bytepress.Compresison
{
    public interface ICompressor
    {
        string Name { get;  }
        byte[] Compress(byte[] data);
    }
}
