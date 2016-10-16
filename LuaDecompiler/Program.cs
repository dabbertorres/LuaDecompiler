using System;

namespace LuaDecompiler
{
	class Program
	{
		public static void Main(string[] args)
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

				Console.WriteLine(header);

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
