using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using bytepress.Engine;
using NDesk.Options;

namespace bytepress
{
    class Program 
    {
        /// <summary>
        /// Main method to handle logic.
        /// </summary>
        static void Main(string[] args)
        {
            string algo = string.Empty;
            bool isWpf = false;
            bool showHelp = false;
            List<string> libs = new List<string>();
            OptionSet argz = new OptionSet {
                {
                    "a|algorithm=", "the compression algorithm to use. (gzip, quicklz, lzma)",
                    v => algo = v
                },
                {
                    "l|lib=", "libraries to merge to main assembly",
                    v => libs.Add(v)
                },
                {
                    "wpf", "must be specified for wpf applications to work",
                    v => isWpf = true
                },
                {
                    "h|help",  "show this message",
                    v => showHelp = v != null
                },
            };

            try
            {
                Watermark();

                List<string> argsList = argz.Parse(args);

                if (showHelp || argsList.Count == 0)
                {
                    ShowHelp(argz);
                    return;
                }

                if (argsList.Count > 0)
                {
                    foreach (string arg in argsList)
                    {
                        if(arg.ToLower().EndsWith(".dll"))
                            libs.Add(arg);
                    }
                }

                string inFile = args[0];

                if (!File.Exists(inFile))
                    throw new Exception("Specified file does not exist");

                FileInfo f = new FileInfo(inFile);
                byte[] original = File.ReadAllBytes(inFile);

                if (!f.Name.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception("Only executable files are supported");
                
                Presser p = new Presser(inFile, original, isWpf);
                p.UpdateStatus += UpdateStatus;
                
                if(!string.IsNullOrEmpty(algo))
                    p.SetCompressor(algo);

                if(libs.Count > 0)
                    p.MergeLibraries(libs);

                p.Process();
            }
            catch (OptionException e)
            {
                UpdateStatus(e.ToString(), Presser.StatusType.Error);
                UpdateStatus("Try bytepress --help for more information.", Presser.StatusType.Error);
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
        /// Displays help information to the user
        /// </summary>
        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: bytepress [file to compress] [option] [value]");
            p.WriteOptionDescriptions(Console.Out);
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
