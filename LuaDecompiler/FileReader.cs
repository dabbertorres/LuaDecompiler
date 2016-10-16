using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuaDecompiler
{
	public struct FileHeader
	{
		public const int HeaderSize = 12;

		public const int SignatureBytes = 0x1B4C7561;
		public const byte Lua51Version = 0x51;

		public string signature;        // should be ".Lua" or SignatureBytes
		public byte version;            // 0x51 (81) for Lua 5.1
		public byte format;             // 0 for official Lua version
		public bool isLittleEndian;
		public byte intSize;            // in bytes. default 4
		public byte size_tSize;         // in bytes. default 4
		public byte instructionSize;    // in bytes. default 4
		public byte lua_NumberSize;     // in bytes. default 8
		public bool isIntegral;         // true = integral number type, false = floating point

		// default header bytes on x86:
		// 1B4C7561 51000104 04040800
	}

	public class FileReader : IDisposable
	{
		private FileStream fileStream;
		private BinaryReader reader;

		private FileHeader header;

		public FileHeader Header
		{
			get { return header; }
		}

		public FileReader(string file)
		{
			try
			{
				fileStream = new FileStream(file, FileMode.Open);
				reader = new BinaryReader(fileStream, Encoding.ASCII);
			}
			catch(FileNotFoundException fnfe)
			{
				Console.WriteLine("File " + file + " does not exist: " + fnfe);
				return;
			}

			ReadHeader();
		}

		public Lua.Function NextFunctionBlock()
		{
			long bytesLeft = fileStream.Length - 1 - fileStream.Position;

			if(bytesLeft == 0)
				return null;

			Lua.Function data = new Lua.Function();

			data.sourceName = ReadString();
			data.lineNumber = ReadInteger(header.intSize);
			data.lastLineNumber = ReadInteger(header.intSize);
			data.numUpvalues = reader.ReadByte();
			data.numParameters = reader.ReadByte();
			data.varArgFlag = (Lua.Function.VarArg)reader.ReadByte();
			data.maxStackSize = reader.ReadByte();

			data.instructions = ReadInstructions();
			data.constants = ReadConstants();
			data.functions = ReadFunctions();
			data.sourceLinePositions = ReadLineNumbers();
			data.locals = ReadLocals();
			data.upvalues = ReadUpvalues();

			return data;
		}

		public void Dispose()
		{
			reader.Dispose();
			fileStream.Dispose();
		}

		private void ReadHeader()
		{
			header = new FileHeader();

			List<byte> bytes = reader.ReadBytes(12).ToList();

			char[] sig = { (char)bytes[0], (char)bytes[1], (char)bytes[2], (char)bytes[3] };

			header.signature = new string(sig);

			if(header.signature != (char)27 + "Lua")
				throw new InvalidDataException("File does not appear to be a Lua bytecode file.");

			header.version = bytes[4];

			if(header.version != FileHeader.Lua51Version)
				throw new NotImplementedException("Only Lua 5.1 is supported.");

			header.format = bytes[5];
			header.isLittleEndian = bytes[6] != 0;
			header.intSize = bytes[7];
			header.size_tSize = bytes[8];
			header.instructionSize = bytes[9];
			header.lua_NumberSize = bytes[10];
			header.isIntegral = bytes[11] != 0;
		}

		private List<Lua.Instruction> ReadInstructions()
		{
			int numInstrs = ReadInteger(header.intSize);

			List<Lua.Instruction> instrs = new List<Lua.Instruction>(numInstrs);

			for(int i = 0; i < numInstrs; ++i)
			{
				instrs.Add(new Lua.Instruction(ReadInteger(header.instructionSize)));
			}

			return instrs;
		}

		private List<Lua.Constant> ReadConstants()
		{
			int numConsts = ReadInteger(header.intSize);

			List<Lua.Constant> consts = new List<Lua.Constant>(numConsts);

			for(int i = 0; i < numConsts; ++i)
			{
				byte type = reader.ReadByte();

				switch((Lua.LuaType)type)
				{
					case Lua.LuaType.Nil:
						consts.Add(new Lua.NilConstant());
						break;
					case Lua.LuaType.Bool:
						consts.Add(new Lua.BoolConstant(reader.ReadBoolean()));
						break;
					case Lua.LuaType.Number:
						consts.Add(new Lua.NumberConstant(ReadNumber(header.lua_NumberSize)));
						break;
					case Lua.LuaType.String:
						consts.Add(new Lua.StringConstant(ReadString()));
						break;
				}
			}

			return consts;
		}

		private List<Lua.Function> ReadFunctions()
		{
			int numFuncs = ReadInteger(header.intSize);

			List<Lua.Function> funcs = new List<Lua.Function>(numFuncs);

			for(int i = 0; i < numFuncs; ++i)
			{
				funcs.Add(NextFunctionBlock());
			}

			return funcs;
		}

		private List<int> ReadLineNumbers()
		{
			int numLinePos = ReadInteger(header.intSize);

			List<int> linePos = new List<int>(numLinePos);

			for(int i = 0; i < numLinePos; ++i)
			{
				// subtract 1 to index from 0, not 1
				linePos.Add(ReadInteger(header.intSize) - 1);
			}

			return linePos;
		}

		private List<Lua.Local> ReadLocals()
		{
			int numLocals = ReadInteger(header.intSize);

			List<Lua.Local> locals = new List<Lua.Local>((int)numLocals);

			for(int i = 0; i < numLocals; ++i)
			{
				locals.Add(new Lua.Local(ReadString(), ReadInteger(header.intSize), ReadInteger(header.intSize)));
			}

			return locals;
		}

		private List<string> ReadUpvalues()
		{
			int numUpvalues = ReadInteger(header.intSize);

			List<string> upvalues = new List<string>((int)numUpvalues);

			for(int i = 0; i < numUpvalues; ++i)
			{
				upvalues.Add(ReadString());
			}

			return upvalues;
		}

		private string ReadString()
		{
			int stringSize = ReadInteger(header.size_tSize);

			byte[] bytes = reader.ReadBytes((int)stringSize);

			char[] chars = new char[bytes.Length];

			for(int i = 0; i < bytes.Length; ++i)
			{
				chars[i] = (char)bytes[i];
			}

			return new string(chars);
		}

		private int ReadInteger(byte intSize)
		{
			byte[] bytes = reader.ReadBytes(intSize);
			int ret = 0;

			if(header.isLittleEndian)
			{
				for(int i = 0; i < intSize; ++i)
				{
					ret += bytes[i] << i * 8;
				}
			}
			else
			{
				for(int i = 0; i < intSize; ++i)
				{
					ret += bytes[i] >> i * 8;
				}
			}

			return ret;
		}

		private double ReadNumber(byte numSize)
		{
			byte[] bytes = reader.ReadBytes(numSize);
			double ret = 0;

			if(numSize == 8)
			{
				ret = BitConverter.ToDouble(bytes, 0);
			}
			else if(numSize == 4)
			{
				ret = BitConverter.ToSingle(bytes, 0);
			}
			else
			{
				throw new NotImplementedException("Uhm...");
			}

			return ret;
		}
	}
}
