using System;
using System.IO;
using System.Windows.Forms;
using bytepress.Engine;

namespace bytepress
{
    class Program
    {
        /// <summary>
        /// Main method to handle logic.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0) return;
                Watermark();

                string inFile = args[0];

                if (!File.Exists(inFile))
                    throw new Exception("Specified file does not exist");

                FileInfo f = new FileInfo(inFile);
                byte[] original = File.ReadAllBytes(inFile);

                if (!f.Name.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception("Only executable files are supported");

                UpdateStatus("Verifying file is .NET assembly...", Presser.StatusType.Normal);
                if (!IsManagedAssembly(original))
                    throw new Exception("Only .NET executable files are supported");

                Presser p = new Presser(inFile, original);
                p.UpdateStatus += UpdateStatus;
                p.Process();

                Console.Read();
            }
            catch (Exception e)
            {
                UpdateStatus(e.ToString(), Presser.StatusType.Error);
                throw;
                Environment.Exit(0); // Failsafe
            }

        }

        /// <summary>
        /// Checks the .NET header conventions to determine if assembly is managed (.NET).
        /// </summary>
        private static bool IsManagedAssembly(byte[] payloadBuffer)
        {
            int e_lfanew = BitConverter.ToInt32(payloadBuffer, 0x3c);
            int magicNumber = BitConverter.ToInt16(payloadBuffer, e_lfanew + 0x18);
            int isManagedOffset = magicNumber == 0x10B ? 0xE8 : 0xF8;
            int isManaged = BitConverter.ToInt32(payloadBuffer, e_lfanew + isManagedOffset);
            return isManaged != 0;
        }

        /// <summary>
        /// Handles the console status updating.
        /// </summary>
        private static void UpdateStatus(string status, Presser.StatusType type)
        {
            switch (type)
            {
                case Presser.StatusType.Normal:
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: {status}");
                    break;
                case Presser.StatusType.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: {status}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case Presser.StatusType.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: {status}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }


        /// <summary>
        /// Watermarks the console with information.
        /// </summary>
        private static void Watermark()
        {
            Console.WriteLine(@"
            ██████╗ ██╗   ██╗████████╗███████╗██████╗ ██████╗ ███████╗███████╗███████╗
            ██╔══██╗╚██╗ ██╔╝╚══██╔══╝██╔════╝██╔══██╗██╔══██╗██╔════╝██╔════╝██╔════╝
            ██████╔╝ ╚████╔╝    ██║   █████╗  ██████╔╝██████╔╝█████╗  ███████╗███████╗
            ██╔══██╗  ╚██╔╝     ██║   ██╔══╝  ██╔═══╝ ██╔══██╗██╔══╝  ╚════██║╚════██║
            ██████╔╝   ██║      ██║   ███████╗██║     ██║  ██║███████╗███████║███████║
            ╚═════╝    ╚═╝      ╚═╝   ╚══════╝╚═╝     ╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝
            Version: " + $"{Application.ProductVersion}" + "\r\nAuthor: Adam Roach\r\nProject: github.com/roachadam/bytepress\r\n");
        }
    }
}
