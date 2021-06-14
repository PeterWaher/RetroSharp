﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// Contains information about a state variable.
	/// </summary>
	public class UPnPStateVariable
	{
		private readonly XmlElement xml;
		private readonly string[] allowedValues;
		private readonly string name;
		private readonly string dataType;
		private readonly string defaultValue;
		private readonly string minimum;
		private readonly string maximum;
		private readonly string step;
		private readonly bool sendsEvents;
		private readonly bool hasAllowedValues = false;
		private readonly bool hasAllowedValueRange = false;

		internal UPnPStateVariable(XmlElement Xml)
		{
			List<string> AllowedValues = new List<string>();

			this.xml = Xml;

			this.sendsEvents = (Xml.HasAttribute("sendEvents") && Xml.GetAttribute("sendEvents") == "yes");

			foreach (XmlNode N in Xml.ChildNodes)
			{
				switch (N.LocalName)
				{
					case "name":
						this.name = N.InnerText;
						break;

					case "dataType":
						this.dataType = N.InnerText;
						break;

					case "defaultValue":
						this.defaultValue = N.InnerText;
						break;

					case "allowedValueList":
						this.hasAllowedValues = true;
						foreach (XmlNode N2 in N.ChildNodes)
						{
							if (N2.LocalName == "allowedValue")
								AllowedValues.Add(N2.InnerText);
						}
						break;

					case "allowedValueRange":
						this.hasAllowedValueRange = true;
						foreach (XmlNode N2 in N.ChildNodes)
						{
							switch (N2.LocalName)
							{
								case "minimum":
									this.minimum = N2.InnerText;
									break;

								case "maximum":
									this.maximum = N2.InnerText;
									break;

								case "step":
									this.step = N2.InnerText;
									break;
							}
						}
						break;
				}
			}

			this.allowedValues = AllowedValues.ToArray();
		}

		/// <summary>
		/// Underlying XML definition.
		/// </summary>
		public XmlElement Xml
		{
			get { return this.xml; }
		}

		/// <summary>
		/// State Variable Name
		/// </summary>
		public string Name { get { return this.name; } }

		/// <summary>
		/// Data Type
		/// </summary>
		public string DataType { get { return this.dataType; } }

		/// <summary>
		/// Default Value
		/// </summary>
		public string DefaultValue { get { return this.defaultValue; } }

		/// <summary>
		/// If state variable sends events.
		/// </summary>
		public bool SendsEvents { get { return this.sendsEvents; } }

		/// <summary>
		/// List of allowed values. Provided if <see cref="HasALlowedValues"/> is true.
		/// </summary>
		public string[] AllowedValues { get { return this.allowedValues; } }

		/// <summary>
		/// If <see cref="AllowedValues"/> contains a list of allowed values.
		/// </summary>
		public bool HasAllowedValues { get { return this.hasAllowedValues; } }

		/// <summary>
		/// If <see cref="Minimum"/>, <see cref="Maximum"/> and <see cref="Step"/> defines a range of allowed values.
		/// </summary>
		public bool HasAllowedValueRange { get { return this.hasAllowedValueRange; } }

		/// <summary>
		/// Smallest value allowed. Provided if <see cref="HasAllowedValueRange"/> is true.
		/// </summary>
		public string Minimum { get { return this.minimum; } }

		/// <summary>
		/// Largest value allowed. Provided if <see cref="HasAllowedValueRange"/> is true.
		/// </summary>
		public string Maximum { get { return this.maximum; } }

		/// <summary>
		/// Step value. Provided if <see cref="HasAllowedValueRange"/> is true.
		/// </summary>
		public string Step { get { return this.step; } }

		/// <summary>
		/// Converts a value to its XML string representation.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>XML string representation.</returns>
		public string ValueToXmlString(object Value)
		{
			switch (this.dataType)
			{
				case "ui1":
				case "ui2":
				case "ui4":
				case "i1":
				case "i2":
				case "i4":
				case "int":
				case "char":
				case "string":
				case "uri":
				case "uuid":
				default:
					return Value.ToString();

				case "r4":
				case "r8":
				case "number":
				case "float":
					if (!(Value is double d))
					{
						if (Value is float f)
							d = f;
						else if (Value is decimal d2)
							d = (double)d2;
						else
							d = Convert.ToDouble(Value);
					}

					return d.ToString().Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");

				case "fixed.14.4":

					if (Value is double d3)
						d = d3;
					else if (Value is float f2)
						d = f2;
					else if (Value is decimal d4)
						d = (double)d4;
					else
						d = Convert.ToDouble(Value);

					return d.ToString("F4").Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");

				case "date":
					if (!(Value is DateTime DT))
						DT = Convert.ToDateTime(Value);

					return DT.ToString("yyyyMMdd");

				case "dateTime":
					if (!(Value is DateTime DT2))
						DT2 = Convert.ToDateTime(Value);

					return DT2.ToString("yyyyMMddTHHmmss");

				case "dateTime.tz":
					if (Value is DateTimeOffset DTO)
					{
						string s = DTO.ToString("yyyyMMddTHHmmss");
						TimeSpan Zone = DTO.Offset;

						if (Zone < TimeSpan.Zero)
						{
							s += "-";
							Zone = -Zone;
						}
						else

							s += "+";

						return s + Zone.Hours.ToString("D2") + ":" + Zone.Minutes.ToString("D2");
					}
					else
					{
						if (!(Value is DateTime DT3))
							DT3 = Convert.ToDateTime(Value);

						return DT3.ToString("yyyyMMddTHHmmss");
					}

				case "time":
					if (Value is TimeSpan TS)
						return TS.Hours.ToString("D2") + ":" + TS.Minutes.ToString("D2") + ":" + TS.Seconds.ToString("D2");
					else if (Value is DateTime DT4)
						return DT4.ToString("HH:mm:ss");
					else if (TimeSpan.TryParse(Value.ToString(), out TS))
						return TS.Hours.ToString("D2") + ":" + TS.Minutes.ToString("D2") + ":" + TS.Seconds.ToString("D2");
					else
					{
						DT = Convert.ToDateTime(Value);
						return DT.ToString("HH:mm:ss");
					}

				case "time.tz":
					if (Value is TimeSpan TS2)
						return TS2.Hours.ToString("D2") + ":" + TS2.Minutes.ToString("D2") + ":" + TS2.Seconds.ToString("D2");
					else if (Value is DateTime DT5)
						return DT5.ToString("HH:mm:ss");
					else if (Value is DateTimeOffset DTO2)
					{
						string s = DTO2.ToString("HH:mm:ss");
						TimeSpan Zone = DTO2.Offset;

						if (Zone < TimeSpan.Zero)
						{
							s += "-";
							Zone = -Zone;
						}
						else

							s += "+";

						return s + Zone.Hours.ToString("D2") + ":" + Zone.Minutes.ToString("D2");
					}
					else if (TimeSpan.TryParse(Value.ToString(), out TS))
						return TS.Hours.ToString("D2") + ":" + TS.Minutes.ToString("D2") + ":" + TS.Seconds.ToString("D2");
					else
					{
						DT = Convert.ToDateTime(Value);
						return DT.ToString("HH:mm:ss");
					}

				case "boolean":
					if (!(Value is bool b))
						b = Convert.ToBoolean(Value);

					return b ? "1" : "0";

				case "bin.base64":
					if (!(Value is byte[] Bin))
						Bin = SerializeToBinary(Value);

					return Convert.ToBase64String(Bin, Base64FormattingOptions.None);

				case "bin.hex":
					Bin = Value as byte[];
					if (Bin is null)
						Bin = SerializeToBinary(Value);

					StringBuilder sb = new StringBuilder();

					foreach (byte b2 in Bin)
						sb.Append(b2.ToString("X2"));

					return sb.ToString();
			}
		}

		/// <summary>
		/// Converts a value from its XML string representation to an object value with the correct type.
		/// </summary>
		/// <param name="Value">String Value</param>
		/// <returns>Parsed value.</returns>
		public object XmlStringToValue(string Value)
		{
			switch (this.dataType)
			{
				case "ui1":
					return byte.Parse(Value);

				case "ui2":
					return ushort.Parse(Value);

				case "ui4":
					return uint.Parse(Value);

				case "i1":
					return sbyte.Parse(Value);

				case "i2":
					return short.Parse(Value);

				case "i4":
					return int.Parse(Value);

				case "int":
					return long.Parse(Value);

				case "char":
					return Value[0];

				case "string":
					return Value;

				case "uri":
					return new Uri(Value);

				case "uuid":
					return new Guid(Value);

				case "r4":
					return float.Parse(Value.Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));

				case "r8":
				case "number":
				case "float":
				case "fixed.14.4":
					return double.Parse(Value.Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));

				case "date":
					return DateTime.Parse(Value);

				case "dateTime":
					return DateTime.Parse(Value.Replace("T", " "));

				case "dateTime.tz":
					return DateTimeOffset.Parse(Value.Replace("T", " "));

				case "time":
				case "time.tz":
					return TimeSpan.Parse(Value.Replace("T", " "));

				case "boolean":
					Value = Value.ToLower();
					return (Value == "1" || Value == "true" || Value == "yes");

				case "bin.base64":
					return System.Convert.FromBase64String(Value);

				case "bin.hex":
					int i, c = Value.Length;
					if ((c & 1) == 1)
						throw new Exception("Invalid bin.hex value");

					byte[] Bin = new byte[c / 2];
					int j;
					byte b;
					char ch;

					for (i = j = 0; i < c; )
					{
						ch = Value[i++];

						if (ch >= '0' && ch <= '9')
							b = (byte)(ch - '0');
						else if (ch >= 'A' && ch <= 'F')
							b = (byte)(ch - 'A' + 10);
						else if (ch >= 'a' && ch <= 'f')
							b = (byte)(ch - 'a' + 10);
						else
							throw new Exception("Invalid bin.hex value");

						b <<= 4;
						ch = Value[i++];

						if (ch >= '0' && ch <= '9')
							b |= (byte)(ch - '0');
						else if (ch >= 'A' && ch <= 'F')
							b |= (byte)(ch - 'A' + 10);
						else if (ch >= 'a' && ch <= 'f')
							b |= (byte)(ch - 'a' + 10);
						else
							throw new Exception("Invalid bin.hex value");

						Bin[j++] = b;
					}

					return Bin;

				default:
					return Value;
			}
		}

		/// <summary>
		/// Serializes an object to an array of bytes.
		/// </summary>
		/// <param name="Value">Value</param>
		/// <returns>Binary serialization.</returns>
		public static byte[] SerializeToBinary(object Value)
		{
			BinaryFormatter Formatter = new BinaryFormatter();

			using (MemoryStream ms = new MemoryStream())
			{
				Formatter.Serialize(ms, Value);

				ms.Capacity = (int)ms.Position;
				return ms.GetBuffer();
			}
		}
	}
}
