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
		public LuaType Type
		{
			get;
			protected set;
		}

		public override abstract string ToString();
	}

	public class Constant<T> : Constant
	{
		public T Value
		{
			get;
			private set;
		}

		protected Constant(LuaType type, T value)
		{
			Type = type;
			Value = value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}

	public class NilConstant : Constant<object>
	{
		public NilConstant() : base(LuaType.Nil, null)
		{ }

		public override string ToString()
		{
			return "nil";
		}
	}

	public class BoolConstant : Constant<bool>
	{
		public BoolConstant(bool value) : base(LuaType.Bool, value)
		{ }

		public override string ToString()
		{
			return Value ? "true" : "false";
		}
	}

	public class NumberConstant : Constant<double>
	{
		public NumberConstant(double value) : base(LuaType.Number, value)
		{ }
	}

	public class StringConstant : Constant<string>
	{
		public StringConstant(string value) : base(LuaType.String, value)
		{ }

		public override string ToString()
		{
			// substring to avoid printing out NULL character
			return '\"' + Value.Substring(0, Value.Length - 1) + '\"';
		}
	}
}
