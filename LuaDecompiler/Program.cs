using System;

namespace LuaDecompiler
{
	class Program
	{
		static void Main(string[] args)
		{
			if(args.Length < 2)
			{
				Console.WriteLine("Must be 2 arguments (input file, output file).");
				return;
			}

			string inputFile = args[0];
			string outputFile = args[1];

			FileHeader header;
			Lua.Function function = null;

			try
			{
				using(FileReader reader = new FileReader(inputFile))
				{
					header = reader.Header;
					function = reader.NextFunctionBlock();
				}

				Console.WriteLine("Signature: " + header.signature);
				Console.WriteLine("Version: " + header.version);
				Console.WriteLine("Format: " + header.format);
				Console.WriteLine("isLittleEndian: " + header.isLittleEndian);
				Console.WriteLine("intSize: " + header.intSize);
				Console.WriteLine("size_tSize: " + header.size_tSize);
				Console.WriteLine("instructionSize: " + header.instructionSize);
				Console.WriteLine("lua_NumberSize: " + header.lua_NumberSize);
				Console.WriteLine("isIntegral: " + header.isIntegral);

				using(Generator gen = new Generator(outputFile))
				{
					gen.Write(function);
				}

				Console.WriteLine("\nDone!");
			}
			catch(Exception ex)
			{
				Console.WriteLine("\nError: {0}", ex);
			}

			Console.WriteLine("Press Enter to continue.");
			Console.ReadLine();
		}
	}
}
