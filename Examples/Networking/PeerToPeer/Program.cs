using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RetroSharp;
using RetroSharp.Networking;
using RetroSharp.Networking.UPnP;

namespace PeerToPeer
{
	[CharacterSet("Consolas", 256)]
	[Characters(80, 43)]
	[ScreenBorder(30, 20)]
	class Program : RetroApplication
	{
		public static void Main(string[] args)
		{
			Initialize();

			try
			{
				using (UPnPClient UPnP = new UPnPClient())
				{
					UPnP.OnDeviceLocation += (sender, Location) =>
					{
						Location.StartGetDevice((sender2, e) =>
						{
							if (e.DeviceDescriptionDocument != null)
							{
								/*foreach (UPnPService Service in e.DeviceDescriptionDocument.ServicesRecursive)
								{
									ServiceDescriptionDocument Scpd = Service.GetService();
									string Xml = Scpd.Xml.InnerXml;
									string ServiceType = Scpd.Service.ServiceType;
									string FileName = ServiceType.Replace(':', '\\') + ".scpd.xml";
									string Dir = Path.GetDirectoryName(FileName);
									if (!Directory.Exists(Dir))
										Directory.CreateDirectory(Dir);
									File.WriteAllText(FileName, Xml);
									Console.Out.WriteLine(ServiceType);
								}*/

								UPnPService Service = e.DeviceDescriptionDocument.GetService("urn:schemas-upnp-org:service:WANIPConnection:1");
								if (Service != null)
								{
									Service.StartGetService((sender3, e2) =>
									{
										Dictionary<string, object> OutputValues = new Dictionary<string, object>();
										UPnPAction GetExternalIPAddress = e2.ServiceDescriptionDocument.GetAction("GetExternalIPAddress");

										GetExternalIPAddress.Invoke(out OutputValues);

										string NewExternalIPAddress = (string)OutputValues["NewExternalIPAddress"];

										Console.Out.WriteLine("NewExternalIPAddress=" + NewExternalIPAddress);
									});
								}
							}
						});
					};

					UPnP.StartSearch("urn:schemas-upnp-org:device:WANConnectionDevice:1", 3);
					//UPnP.StartSearch(10);
					Console.In.ReadLine();
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(ex.Message);
				Console.In.ReadLine();
			}


			Terminate();
		}
	}
}