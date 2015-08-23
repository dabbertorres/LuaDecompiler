// Representation of a Lua local variable

namespace LuaDecompiler.Lua
{
	public class Local
	{
		private string name;
		private uint scopeStart;
		private uint scopeEnd;

		public string Name
		{
			get { return name; }
		}

		public uint ScopeStart
		{
			get { return scopeStart; }
		}

		public uint ScopeEnd
		{
			get { return scopeEnd; }
		}

		public Local(string vn, uint ss, uint se)
		{
			name = vn;
			scopeStart = ss;
			scopeEnd = se;
		}
	}
}
