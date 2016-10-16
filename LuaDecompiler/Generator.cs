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
			writer = new StreamWriter(fileStream, Encoding.ASCII);
			writer.NewLine = "\n";
			writer.AutoFlush = true;

			functionCount = 0;
		}

		public void Write(Lua.Function function, int indentLevel = 0)
		{
			// top level function
			if(function.lineNumber == 0 && function.lastLineNumber == 0)
			{
//				WriteConstants(function);

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

//				WriteConstants(function, indentLevel + 1);

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
				writer.WriteLine("{2}const{0} = {1}", constCount, c.ToString(), indents);
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
				switch(i.OpCode)
				{
					case Lua.Instruction.Op.Move:
						writer.WriteLine("{2}var{0} = var{1}", i.A, i.B, indents);
						break;

					case Lua.Instruction.Op.LoadK:
						writer.WriteLine("{2}var{0} = {1}", i.A, GetConstant(i.Bx, function), indents);
						break;

					case Lua.Instruction.Op.LoadBool:
						writer.WriteLine("{2}var{0} = {1}", i.A, (i.B != 0 ? "true" : "false"), indents);
						break;

					case Lua.Instruction.Op.LoadNil:
						for(int x = i.A; x < i.B + 1; ++x)
							writer.WriteLine("{1}var{0} = nil", x, indents);
						break;

					case Lua.Instruction.Op.GetUpVal:
						writer.WriteLine("{2}var{0} = upvalue[{1}]", i.A, i.B, indents);
						break;

					case Lua.Instruction.Op.GetGlobal:
						writer.WriteLine("{2}var{0} = _G[{1}]", i.A, GetConstant(i.Bx, function), indents);
						break;

					case Lua.Instruction.Op.GetTable:
						writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
						break;

					case Lua.Instruction.Op.SetGlobal:
						writer.WriteLine("{2}_G[{0}] = var{1}", GetConstant(i.Bx, function), i.A, indents);
						break;

					case Lua.Instruction.Op.SetUpVal:
						writer.WriteLine("{2}upvalue[{0}] = var{1}", i.B, i.A, indents);
						break;

					case Lua.Instruction.Op.SetTable:
						writer.WriteLine("{3}var{0}[{1}] = {2}", i.A, WriteIndex(i.B, function), WriteIndex(i.C, function), indents);
						break;

					case Lua.Instruction.Op.NewTable:
						writer.WriteLine("{1}var{0} = {{}}", i.A, indents);
						break;

					case Lua.Instruction.Op.Self:
						writer.WriteLine("{2}var{0} = var{1}", i.A + 1, i.B, indents);
						writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
						break;

					case Lua.Instruction.Op.Add:
						writer.WriteLine("{3}var{0} = var{1} + var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Sub:
						writer.WriteLine("{3}var{0} = var{1} - var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Mul:
						writer.WriteLine("{3}var{0} = var{1} * var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Div:
						writer.WriteLine("{3}var{0} = var{1} / var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Mod:
						writer.WriteLine("{3}var{0} = var{1} % var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Pow:
						writer.WriteLine("{3}var{0} = var{1} ^ var{2}", i.A, i.B, i.C, indents);
						break;

					case Lua.Instruction.Op.Unm:
						writer.WriteLine("{2}var{0} = -var{1}", i.A, i.B, indents);
						break;

					case Lua.Instruction.Op.Not:
						writer.WriteLine("{2}var{0} = not var{1}", i.A, i.B, indents);
						break;

					case Lua.Instruction.Op.Len:
						writer.WriteLine("{2}var{0} = #var{1}", i.A, i.B, indents);
						break;

					case Lua.Instruction.Op.Concat:
						writer.Write("{1}var{0} = ", i.A, indents);

						for(int x = i.B; x < i.C; ++x)
							writer.Write("var{0} .. ", x);

						writer.WriteLine("var{0}", i.C);
						break;

					case Lua.Instruction.Op.Jmp:
						throw new NotImplementedException("Jmp");

					case Lua.Instruction.Op.Eq:
						writer.WriteLine("{3}if ({0} == {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
						break;

					case Lua.Instruction.Op.Lt:
						writer.WriteLine("{3}if ({0} < {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
						break;

					case Lua.Instruction.Op.Le:
						writer.WriteLine("{3}if ({0} <= {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
						break;

					case Lua.Instruction.Op.Test:
						writer.WriteLine("{2}if not var{0} <=> {1} then", i.A, i.C, indents);
						break;

					case Lua.Instruction.Op.TestSet:
						writer.WriteLine("{2}if var{0} <=> {1} then", i.B, i.C, indents);
						writer.WriteLine("{2}\tvar{0} = var{1}", i.A, i.B, indents);
						writer.WriteLine("end");
						break;

					case Lua.Instruction.Op.Call:
						StringBuilder sb = new StringBuilder();

						if(i.C != 0)
						{
							sb.Append(indents);
							var indentLen = sb.Length;

							// return values
							for(int x = i.A; x < i.A + i.C - 2; ++x)
								sb.AppendFormat("var{0}, ", x);

							if(sb.Length - indentLen > 2)
							{
								sb.Remove(sb.Length - 2, 2);
								sb.Append(" = ");
							}
						}
						else
						{
							throw new NotImplementedException("i.C == 0");
						}

						// function
						sb.AppendFormat("var{0}(", i.A);

						if(i.B != 0)
						{
							var preArgsLen = sb.Length;

							// arguments
							for(int x = i.A; x < i.A + i.B - 1; ++x)
								sb.AppendFormat("var{0}, ", x + 1);

							if(sb.Length - preArgsLen > 2)
								sb.Remove(sb.Length - 2, 2);

							sb.Append(')');
						}
						else
						{
							throw new NotImplementedException("i.B == 0");
						}

						writer.WriteLine(sb.ToString());

						break;

					case Lua.Instruction.Op.TailCall:
						throw new NotImplementedException("TailCall");

					case Lua.Instruction.Op.Return:
						writer.WriteLine("return");
						break;

					case Lua.Instruction.Op.ForLoop:
						throw new NotImplementedException("ForLoop");

					case Lua.Instruction.Op.ForPrep:
						throw new NotImplementedException("ForPrep");

					case Lua.Instruction.Op.TForLoop:
						throw new NotImplementedException("TForLoop");

					case Lua.Instruction.Op.SetList:
						throw new NotImplementedException("SetList");

					case Lua.Instruction.Op.Close:
						throw new NotImplementedException("Close");

					case Lua.Instruction.Op.Closure:
						throw new NotImplementedException("Closure");

					case Lua.Instruction.Op.VarArg:
						throw new NotImplementedException("VarArg");
				}
			}
		}

		private string GetConstant(int idx, Lua.Function function)
		{
			return function.constants[idx].ToString();
		}

		private int ToIndex(int value, out bool isConstant)
		{
			// this is the logic from lua's source code (lopcodes.h)
			if(isConstant = (value & 1 << 8) != 0)
				return value & ~(1 << 8);
			else
				return value;
		}

		private string WriteIndex(int value, Lua.Function function)
		{
			bool constant;
			int idx = ToIndex(value, out constant);

			if(constant)
				return function.constants[idx].ToString();
			else
				return "var" + idx;
		}
	}
}
