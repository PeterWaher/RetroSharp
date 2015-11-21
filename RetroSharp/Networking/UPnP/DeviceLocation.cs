using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// Contains information about the location of a device on the network.
	/// </summary>
	public class DeviceLocation : IEnumerable<KeyValuePair<string, string>>
	{
		private UPnPClient client;
		private Dictionary<string, string> headers;
		private string searchTarget;
		private string server;
		private string location;
		private string uniqueServiceName;

		/// <summary>
		/// Contains information about the location of a device on the network.
		/// </summary>
		/// <param name="SearchTarget">SSDP Search Target</param>
		/// <param name="Server">Server</param>
		/// <param name="Location">Location of device information</param>
		/// <param name="UniqueServiceName">Unique Service Name (USN)</param>
		/// <param name="Headers">All headers in response.</param>
		internal DeviceLocation(UPnPClient Client, string SearchTarget, string Server, string Location, string UniqueServiceName, Dictionary<string, string> Headers)
		{
			this.client = Client;
			this.searchTarget = SearchTarget;
			this.server = Server;
			this.location = Location;
			this.uniqueServiceName = UniqueServiceName;
			this.headers = Headers;
		}

		/// <summary>
		/// SSDP Search Target
		/// </summary>
		public string SearchTarget
		{ 
			get { return this.searchTarget; } 
		}

		/// <summary>
		/// Server
		/// </summary>
		public string Server
		{
			get { return this.server; }
		}

		/// <summary>
		/// Location of device information
		/// </summary>
		public string Location
		{
			get { return this.location; }
		}

		/// <summary>
		/// Unique Service Name (USN)
		/// </summary>
		public string UniqueServiceName
		{
			get { return this.uniqueServiceName; }
		}

		/// <summary>
		/// Gets the value of a given key.
		/// </summary>
		/// <param name="Key">SSDP Header Key</param>
		/// <returns>Value, if key is found or <see cref="string.Empty"/> if not found.</returns>
		public string this[string Key]
		{
			get
			{
				string Value;

				if (this.headers.TryGetValue(Key.ToUpper(), out Value))
					return Value;
				else
					return string.Empty;
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return this.headers.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.headers.GetEnumerator();
		}

		/// <summary>
		/// Gets the device description document from a device in the network. 
		/// This method is the synchronous version of <see cref="StartGetDevice"/>.
		/// </summary>
		/// <param name="Location">URL of the Device Description Document.</param>
		/// <returns>Device Description Document.</returns>
		/// <exception cref="TimeoutException">If the document could not be retrieved within the timeout time.</exception>
		/// <exception cref="Exception">If the document could not be retrieved, or could not be parsed.</exception>
		public DeviceDescriptionDocument GetDevice()
		{
			return this.client.GetDevice(this.location);
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
		public DeviceDescriptionDocument GetDevice(int Timeout)
		{
			return this.client.GetDevice(this.location, Timeout);
		}

		/// <summary>
		/// Starts the retrieval of a Device Description Document.
		/// </summary>
		/// <param name="Callback">Callback method. Will be called when the document has been downloaded, or an error has occurred.</param>
		public void StartGetDevice(DeviceDescriptionEventHandler Callback)
		{
			this.client.StartGetDevice(this.location, Callback);
		}

		/// <summary>
		/// <see cref="Object.ToString()"/>
		/// </summary>
		public override string ToString()
		{
			return this.location;
		}
	}
}
