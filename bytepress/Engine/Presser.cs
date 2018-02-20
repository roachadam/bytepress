using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using bytepress.Compresison;
using bytepress.Compresison.LZMA;
using bytepress.Extensions;

namespace bytepress.Engine
{
    public class Presser
    {
        private string _source;
        private string _file;
        private byte[] _fileBytes;
        private byte[] _fileBytesCompressed;
        private AssemblyCloner _cloner;
        private static List<ICompressor> _compressors;

        public UpdateHandler UpdateStatus = delegate { };
        public delegate void UpdateHandler(string status, StatusType type);

        public enum StatusType
        {
            Normal,
            Warning,
            Error
        };


        public Presser(string file)
        {
            _file = file;
            _source = Properties.Resources.source;
            _cloner = new AssemblyCloner(_file);
            _compressors = new List<ICompressor>();
            LoadCompressors();
            CleanWorkspace();
        }

        public Presser(string file, byte[] data)
        {
            _file = file;
            _fileBytes = data;
            _source = Properties.Resources.source;
            _cloner = new AssemblyCloner(_file);
            _compressors = new List<ICompressor>();
            LoadCompressors();
            CleanWorkspace();
        }

        /// <summary>
        /// Loads the available compression algorithms.
        /// </summary>
        private void LoadCompressors()
        {
            _compressors.Add(new GZIP());
            _compressors.Add(new QuickLZ());
            _compressors.Add(new LZMAez());
        }

        /// <summary>
        /// Compresses the supplied executable and generates a wrapper program to load it in memory.
        /// </summary>
        public void Process()
        {
            if(_fileBytes == null)
                _fileBytes = File.ReadAllBytes(_file);

            FileInfo f = new FileInfo(_file);

            UpdateStatus($"Calculating the best compression algorithm...", StatusType.Normal);
            PickCompressor();

            UpdateStatus($"Writing compressed payload to %temp%...", StatusType.Normal);
            File.WriteAllBytes(Path.GetTempPath() + "\\data", _fileBytesCompressed);

            UpdateStatus("Copying assembly information and icon...", StatusType.Normal);
            CopyAssembly();

            UpdateStatus("Compiling...", StatusType.Normal);
            Compiler comp = new Compiler
            {
                // add option to save as same name
                CompileLocation = f.DirectoryName + "\\" + f.Name.Replace(".exe", "_bytepressed.exe"),
                ResourceFiles = new[] { Path.GetTempPath() + "\\data", Application.StartupPath + "\\bytepress.lib.dll" },
                SourceCodes = new[] { _source },
                References = new[] {
                    "mscorlib.dll",
                    "System.dll",
                    "System.Reflection.dll"
                },
                Icon = Path.GetTempPath() + "\\icon.ico"
            };

            if (!comp.Compile())
            {
                CleanWorkspace();
                throw new Exception(comp.CompileError);
            }
                
            Console.WriteLine();

            byte[] compiled = File.ReadAllBytes(comp.CompileLocation);
            double compressionRatio = 100 - (double)compiled.Length / _fileBytes.Length * 100.0;
            UpdateStatus("Compression Results", StatusType.Normal);
            UpdateStatus("--------------------------------------------", StatusType.Normal);
            UpdateStatus($"Initial File Size:    {_fileBytes.Length.ToPrettySize(2)}", StatusType.Normal);
            UpdateStatus($"Compressed File Size: {_fileBytesCompressed.Length.ToPrettySize(2)}", StatusType.Normal);
            UpdateStatus($"Compression Ratio:    {compressionRatio:F}%", StatusType.Normal);

            if (compiled.Length > _fileBytes.Length)
                UpdateStatus("Warning: Compressed size is larger than original.", StatusType.Warning);
            UpdateStatus("--------------------------------------------", StatusType.Normal);
            Console.WriteLine();
            UpdateStatus("Cleaning up...", StatusType.Normal);
            CleanWorkspace();
            
            UpdateStatus($"Done! File successfully bytepressed to: {comp.CompileLocation}", StatusType.Normal);
        }

        /// <summary>
        /// Tests and chooses the best compression algorithm.
        /// </summary>
        private void PickCompressor()
        {
            ICompressor best = null;
            long bestLength = long.MaxValue;
            foreach (ICompressor c in _compressors)
            {
                UpdateStatus("--------------------------------------------", StatusType.Normal);
                UpdateStatus($"Testing algorithm: {c.Name}...", StatusType.Normal);
                byte[] tmp = c.Compress(_fileBytes);
                double cR = 100 - (double)tmp.Length / _fileBytes.Length * 100.0;
                UpdateStatus($"{c.Name} Compression Ratio: {cR:F}%", StatusType.Normal);
                if (tmp.Length < bestLength)
                {
                    best = c;
                    bestLength = tmp.Length;
                    _fileBytesCompressed = tmp;
                }
            }

            UpdateStatus("--------------------------------------------", StatusType.Normal);

            if (best.GetType() == typeof(GZIP))
            {
                _source = _source.Replace("*type*", "0");
                UpdateStatus("Chosen algorithm: GZIP", StatusType.Normal);
            }
            if (best.GetType() == typeof(QuickLZ))
            {
                _source = _source.Replace("*type*", "1");
                UpdateStatus("Chosen algorithm: QuickLZ", StatusType.Normal);
            }
            if (best.GetType() == typeof(LZMAez))
            {
                _source = _source.Replace("*type*", "2");
                UpdateStatus("Chosen algorithm: LZMA", StatusType.Normal);
            }
        }

        /// <summary>
        /// Replaces the codedom assembly information with the information from the supplied executable.
        /// </summary>
        private void CopyAssembly()
        {
            _source = _source.Replace("[assembly: AssemblyTitle(\"\")]", $"[assembly: AssemblyTitle(\"{_cloner.Title}\")]");
            _source = _source.Replace("[assembly: AssemblyDescription(\"\")]", $"[assembly: AssemblyDescription(\"{_cloner.Description}\")]");
            _source = _source.Replace("[assembly: AssemblyCompany(\"\")]", $"[assembly: AssemblyCompany(\"{_cloner.Company}\")]");
            _source = _source.Replace("[assembly: AssemblyProduct(\"\")]", $"[assembly: AssemblyProduct(\"{_cloner.Product}\")]");
            _source = _source.Replace("[assembly: AssemblyCopyright(\"\")]", $"[assembly: AssemblyCopyright(\"{_cloner.Copyright}\")]");
            _source = _source.Replace("[assembly: Guid(\"\")]", "[assembly: Guid(\"" + Guid.NewGuid() + "\")]");
            _source = _source.Replace("[assembly: AssemblyVersion(\"\")]", $"[assembly: AssemblyVersion(\"{_cloner.Version}\")]");
            _source = _source.Replace("[assembly: AssemblyFileVersion(\"\")]", $"[assembly: AssemblyFileVersion(\"{_cloner.Version}\")]");

            if (_cloner.Icon != null)
                using (FileStream s = new FileStream(Path.GetTempPath() + "icon.ico", FileMode.CreateNew))
                    _cloner.Icon.Save(s);
        }

        /// <summary>
        /// Cleans the temporary files needed for compilation.
        /// </summary>
        private void CleanWorkspace()
        {
            string[] tempFiles =
            {
                "data",
                "icon.ico"
            };
            foreach (string file in tempFiles)
            {
                if (File.Exists(Path.GetTempPath() + file))
                    File.Delete(Path.GetTempPath() + file);
            }
        }
    }
}
