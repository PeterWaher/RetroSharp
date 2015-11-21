using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// UPnP error event handler.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="Exception">Information about error received.</param>
	public delegate void UPnPExceptionEventHandler(UPnPClient Sender, Exception Exception);

	/// <summary>
	/// UPnP Device Location event handler.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="Location">Device location.</param>
	public delegate void UPnPDeviceLocationEventHandler(UPnPClient Sender, DeviceLocation Location);

	/// <summary>
	/// Implements support for the UPnP protocol, as described in:
	/// http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.0.pdf
	/// </summary>
	public class UPnPClient : IDisposable
	{
		private const int ssdpPort = 1900;
		private const int defaultMaximumSearchTimeSeconds = 10;

		private LinkedList<KeyValuePair<UdpClient, IPEndPoint>> ssdpOutgoing = new LinkedList<KeyValuePair<UdpClient, IPEndPoint>>();

		/// <summary>
		/// Implements support for the UPnP protocol, as described in:
		/// http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.0.pdf
		/// </summary>
		public UPnPClient()
		{
			UdpClient Outgoing;

			foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (Interface.OperationalStatus != OperationalStatus.Up)
					continue;

				IPInterfaceProperties Properties = Interface.GetIPProperties();
				string MulticastAddress;

				foreach (UnicastIPAddressInformation UnicastAddress in Properties.UnicastAddresses)
				{
					if (UnicastAddress.Address.AddressFamily == AddressFamily.InterNetwork && Socket.SupportsIPv4)
					{
						try
						{
							Outgoing = new UdpClient(AddressFamily.InterNetwork);
							MulticastAddress = "239.255.255.250";
							Outgoing.DontFragment = true;
						}
						catch (Exception)
						{
							continue;
						}
					}
					else if (UnicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6)
					{
						try
						{
							Outgoing = new UdpClient(AddressFamily.InterNetworkV6);
							MulticastAddress = "[FF02::C]";
						}
						catch (Exception)
						{
							continue;
						}
					}
					else
						continue;

					Outgoing.EnableBroadcast = true;
					Outgoing.MulticastLoopback = false;
					Outgoing.Ttl = 30;
					Outgoing.Client.Bind(new IPEndPoint(UnicastAddress.Address, 0));
					Outgoing.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));

					IPEndPoint EP = new IPEndPoint(IPAddress.Parse(MulticastAddress), ssdpPort);
					this.ssdpOutgoing.AddLast(new KeyValuePair<UdpClient, IPEndPoint>(Outgoing, EP));

					Outgoing.BeginReceive(this.EndReceive, Outgoing);
				}
			}
		}

		private void EndReceive(IAsyncResult ar)
		{
			try
			{
				UdpClient UdpClient = (UdpClient)ar.AsyncState;
				IPEndPoint RemoteIP = null;
				byte[] Packet = UdpClient.EndReceive(ar, ref RemoteIP);
				string Header = Encoding.ASCII.GetString(Packet);
				string[] Rows = Header.Split(CRLF, StringSplitOptions.RemoveEmptyEntries);
				int i, j, c;

				if ((c = Rows.Length) > 0 && Rows[0] == "HTTP/1.1 200 OK")
				{
					Dictionary<string, string> Headers = new Dictionary<string, string>();
					string s, Key, Value;
					string SearchTarget = string.Empty;
					string Server = string.Empty;
					string Location = string.Empty;
					string UniqueServiceName = string.Empty;

					for (i = 1; i < c; i++)
					{
						s = Rows[i];
						j = s.IndexOf(':');

						Key = s.Substring(0, j).ToUpper();
						Value = s.Substring(j + 1).TrimStart();

						Headers[Key] = Value;

						switch (Key)
						{
							case "ST":
								SearchTarget = Value;
								break;

							case "SERVER":
								Server = Value;
								break;

							case "LOCATION":
								Location = Value;
								break;

							case "USN":
								UniqueServiceName = Value;
								break;
						}
					}

					if (!string.IsNullOrEmpty(Location))
					{
						UPnPDeviceLocationEventHandler h = this.OnDeviceLocation;
						if (h != null)
						{
							DeviceLocation DeviceLocation = new DeviceLocation(this, SearchTarget, Server, Location, UniqueServiceName, Headers);
							try
							{
								h(this, DeviceLocation);
							}
							catch (Exception ex)
							{
								this.RaiseOnError(ex);
							}
						}
					}
				}

				UdpClient.BeginReceive(this.EndReceive, UdpClient);
			}
			catch (Exception ex)
			{
				this.RaiseOnError(ex);
			}
		}

		private static readonly char[] CRLF = new char[] { '\r', '\n' };

		public event UPnPDeviceLocationEventHandler OnDeviceLocation = null;

		/// <summary>
		/// Starts a search for devices on the network.
		/// </summary>
		public void StartSearch()
		{
			this.StartSearch("ssdp:all", defaultMaximumSearchTimeSeconds);
		}

		/// <summary>
		/// Starts a search for devices on the network.
		/// </summary>
		/// <param name="MaximumWaitTimeSeconds">Maximum Wait Time, in seconds. Default=10 seconds.</param>
		public void StartSearch(int MaximumWaitTimeSeconds)
		{
			this.StartSearch("ssdp:all", MaximumWaitTimeSeconds);
		}

		/// <summary>
		/// Starts a search for devices on the network.
		/// </summary>
		/// <param name="SearchTarget">Search target. (Default="ssdp:all", which searches for all types of devices.)</param>
		public void StartSearch(string SearchTarget)
		{
			this.StartSearch(SearchTarget, defaultMaximumSearchTimeSeconds);
		}

		/// <summary>
		/// Starts a search for devices on the network.
		/// </summary>
		/// <param name="SearchTarget">Search target. (Default="ssdp:all", which searches for all types of devices.)</param>
		/// <param name="MaximumWaitTimeSeconds">Maximum Wait Time, in seconds. Default=10 seconds.</param>
		public void StartSearch(string SearchTarget, int MaximumWaitTimeSeconds)
		{
			foreach (KeyValuePair<UdpClient, IPEndPoint> P in this.ssdpOutgoing)
			{
				string MSearch = "M-SEARCH * HTTP/1.1\r\n" +
					"HOST: " + P.Value.ToString() + "\r\n" +
					"MAN:\"ssdp:discover\"\r\n" +
					"ST: " + SearchTarget + "\r\n" +
					"MX:" + MaximumWaitTimeSeconds.ToString() + "\r\n\r\n";
				byte[] Packet = Encoding.ASCII.GetBytes(MSearch);

				this.SendPacket(P.Key, P.Value, Packet);
			}
		}

		private void SendPacket(UdpClient Client, IPEndPoint Destination, byte[] Packet)
		{
			Client.BeginSend(Packet, Packet.Length, Destination, this.EndSend, Client);
		}

		private void EndSend(IAsyncResult ar)
		{
			try
			{
				UdpClient UdpClient = (UdpClient)ar.AsyncState;
				UdpClient.EndSend(ar);
			}
			catch (Exception ex)
			{
				this.RaiseOnError(ex);
			}
		}

		private void RaiseOnError(Exception ex)
		{
			UPnPExceptionEventHandler h = this.OnError;
			if (h != null)
			{
				try
				{
					h(this, ex);
				}
				catch (Exception)
				{
					// Ignore
				}
			}
		}

		/// <summary>
		/// Event raised when an error occurs.
		/// </summary>
		public event UPnPExceptionEventHandler OnError = null;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			foreach (KeyValuePair<UdpClient, IPEndPoint> P in this.ssdpOutgoing)
			{
				try
				{
					P.Key.Close();
				}
				catch (Exception)
				{
					// Ignore
				}
			}

			this.ssdpOutgoing.Clear();
		}

		/// <summary>
		/// Gets the device description document from a device in the network. 
		/// This method is the synchronous version of <see cref="StartGetDevice"/>.
		/// </summary>
		/// <param name="Location">URL of the Device Description Document.</param>
		/// <returns>Device Description Document.</returns>
		/// <exception cref="TimeoutException">If the document could not be retrieved within the timeout time.</exception>
		/// <exception cref="Exception">If the document could not be retrieved, or could not be parsed.</exception>
		public DeviceDescriptionDocument GetDevice(string Location)
		{
			return this.GetDevice(Location, 10000);
		}

		/// <summary>
		/// Gets the device description document from a device in the network. 
		/// This method is the synchronous version of <see cref="StartGetDevice"/>.
		/// </summary>
		/// <param name="Location">URL of the Device Description Document.</param>
		/// <param name="Timeout">Timeout, in milliseconds.</param>
		/// <returns>Device Description Document.</returns>
		/// <exception cref="TimeoutException">If the document could not be retrieved within the timeout time.</exception>
		/// <exception cref="Exception">If the document could not be retrieved, or could not be parsed.</exception>
		public DeviceDescriptionDocument GetDevice(string Location, int Timeout)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			DeviceDescriptionEventArgs e = null;

			this.StartGetDevice(Location, (sender, e2) =>
				{
					e = e2;
					Done.Set();
				});

			if (!Done.WaitOne(Timeout))
				throw new TimeoutException("Timeout.");

			if (e.Exception != null)
				throw e.Exception;

			return e.DeviceDescriptionDocument;
		}

		/// <summary>
		/// Starts the retrieval of a Device Description Document.
		/// </summary>
		/// <param name="Location">URL of the Device Description Document.</param>
		/// <param name="Callback">Callback method. Will be called when the document has been downloaded, or an error has occurred.</param>
		public void StartGetDevice(string Location, DeviceDescriptionEventHandler Callback)
		{
			Uri LocationUri = new Uri(Location);
			WebClient Client = new WebClient();
			Client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDeviceCompleted);
			Client.DownloadDataAsync(LocationUri, Callback);
		}

		private void DownloadDeviceCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			DeviceDescriptionEventHandler Callback = (DeviceDescriptionEventHandler)e.UserState;
			DeviceDescriptionEventArgs e2;

			if (e.Error != null)
				e2 = new DeviceDescriptionEventArgs(e.Error, this);
			else 
			{
				try
				{
					XmlDocument Xml = new XmlDocument();
					Xml.Load(new MemoryStream(e.Result));

					DeviceDescriptionDocument Device = new DeviceDescriptionDocument(Xml, this);
					e2 = new DeviceDescriptionEventArgs(Device, this);
				}
				catch (Exception ex)
				{
					this.RaiseOnError(ex);
					e2 = new DeviceDescriptionEventArgs(e.Error, this);
				}
				finally
				{
					WebClient Client = sender as WebClient;
					if (Client != null)
						Client.Dispose();
				}
			}


			try
			{
				Callback(this, e2);
			}
			catch (Exception ex)
			{
				this.RaiseOnError(ex);
			}
		}

		/// <summary>
		/// Gets the service description document from a service in the network. 
		/// This method is the synchronous version of <see cref="StartGetService"/>.
		/// </summary>
		/// <param name="Service">Service to get.</param>
		/// <returns>Service Description Document.</returns>
		/// <exception cref="TimeoutException">If the document could not be retrieved within the timeout time.</exception>
		/// <exception cref="Exception">If the document could not be retrieved, or could not be parsed.</exception>
		public ServiceDescriptionDocument GetService(UPnPService Service)
		{
			return this.GetService(Service, 10000);
		}

		/// <summary>
		/// Gets the service description document from a service in the network. 
		/// This method is the synchronous version of <see cref="StartGetService"/>.
		/// </summary>
		/// <param name="Service">Service to get.</param>
		/// <param name="Timeout">Timeout, in milliseconds.</param>
		/// <returns>Service Description Document.</returns>
		/// <exception cref="TimeoutException">If the document could not be retrieved within the timeout time.</exception>
		/// <exception cref="Exception">If the document could not be retrieved, or could not be parsed.</exception>
		public ServiceDescriptionDocument GetService(UPnPService Service, int Timeout)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ServiceDescriptionEventArgs e = null;

			this.StartGetService(Service, (sender, e2) =>
			{
				e = e2;
				Done.Set();
			});

			if (!Done.WaitOne(Timeout))
				throw new TimeoutException("Timeout.");

			if (e.Exception != null)
				throw e.Exception;

			return e.ServiceDescriptionDocument;
		}

		/// <summary>
		/// Starts the retrieval of a Service Description Document.
		/// </summary>
		/// <param name="Service">Service object.</param>
		/// <param name="Callback">Callback method. Will be called when the document has been downloaded, or an error has occurred.</param>
		public void StartGetService(UPnPService Service, ServiceDescriptionEventHandler Callback)
		{
			WebClient Client = new WebClient();
			Client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadServiceCompleted);
			Client.DownloadDataAsync(Service.SCPDURI, new object[]{ Service, Callback });
		}

		private void DownloadServiceCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			object[] P = (object[])e.UserState;
			UPnPService Service = (UPnPService)P[0];
			ServiceDescriptionEventHandler Callback = (ServiceDescriptionEventHandler)P[1];
			ServiceDescriptionEventArgs e2;

			if (e.Error != null)
				e2 = new ServiceDescriptionEventArgs(e.Error, this);
			else
			{
				try
				{
					XmlDocument Xml = new XmlDocument();
					Xml.Load(new MemoryStream(e.Result));

					ServiceDescriptionDocument ServiceDoc = new ServiceDescriptionDocument(Xml, this, Service);
					e2 = new ServiceDescriptionEventArgs(ServiceDoc, this);
				}
				catch (Exception ex)
				{
					this.RaiseOnError(ex);
					e2 = new ServiceDescriptionEventArgs(e.Error, this);
				}
				finally
				{
					WebClient Client = sender as WebClient;
					if (Client != null)
						Client.Dispose();
				}
			}

			try
			{
				Callback(this, e2);
			}
			catch (Exception ex)
			{
				this.RaiseOnError(ex);
			}
		}


	}
}
