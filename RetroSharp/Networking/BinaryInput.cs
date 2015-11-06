﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp.Networking
{
	/// <summary>
	/// Class that helps deserialize information stored in a binary packet.
	/// </summary>
	public class BinaryInput
	{
		private MemoryStream ms;

		/// <summary>
		/// Class that helps deserialize information stored in a binary packet.
		/// </summary>
		/// <param name="Data">Binary Data</param>
		public BinaryInput(byte[] Data)
		{
			this.ms = new MemoryStream(Data);
		}

		/// <summary>
		/// Class that helps deserialize information stored in a binary packet.
		/// </summary>
		/// <param name="Data">Binary Data</param>
		public BinaryInput(MemoryStream Data)
		{
			this.ms = Data;
			this.ms.Position = 0;
		}

		/// <summary>
		/// Reads the next byte of the stream.
		/// </summary>
		/// <returns>Next byte</returns>
		/// <exception cref="EndOfStreamException">If there are no more bytes available.</exception>
		public byte ReadByte()
		{
			int i = this.ms.ReadByte();
			if (i < 0)
				throw new EndOfStreamException();

			return (byte)i;
		}

		/// <summary>
		/// Reads the next set of bytes of the stream.
		/// </summary>
		/// <param name="Length">Number of bytes to retrieve.</param>
		/// <returns>Binary block of data.</returns>
		/// <exception cref="EndOfStreamException">If there is not sufficient bytes available.</exception>
		public byte[] ReadBytes(int Length)
		{
			byte[] Result = new byte[Length];
			int i = this.ms.Read(Result, 0, Length);
			if (i < Length)
				throw new EndOfStreamException();

			return Result;
		}

		/// <summary>
		/// Reads the next string of the stream.
		/// </summary>
		/// <returns>String value.</returns>
		/// <exception cref="EndOfStreamException">If there is not sufficient bytes available.</exception>
		public string ReadString()
		{
			int Len = this.ReadByte();
			Len <<= 8;
			Len |= this.ReadByte();

			if (Len == 0)
				return string.Empty;

			byte[] Data = this.ReadBytes(Len);

			return Encoding.UTF8.GetString(Data);
		}

		/// <summary>
		/// Reads a variable-length unsigned integer from the stream.
		/// </summary>
		/// <returns>Unsigned integer.</returns>
#pragma warning disable
		public uint ReadUInt()
#pragma warning enable
		{
			byte b = this.ReadByte();
			int Offset = 0;
			uint Result = (uint)(b & 127);

			while ((b & 128) != 0)
			{
				b = this.ReadByte();
				Offset += 7;
				Result |= (uint)((b & 127) << Offset);
			}

			return Result;
		}

		/// <summary>
		/// Reads an unsignd 16-bit integer.
		/// </summary>
		/// <returns>16-bit integer.</returns>
		public ushort ReadUInt16()
		{
			ushort Result = this.ReadByte();
			Result <<= 8;
			Result |= this.ReadByte();

			return Result;
		}

		/// <summary>
		/// Current position in input stream.
		/// </summary>
		public int Position
		{
			get { return (int)this.ms.Position; }
		}

		/// <summary>
		/// Number of bytes left.
		/// </summary>
		public int BytesLeft
		{
			get { return (int)(this.ms.Length - this.ms.Position); }
		}

	}
}