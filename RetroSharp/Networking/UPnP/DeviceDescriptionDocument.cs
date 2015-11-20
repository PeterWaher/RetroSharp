using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// Contains the information provided in a Device Description Document, downloaded from a device in the network.
	/// </summary>
	public class DeviceDescriptionDocument
	{
		private XmlDocument xml;
		private UPnPDevice device;
		private int majorVersion;
		private int minorVersion;
		private string baseUrl;
		private Uri baseUri;

		internal DeviceDescriptionDocument(XmlDocument Xml, UPnPClient Client)
		{
			this.xml = Xml;

			if (Xml.DocumentElement != null && Xml.DocumentElement.LocalName == "root" && 
				Xml.DocumentElement.NamespaceURI == "urn:schemas-upnp-org:device-1-0")
			{
				foreach (XmlNode N in Xml.DocumentElement.ChildNodes)
				{
					switch (N.LocalName)
					{
						case "specVersion":
							foreach (XmlNode N2 in N.ChildNodes)
							{
								switch (N2.LocalName)
								{
									case "major":
										this.majorVersion = int.Parse(N2.InnerText);
										break;

									case "minor":
										this.minorVersion = int.Parse(N2.InnerText);
										break;
								}
							}
							break;

						case "URLBase":
							this.baseUrl = N.InnerText;
							this.baseUri = new Uri(this.baseUrl);
							break;

						case "device":
							this.device = new UPnPDevice((XmlElement)N, this.baseUri, Client);
							break;
					}
				}
			}
			else
				throw new Exception("Unrecognized file format.");
		}

		/// <summary>
		/// Underlying XML Document.
		/// </summary>
		public XmlDocument Xml
		{
			get { return this.xml; }
		}

		/// <summary>
		/// Major version
		/// </summary>
		public int MajorVersion
		{
			get { return this.majorVersion; }
		}

		/// <summary>
		/// Minor version
		/// </summary>
		public int MinorVersion
		{
			get { return this.minorVersion; }
		}

		/// <summary>
		/// Base URL
		/// </summary>
		public string BaseUrl
		{
			get { return this.baseUrl; }
		}

		/// <summary>
		/// Base URI
		/// </summary>
		public Uri BaseUri
		{
			get { return this.baseUri; }
		}

		/// <summary>
		/// Root device
		/// </summary>
		public UPnPDevice Device
		{
			get { return this.device; }
		}

		/// <summary>
		/// <see cref="Object.ToString()"/>
		/// </summary>
		public override string ToString()
		{
			return this.baseUrl;
		}

		/// <summary>
		/// Gets a device or embedded device, given its device type.
		/// </summary>
		/// <param name="DeviceType">Device type.</param>
		/// <returns>Device object, if found, null otherwise.</returns>
		public UPnPDevice GetDevice(string DeviceType)
		{
			if (this.device == null)
				return null;
			else
				return this.device.GetDevice(DeviceType);
		}

		/// <summary>
		/// Gets a service, given its service type.
		/// </summary>
		/// <param name="ServiceType">Service type.</param>
		/// <returns>Service object, if found, null otherwise.</returns>
		public UPnPService GetService(string ServiceType)
		{
			if (this.device == null)
				return null;
			else
				return this.device.GetService(ServiceType);
		}
	}
}
