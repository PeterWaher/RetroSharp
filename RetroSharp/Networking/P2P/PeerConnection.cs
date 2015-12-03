using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RetroSharp.Networking.P2P
{
	/// <summary>
	/// Event handler for binary packet events.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="Packet">Binary packet.</param>
	public delegate void BinaryEventHandler(object Sender, byte[] Packet);

	/// <summary>
	/// Maintains a peer connection
	/// </summary>
	public class PeerConnection : IDisposable
	{
		private const int BufferSize = 65536;

		private byte[] incomingBuffer = new byte[BufferSize];
		private byte[] packetBuffer = null;
		private LinkedList<byte[]> outgoingPackets = new LinkedList<byte[]>();
		private TcpClient tcpConnection;
		private NetworkStream stream;
		private object stateObject = null;
		private int readState = 0;
		private int packetSize = 0;
		private int packetSizeOffset = 0;
		private int packetPos = 0;
		private bool writing = false;
		private bool closed = false;

		internal PeerConnection(TcpClient TcpConnection)
		{
			this.tcpConnection = TcpConnection;
			this.stream = this.tcpConnection.GetStream();
		}

		/// <summary>
		/// Starts receiving on the connection.
		/// </summary>
		public void Start()
		{
			this.readState = 0;
			this.packetSize = 0;
			this.packetSizeOffset = 0;
			this.stream.BeginRead(this.incomingBuffer, 0, BufferSize, this.EndRead, null);
		}

		/// <summary>
		/// Underlying TCP connection
		/// </summary>
		public TcpClient Tcp
		{
			get { return this.tcpConnection; }
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			if (this.tcpConnection != null)
			{
				this.stream.Dispose();
				this.stream = null;

				this.tcpConnection.Close();
				this.tcpConnection = null;
			}

			this.Closed();
		}

		/// <summary>
		/// Sends a packet to the peer at the other side of the connection. Transmission is done asynchronously and is
		/// buffered if a sending operation is being performed.
		/// </summary>
		/// <param name="Packet">Packet to send.</param>
		public void Send(byte[] Packet)
		{
			int i = Packet.Length;
			int j = 0;
			int c = 1;
			byte b;

			i >>= 7;
			while (i > 0)
			{
				c++;
				i >>= 7;
			}

			i = Packet.Length;

			byte[] Packet2 = new byte[c + i];
			Array.Copy(Packet, 0, Packet2, c, i);

			do
			{
				b = (byte)(i & 127);
				i >>= 7;
				if (i > 0)
					b |= 128;

				Packet2[j++] = b;
			}
			while (i > 0);

			lock (this.outgoingPackets)
			{
				if (this.writing)
					this.outgoingPackets.AddLast(Packet2);
				else
				{
					this.writing = true;
					this.stream.BeginWrite(Packet2, 0, Packet2.Length, this.EndWrite, Packet);
				}
			}
		}

		private void EndWrite(IAsyncResult ar)
		{
			lock (this.outgoingPackets)
			{
				try
				{
					this.stream.EndWrite(ar);

					BinaryEventHandler h = this.OnSent;
					if (h != null)
					{
						try
						{
							h(this, (byte[])ar.AsyncState);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(ex.Message);
							Debug.WriteLine(ex.StackTrace.ToString());
						}
					}

					if (this.outgoingPackets.First != null)
					{
						byte[] Packet = this.outgoingPackets.First.Value;
						this.outgoingPackets.RemoveFirst();
						this.stream.BeginWrite(Packet, 0, Packet.Length, this.EndWrite, Packet);
					}
					else
						this.writing = false;
				}
				catch (Exception)
				{
					this.Closed();
				}
			}
		}

		/// <summary>
		/// Event raised when a packet has been sent.
		/// </summary>
		public event BinaryEventHandler OnSent = null;

		private void EndRead(IAsyncResult ar)
		{
			BinaryEventHandler h = null;
			int Pos;
			int NrLeft;
			byte b;

			try
			{
				int NrRead = this.stream.EndRead(ar);
				if (NrRead <= 0)
					this.Closed();
				else
				{
					Pos = 0;
					while (Pos < NrRead)
					{
						switch (this.readState)
						{
							case 0:
								b = this.incomingBuffer[Pos++];
								this.packetSize |= (b & 127) << this.packetSizeOffset;
								this.packetSizeOffset += 7;
								if ((b & 128) == 0)
								{
									this.packetBuffer = new byte[this.packetSize];
									this.packetPos = 0;
									this.readState++;
								}
								break;

							case 1:
								NrLeft = NrRead - Pos;
								if (NrLeft > this.packetSize - this.packetPos)
									NrLeft = this.packetSize - this.packetPos;

								Array.Copy(this.incomingBuffer, Pos, this.packetBuffer, this.packetPos, NrLeft);
								Pos += NrLeft;
								this.packetPos += NrLeft;

								if (this.packetPos >= this.packetSize)
								{
									h = this.OnReceived;
									if (h != null)
									{
										try
										{
											h(this, this.packetBuffer);
										}
										catch (Exception ex)
										{
											Debug.WriteLine(ex.Message);
											Debug.WriteLine(ex.StackTrace.ToString());
										}
									}

									this.readState = 0;
									this.packetSize = 0;
									this.packetSizeOffset = 0;
									this.packetBuffer = null;
								}
								break;
						}
					}

					this.stream.BeginRead(this.incomingBuffer, 0, BufferSize, this.EndRead, null);
				}
			}
			catch (Exception)
			{
				this.Closed();
			}
		}

		/// <summary>
		/// Event received when binary data has been received.
		/// </summary>
		public event BinaryEventHandler OnReceived = null;

		private void Closed()
		{
			if (!this.closed)
			{
				this.closed = true;
				this.writing = false;
				this.outgoingPackets.Clear();

				EventHandler h = this.OnClosed;
				if (h != null)
				{
					try
					{
						h(this, new EventArgs());
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.Message);
						Debug.WriteLine(ex.StackTrace.ToString());
					}
				}
			}
		}

		/// <summary>
		/// Event raised when a connection has been closed for some reason.
		/// </summary>
		public event EventHandler OnClosed = null;

		/// <summary>
		/// State object that applications can use to attach information to a connection.
		/// </summary>
		public object StateObject
		{
			get { return this.stateObject; }
			set { this.stateObject = value; }
		}
	}
}
