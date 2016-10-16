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
		public int lineNumber;
		public int lastLineNumber;
		public byte numUpvalues;
		public byte numParameters;
		public VarArg varArgFlag;
		public byte maxStackSize;

		public List<Instruction> instructions;
		public List<Constant> constants;
		public List<Function> functions;

		// Debug data
		public List<int> sourceLinePositions;
		public List<Local> locals;
		public List<string> upvalues;
	}
}
