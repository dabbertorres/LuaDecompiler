using System;
using System.IO;
using System.Text;

namespace LuaDecompiler
{
	public class Generator : IDisposable
	{
		private FileStream fileStream;
		private StreamWriter writer;

		private uint functionCount;

		public Generator(string path)
		{
			fileStream = new FileStream(path, FileMode.Create);
			writer = new StreamWriter(fileStream, Encoding.UTF8);
			writer.NewLine = "\n";
			writer.AutoFlush = true;

			functionCount = 0;
		}

		public void Write(Lua.Function function, int indentLevel = 0)
		{
			// top level function
			if(function.lineNumber == 0 && function.lastLineNumber == 0)
			{
				WriteConstants(function);

				WriteChildFunctions(function);

				WriteInstructions(function);
			}
			else
			{
				string indents = new string('\t', indentLevel);

				string functionHeader = indents + "function func" + functionCount + "(";

				for(int i = 0; i < function.numParameters; ++i)
				{
					functionHeader += "arg" + i + (i + 1 != function.numParameters ? ", " : ")");
				}

				writer.Write(functionHeader);
				++functionCount;

				WriteConstants(function, indentLevel + 1);

				WriteChildFunctions(function, indentLevel + 1);

				WriteInstructions(function, indentLevel + 1);
			}
		}

		public void Dispose()
		{
			writer.Dispose();
			fileStream.Dispose();
		}

		private void WriteConstants(Lua.Function function, int indentLevel = 0)
		{
			uint constCount = 0;

			string indents = new string('\t', indentLevel);

			foreach(var c in function.constants)
			{
				string toWrite = indents + "const" + constCount + " = ";

				switch(c.Type)
				{
					case Lua.LuaType.Nil:
						toWrite += "nil";
						break;
					case Lua.LuaType.Bool:
						toWrite += ((Lua.BoolConstant)c).Value ? "true" : "false";
						break;
					case Lua.LuaType.Number:
						toWrite += ((Lua.NumberConstant)c).Value;
						break;
					case Lua.LuaType.String:
						{
							string str = ((Lua.StringConstant)c).Value;
							toWrite += '\"' + str.Substring(0, str.Length - 1) + '\"';  // substring to avoid printing out the null character
							break;
						}
				}

				writer.WriteLine(toWrite);
				++constCount;
			}
		}

		private void WriteChildFunctions(Lua.Function function, int indentLevel = 0)
		{
			foreach(var f in function.functions)
			{
				Write(f, indentLevel + 1);
			}
		}

		private void WriteInstructions(Lua.Function function, int indentLevel = 0)
		{
			string indents = new string('\t', indentLevel);

			foreach(var i in function.instructions)
			{
				string toWrite = indents;

				switch(i.OpCode)
				{
					case Lua.Instruction.Op.Move:
						toWrite = "var" + i.A + " = var" + i.B;
						break;

					case Lua.Instruction.Op.LoadK:
						toWrite = "var" + i.A + " = const" + i.Bx;
						break;

					case Lua.Instruction.Op.LoadBool:
						toWrite = "var" + i.A + " = " + (i.B != 0 ? "true" : "false");
						break;

					case Lua.Instruction.Op.LoadNil:
						for(uint x = i.A; x < i.B + 1; ++x)
							writer.WriteLine("var" + x + " = nil");
						break;

					case Lua.Instruction.Op.GetUpVal:
						toWrite = "var" + i.A + " = upvalue[" + i.B + "]";
						break;

					case Lua.Instruction.Op.GetGlobal:
						toWrite = "var" + i.A + " = _G[const" + i.Bx + "]";
						break;

					case Lua.Instruction.Op.GetTable:
						toWrite = "var" + i.A + " = var" + i.B + "[" + ToConstantIndex(i.C) + "]";
						break;

					case Lua.Instruction.Op.SetGlobal:
						toWrite = "_G[const" + i.Bx + "] = var" + i.A;
						break;

					case Lua.Instruction.Op.SetUpVal:
						toWrite = "upvalue[" + i.B + "] = var" + i.A;
						break;

					case Lua.Instruction.Op.SetTable:
						toWrite = "var" + i.A + "[" + ToConstantIndex(i.B) + "] = " + ToConstantIndex(i.C);
						break;

					case Lua.Instruction.Op.NewTable:
						toWrite = "var" + i.A + " = {}";
						break;

					case Lua.Instruction.Op.Self:
						toWrite = "var" + (i.A + 1) + " = var" + i.B + "\nvar" + i.A + " = var" + i.B + "[" + ToConstantIndex(i.C) + "]";
						break;

					case Lua.Instruction.Op.Add:
						toWrite = "var" + i.A + " = var" + i.B + " + var" + i.C;
						break;

					case Lua.Instruction.Op.Sub:
						toWrite = "var" + i.A + " = var" + i.B + " - var" + i.C;
						break;

					case Lua.Instruction.Op.Mul:
						toWrite = "var" + i.A + " = var" + i.B + " * var" + i.C;
						break;

					case Lua.Instruction.Op.Div:
						toWrite = "var" + i.A + " = var" + i.B + " / var" + i.C;
						break;

					case Lua.Instruction.Op.Mod:
						toWrite = "var" + i.A + " = var" + i.B + " % var" + i.C;
						break;

					case Lua.Instruction.Op.Pow:
						toWrite = "var" + i.A + " = var" + i.B + " ^ var" + i.C;
						break;

					case Lua.Instruction.Op.Unm:
						toWrite = "var" + i.A + " = -var" + i.B;
						break;

					case Lua.Instruction.Op.Not:
						toWrite = "var" + i.A + " = not var" + i.B;
						break;

					case Lua.Instruction.Op.Len:
						toWrite = "var" + i.A + " = #var" + i.B;
						break;

					case Lua.Instruction.Op.Concat:
						toWrite = "var" + i.A + " = ";
						for(uint x = i.B; x < i.C + 1; ++x)
							toWrite += "var" + x + (x != i.C ? ".." : "");
						break;

					case Lua.Instruction.Op.Jmp:
						break;

					case Lua.Instruction.Op.Eq:
						toWrite = "if (" + ToConstantIndex(i.B) + " == " + ToConstantIndex(i.C) + ") ~= " + i.A + " then";
						break;

					case Lua.Instruction.Op.Lt:
						toWrite = "if (" + ToConstantIndex(i.B) + " < " + ToConstantIndex(i.C) + ") ~= " + i.A + " then";
						break;

					case Lua.Instruction.Op.Le:
						toWrite = "if (" + ToConstantIndex(i.B) + " <= " + ToConstantIndex(i.C) + ") ~= " + i.A + " then";
						break;

					case Lua.Instruction.Op.Test:
						toWrite = "if not var" + i.A + " <=> " + i.C + " then";
						break;

					case Lua.Instruction.Op.TestSet:
						toWrite = "if var" + i.B + " <=> " + i.C + " then\n\tvar" + i.A + " = var" + i.B + "\nend";
						break;

					case Lua.Instruction.Op.Call:
						if(i.C != 0)
						{
							for(uint x = i.A; x < i.A + i.C - 1; ++x)
								toWrite += "var" + x + (x != i.A + i.C - 2 ? ", " : " = ");
						}
						else
						{
							throw new NotImplementedException("Yeah...");
						}

						toWrite += "var" + i.A + "(";

						if(i.B != 0)
						{
							for(uint x = i.A + 1; x < i.A + i.B; ++x)
								toWrite += "var" + x + (x != i.A + i.B - 1 ? ", " : ")");
						}
						else
						{
							throw new NotImplementedException("Yeah...");
						}

						break;

					case Lua.Instruction.Op.TailCall:
						break;

					case Lua.Instruction.Op.Return:
						toWrite = "return";
						break;

					case Lua.Instruction.Op.ForLoop:
						break;

					case Lua.Instruction.Op.ForPrep:
						break;

					case Lua.Instruction.Op.TForLoop:
						break;

					case Lua.Instruction.Op.SetList:
						break;

					case Lua.Instruction.Op.Close:
						break;

					case Lua.Instruction.Op.Closure:
						break;

					case Lua.Instruction.Op.VarArg:
						break;
				}

				writer.WriteLine(toWrite);
			}
		}

		private string ToConstantIndex(uint value)
		{
			// this is the logic from lua's source code (lopcodes.h).
			// specifically, the BITRK, ISK(x), and INDEXK(r) macros
			if((value & 1 << 8) != 0)
				return "const" + (value & ~(1 << 8));
			else
				return "var" + value;
		}
	}
}
