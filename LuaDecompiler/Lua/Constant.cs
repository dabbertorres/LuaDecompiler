// Representation of a constant in Lua

namespace LuaDecompiler.Lua
{
	public enum LuaType
	{
		Nil = 0,
		Bool = 1,
		Number = 3,
		String = 4,
	}

	public abstract class Constant
	{
		protected LuaType type;

		public LuaType Type
		{
			get { return type; }
		}
	}

	public class Constant<T> : Constant
	{
		private T value;

		public T Value
		{
			get { return value; }
		}

		protected Constant(LuaType t, T v)
		{
			type = t;
			value = v;
		}
	}

	public class NilConstant : Constant<object>
	{
		public NilConstant() : base(LuaType.Nil, null)
		{ }
	}

	public class BoolConstant : Constant<bool>
	{
		public BoolConstant(bool v) : base(LuaType.Bool, v)
		{ }
	}

	public class NumberConstant : Constant<double>
	{
		public NumberConstant(double v) : base(LuaType.Number, v)
		{ }
	}

	public class StringConstant : Constant<string>
	{
		public StringConstant(string v) : base(LuaType.String, v)
		{ }
	}
}
