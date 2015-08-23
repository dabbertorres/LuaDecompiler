using System.Collections.Generic;

namespace LuaDecompiler.Lua
{
	public class Function
	{
		public enum VarArg
		{
			Has = 1,
			Is = 2,
			Needs = 4,
		}

		public string sourceName;
		public uint lineNumber;
		public uint lastLineNumber;
		public byte numUpvalues;
		public byte numParameters;
		public VarArg varArgFlag;
		public byte maxStackSize;

		public List<Lua.Instruction> instructions;
		public List<Lua.Constant> constants;
		public List<Function> functions;
		public List<uint> sourceLinePositions;      // Debug data
		public List<Lua.Local> locals;              // Debug data
		public List<string> upvalues;               // Debug data;
	}
}
