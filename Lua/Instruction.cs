// Representation of a Lua VM Instruction

namespace LuaDecompiler.Lua
{
	public class Instruction
	{
		public enum Op
		{
			Move,       // copy value between registers
			LoadK,      // Load constant to register
			LoadBool,   // load bool to register
			LoadNil,    // load nil values into a range of registers
			GetUpVal,   // read upvalue into register
			GetGlobal,  // read global into register
			GetTable,   // read table element into register
			SetGlobal,  // write register value to a global variable
			SetUpVal,   // write register value to an upvalue
			SetTable,   // write register value into table element
			NewTable,   // create a new table
			Self,       // prepare object for method call
			Add,        // addition
			Sub,        // subtraction
			Mul,        // multiplication
			Div,        // division
			Mod,        // modulus
			Pow,        // exponentiation
			Unm,        // unary minus
			Not,        // logical not
			Len,        // length operator
			Concat,     // concatenate a range of registers
			Jmp,        // unconditional jump
			Eq,         // equality test
			Lt,         // less than test
			Le,         // less than or equal test
			Test,       // boolean test with conditional jump
			TestSet,    // boolean test with conditional jump and assignment
			Call,       // call a closure
			TailCall,   // perform a tail call
			Return,     // return from function call
			ForLoop,    // iterate a numeric for loop
			ForPrep,    // initialization for a numeric for loop
			TForLoop,   // iterate a generic for loop
			SetList,    // set a range of array elements for a table
			Close,      // close a range of locals being used as upvalues
			Closure,    // create a closure of a function
			VarArg,     // assign vararg function args to registers
		}

		private const int HalfMax18Bit = 2 << 17;	// == 2^18 / 2 == 131071

		private uint data;
		
		private Op opcode;

		private uint a;
		private uint b;
		private uint c;

		private bool makeBx;
		private bool signed;

		public uint Data
		{
			get { return data; }
		}

		public Op OpCode
		{
			get { return opcode; }
		}

		public uint A
		{
			get { return a; }
		}

		public uint B
		{
			get { return b; }
		}

		public uint C
		{
			get { return c; }
		}

		public uint Bx
		{
			get { return ((b << 9) & 0xFFE00 | c) & 0x3FFFF; }
		}

		public int sBx
		{
			get { return (int)Bx - HalfMax18Bit; }
		}

		public bool HasBx
		{
			get { return makeBx; }
		}

		public bool Signed
		{
			get { return signed; }
		}

		public Instruction(uint dat)
		{
			data = dat;

			opcode = (Op)(dat & 0x3F);
			a = (dat >> 6) & 0xFF;
			b = (dat >> 23) & 0x1FF;
			c = (dat >> 14) & 0x1FF;

			switch(opcode)
			{
				case Op.Jmp:
				case Op.ForLoop:
				case Op.ForPrep:
					signed = true;
					goto case Op.LoadK;

				case Op.LoadK:
				case Op.GetGlobal:
				case Op.SetGlobal:
				case Op.Closure:
					makeBx = true;
					break;

				default:
					makeBx = false;
					signed = false;
					break;
			}
		}
	}
}
