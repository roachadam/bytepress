using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CSharp;

namespace bytepress.Engine
{
	public class Compiler
	{
		public string[] References { get; set; }
		public string CompileLocation { get; set; }
		public string Icon { get; set; }
		public string[] SourceCodes { get; set; }
		public string[] ResourceFiles { get; set; }
		public string CompileError { get; set; }

		public bool Compile()
		{
			try
			{
				if (References == null)
					throw new ArgumentNullException("References");
				if (CompileLocation == null)
					throw new ArgumentNullException("CompileLocation");
				if (SourceCodes == null)
					throw new ArgumentNullException("SourceCodes");

				string res = null;
				string net = Environment.SystemDirectory[0] + @":\Windows\Microsoft.NET\Framework\v4.0.30319\";

				CompilerParameters compilerParameters = new CompilerParameters()
				{
                   TreatWarningsAsErrors = false,
				   OutputAssembly = CompileLocation,
                };
				CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider(new Dictionary<string, string>
				{
					{
						"CompilerVersion",
						"v4.0"
					}
				});

				foreach (string reference in References)
					compilerParameters.ReferencedAssemblies.Add(net + reference);

			    if (ResourceFiles != null)
			    {
			        foreach (string resName in ResourceFiles)
			            res += "/res:\"" + resName + "\" ";
                }

                StringBuilder args = new StringBuilder();
			    args.Append("/platform:x86 /target:winexe /nostdlib /filealign:512 /debug- /unsafe /optimize ");

			    if (!string.IsNullOrEmpty(Icon) && File.Exists(Icon))
			        args.Append("/win32icon:\"" + Icon + "\" ");

			    args.Append(res);
				//if (!string.IsNullOrEmpty(Icon))
				//	compilerParameters.CompilerOptions = "/platform:x86 /target:winexe /nostdlib /filealign:512 /debug- /unsafe /optimize- /win32icon:\"" +
				//		Icon + "\" " + res;
				//else
				//	compilerParameters.CompilerOptions = "/platform:x86 /target:winexe /nostdlib /filealign:512 /debug- /unsafe /optimize- " + res;

			    compilerParameters.CompilerOptions = args.ToString();
				CompilerResults compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, SourceCodes);
				foreach (var v in compilerResults.Output)
					CompileError += v + Environment.NewLine;

				return CompileError == null;
			}
			catch (Exception ex)
			{
				CompileError = ex.ToString();
				return false;
			}
		}
	}
}


