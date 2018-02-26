using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using bytepress.Compresison;
using bytepress.Compresison.LZMA;
using bytepress.Extensions;
using bytepress.Properties;

namespace bytepress.Engine
{
    public class Presser
    {
        private string _source;
        private string _file;
        private byte[] _fileBytes;
        private byte[] _fileBytesCompressed;
        private bool _isWpf;
        private AssemblyCloner _cloner;
        private ICompressor _compressor;
        private List<ICompressor> _compressors;
        private List<string> _libraries;
        public UpdateHandler UpdateStatus = delegate { };
        public delegate void UpdateHandler(string status, StatusType type);

        public enum StatusType
        {
            Normal,
            Warning,
            Error
        };

        public Presser(string file, bool isWpf)
        {
            _file = file;
            _isWpf = isWpf;
            _source = Resources.source;
            _cloner = new AssemblyCloner(_file);
            _compressors = new List<ICompressor>();
            LoadCompressors();
            CleanWorkspace();
        }

        public Presser(string file, byte[] data, bool isWpf)
        {
            _file = file;
            _fileBytes = data;
            _isWpf = isWpf;
            _source = Resources.source;
            _cloner = new AssemblyCloner(_file);
            _compressors = new List<ICompressor>();
            LoadCompressors();
            CleanWorkspace();
        }

        /// <summary>
        /// Set which compression algorithm to use.
        /// </summary>
        public void SetCompressor(string algorithm)
        {
            algorithm = algorithm.ToLower().Trim();
            switch (algorithm)
            {
                case "gzip":
                    _compressor = _compressors[0];
                    break;
                case "quicklz":
                    _compressor = _compressors[1];
                    break;
                case "lzma":
                    _compressor = _compressors[2];
                    break;
                default:
                    UpdateStatus($"Invalid compression algorithm '{algorithm}'. Defaulting to automatic calculation...", StatusType.Warning);
                    break;
            }

        }

        /// <summary>
        /// Verifies and sets the libraries to be merged with the main assembly.
        /// </summary>
        /// <param name="libraries"></param>
        public void MergeLibraries(List<string> libraries)
        {
            foreach (string lib in libraries)
            {
                if(!lib.ToLower().EndsWith(".dll"))
                    throw new Exception("Additional files must be .NET libraries (.dll).");

                byte[] temp = File.ReadAllBytes(lib);
                if (!IsManagedAssembly(temp))
                    throw new Exception("Libraries to merge must be valid .NET assemblies.");
                Array.Clear(temp, 0, temp.Length);
            }
            _libraries = libraries;
        }

        /// <summary>
        /// Checks the .NET header conventions to determine if assembly is managed (.NET).
        /// </summary>
        private bool IsManagedAssembly(byte[] payloadBuffer)
        {
            int e_lfanew = BitConverter.ToInt32(payloadBuffer, 0x3c);
            int magicNumber = BitConverter.ToInt16(payloadBuffer, e_lfanew + 0x18);
            int isManagedOffset = magicNumber == 0x10B ? 0xE8 : 0xF8;
            int isManaged = BitConverter.ToInt32(payloadBuffer, e_lfanew + isManagedOffset);
            return isManaged != 0;
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

            UpdateStatus("Verifying file is .NET assembly...", StatusType.Normal);
            if (!IsManagedAssembly(_fileBytes))
                throw new Exception("Only .NET executable files are supported");

            FileInfo f = new FileInfo(_file);

            if(_compressor == null)
                UpdateStatus("Calculating the best compression algorithm...", StatusType.Normal);

            Compress();

            UpdateStatus("Writing compressed payload to %temp%...", StatusType.Normal);
            File.WriteAllBytes(Path.GetTempPath() + "\\data", _fileBytesCompressed);

            UpdateStatus("Copying assembly information and icon...", StatusType.Normal);
            CopyAssembly();

            UpdateStatus("Compiling...", StatusType.Normal);

            string outLocation;
            if (_libraries != null && _libraries.Count > 0)
                outLocation = Path.GetTempPath() + f.Name.Replace(".exe", "_bytepressed.exe");

            else
                outLocation = f.DirectoryName + "\\" + f.Name.Replace(".exe", "_bytepressed.exe");

            Compiler comp = new Compiler
            {
                CompileLocation = outLocation,
                ResourceFiles = new[] { Path.GetTempPath() + "\\data", Application.StartupPath + "\\bytepress.lib.dll" },
                SourceCodes = new[] { _source },
                References = new[] {
                    "System.dll",
                    "System.Reflection.dll",
                },

                Icon = Path.GetTempPath() + "\\icon.ico"
            };
            if (_isWpf)
            {
                // Credit: https://stackoverflow.com/questions/12429917/load-wpf-application-from-the-memory
                comp.WPFReferences = new[]
                {
                    "System.Xaml.dll",
                    "PresentationCore.dll",
                    "PresentationFramework.dll",
                    "WindowsBase.dll"
                };
                _source = _source.Replace("//wpfhack", Resources.wpfhack);
                comp.SourceCodes = new[] {_source};
            }

            if (!comp.Compile())
            {
                CleanWorkspace();
                throw new Exception(comp.CompileError);
            }

            if (_libraries != null && _libraries.Count > 0)
            {
                UpdateStatus("Merging additional libraries...", StatusType.Normal);
                Merger m = new Merger(_libraries);
                if (!m.Merge(outLocation, f.DirectoryName + "\\" + f.Name.Replace(".exe", "_bytepressed.exe")))
                    throw new Exception("Failed to merge libraries");
            }


            Console.WriteLine();

            byte[] compiled = File.ReadAllBytes(f.DirectoryName + "\\" + f.Name.Replace(".exe", "_bytepressed.exe"));
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
        private void Compress()
        {
            if (_compressor == null)
            {
                long bestLength = Int64.MaxValue;
                foreach (ICompressor c in _compressors)
                {
                    UpdateStatus("--------------------------------------------", StatusType.Normal);
                    UpdateStatus($"Testing algorithm: {c.Name}...", StatusType.Normal);
                    byte[] tmp = c.Compress(_fileBytes);
                    double cR = 100 - (double)tmp.Length / _fileBytes.Length * 100.0;
                    UpdateStatus($"{c.Name} Compression Ratio: {cR:F}%", StatusType.Normal);
                    if (tmp.Length < bestLength)
                    {
                        _compressor = c;
                        bestLength = tmp.Length;
                        _fileBytesCompressed = tmp;
                    }
                }
            }
            else
            {
                UpdateStatus($"Using algorithm: {_compressor.Name}...", StatusType.Normal);
                _fileBytesCompressed = _compressor.Compress(_fileBytes);
                double cR = 100 - (double)_fileBytesCompressed.Length / _fileBytes.Length * 100.0;
                UpdateStatus($"{_compressor.Name} Compression Ratio: {cR:F}%", StatusType.Normal);
                _source = _source.Replace("*type*", _compressors.IndexOf(_compressor).ToString());
                return;
            }


            UpdateStatus("--------------------------------------------", StatusType.Normal);
            
            if (_compressor.GetType() == typeof(GZIP))
            {
                _source = _source.Replace("*type*", "0");
                UpdateStatus("Chosen algorithm: GZIP", StatusType.Normal);
            }
            if (_compressor.GetType() == typeof(QuickLZ))
            {
                _source = _source.Replace("*type*", "1");
                UpdateStatus("Chosen algorithm: QuickLZ", StatusType.Normal);
            }
            if (_compressor.GetType() == typeof(LZMAez))
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
                "icon.ico",
            };
            foreach (string file in tempFiles)
            {
                if (File.Exists(Path.GetTempPath() + file))
                    File.Delete(Path.GetTempPath() + file);
            }
        }
    }
}
