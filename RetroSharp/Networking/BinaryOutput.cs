using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp.Networking
{
	/// <summary>
	/// Class that helps serialize information into a a binary packet.
	/// </summary>
	public class BinaryOutput
	{
		private MemoryStream ms;

		/// <summary>
		/// Class that helps serialize information into a a binary packet.
		/// </summary>
		public BinaryOutput()
		{
			this.ms = new MemoryStream();
		}

		/// <summary>
		/// Class that helps serialize information into a a binary packet.
		/// </summary>
		/// <param name="Data">Binary Data</param>
		public BinaryOutput(byte[] Data)
		{
			this.ms = new MemoryStream(Data);
			this.ms.Position = Data.Length;
		}

		/// <summary>
		/// Class that helps serialize information into a a binary packet.
		/// </summary>
		/// <param name="Data">Binary Data</param>
		public BinaryOutput(MemoryStream Data)
		{
			this.ms = Data;
		}

		/// <summary>
		/// Writes a byte to the binary output packet.
		/// </summary>
		/// <param name="Value">Value to write.</param>
		public void WriteByte(byte Value)
		{
			this.ms.WriteByte(Value);
		}

		/// <summary>
		/// Writes a block of bytes to the binary output packet.
		/// </summary>
		/// <param name="Value">Value to write.</param>
		public void WriteBytes(byte[] Value)
		{
			this.ms.Write(Value, 0, Value.Length);
		}

		/// <summary>
		/// Writes a string to the binary output packet.
		/// </summary>
		/// <param name="Value">Value to write.</param>
		public void WriteString(string Value)
		{
			Value = Value.Normalize();

			byte[] Data = Encoding.UTF8.GetBytes(Value);
			int Length = Data.Length;
			if (Length > 65535)
				throw new ArgumentException("String too long.", "Value");

			this.WriteByte((byte)(Length >> 8));
			this.WriteByte((byte)Length);
			this.WriteBytes(Data);
		}

		/// <summary>
		/// Gets the binary packet written so far.
		/// </summary>
		/// <returns>Binary packet.</returns>
		public byte[] GetPacket()
		{
			this.ms.Flush();
			this.ms.Capacity = (int)this.ms.Position;
			return this.ms.GetBuffer();
		}

		/// <summary>
		/// Writes a variable-length unsigned integer.
		/// </summary>
		/// <param name="Value">Value to write.</param>
#pragma warning disable
		public void WriteUInt(uint Value)
#pragma warning enable
		{
			while (Value >= 128)
			{
				this.ms.WriteByte((byte)(Value | 0x80));
				Value >>= 7;
			}

			this.ms.WriteByte((byte)Value);
		}

		/// <summary>
		/// Writes a 16-bit integer to the stream.
		/// </summary>
		/// <param name="Value">Value to write.</param>
		public void WriteUInt16(ushort Value)
		{
			this.ms.WriteByte((byte)(Value >> 8));
			this.ms.WriteByte((byte)Value);
		}

	}
}
