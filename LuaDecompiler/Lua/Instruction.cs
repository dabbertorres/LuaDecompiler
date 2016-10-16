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

		public int Data
		{
			get;
			private set;
		}

		public Op OpCode
		{
			get;
			private set;
		}

		public int A
		{
			get;
			private set;
		}

		public int B
		{
			get;
			private set;
		}

		public int C
		{
			get;
			private set;
		}

		public int Bx
		{
			get { return ((B << 9) & 0xFFE00 | C) & 0x3FFFF; }
		}

		public int sBx
		{
			get { return Bx - HalfMax18Bit; }
		}

		public bool HasBx
		{
			get;
			private set;
		}

		public bool Signed
		{
			get;
			private set;
		}

		public Instruction(int data)
		{
			Data = data;

			OpCode = (Op)(data & 0x3F);
			A = (data >> 6) & 0xFF;
			B = (data >> 23) & 0x1FF;
			C = (data >> 14) & 0x1FF;

			switch(OpCode)
			{
				case Op.Jmp:
				case Op.ForLoop:
				case Op.ForPrep:
					Signed = true;
					goto case Op.LoadK;

				case Op.LoadK:
				case Op.GetGlobal:
				case Op.SetGlobal:
				case Op.Closure:
					HasBx = true;
					break;

				default:
					HasBx = false;
					Signed = false;
					break;
			}
		}
	}
}
