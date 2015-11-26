using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RetroSharp;
using RetroSharp.Networking;
using RetroSharp.Networking.UPnP;
using RetroSharp.Networking.UPnP.Services;

namespace PeerToPeer
{
	[CharacterSet("Consolas", 256)]
	[Characters(120, 43)]
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
					UPnP.OnDeviceLocation += (sender, LocationEventArgs) =>
					{
						LocationEventArgs.Location.StartGetDevice((sender2, DeviceEventArgs) =>
						{
							if (DeviceEventArgs.DeviceDescriptionDocument != null)
							{
								/*foreach (UPnPService Service in DeviceEventArgs.DeviceDescriptionDocument.ServicesRecursive)
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

								UPnPService Service = DeviceEventArgs.DeviceDescriptionDocument.GetService("urn:schemas-upnp-org:service:WANIPConnection:1");
								if (Service != null)
								{
									Service.StartGetService((sender3, ServiceEventArgs) =>
									{
										WANIPConnectionV1 WANIPConnectionV1 = new WANIPConnectionV1(ServiceEventArgs.ServiceDescriptionDocument);
										Dictionary<ushort, bool> TcpPortMapped = new Dictionary<ushort, bool>();
										Dictionary<ushort, bool> UdpPortMapped = new Dictionary<ushort, bool>();
										string NewExternalIPAddress;
										string NewConnectionType;
										string NewPossibleConnectionTypes;
										bool NewRSIPAvailable;
										bool NewNATEnabled;
										string NewConnectionStatus;
										string NewLastConnectionError;
										uint NewUptime;
										ushort PortMappingIndex;
										string NewRemoteHost;
										ushort NewExternalPort;
										string NewProtocol;
										ushort NewInternalPort;
										string NewInternalClient;
										bool NewEnabled;
										string NewPortMappingDescription;
										uint NewLeaseDuration;

										Console.Out.WriteLine("Internet Gateway found.");
										Console.Out.WriteLine(new string('=', 110));

										WANIPConnectionV1.GetExternalIPAddress(out NewExternalIPAddress);
										Console.Out.WriteLine("External IP-Address:\t\t" + NewExternalIPAddress);

										WANIPConnectionV1.GetConnectionTypeInfo(out NewConnectionType, out NewPossibleConnectionTypes);
										Console.Out.WriteLine("Connection type:\t\t" + NewConnectionType);
										Console.Out.WriteLine("Possible connection types:\t" + NewPossibleConnectionTypes);

										WANIPConnectionV1.GetNATRSIPStatus(out NewRSIPAvailable, out NewNATEnabled);
										Console.Out.WriteLine("RSIPAvailable:\t\t\t" + NewRSIPAvailable.ToString());
										Console.Out.WriteLine("NATEnabled:\t\t\t" + NewRSIPAvailable.ToString());

										WANIPConnectionV1.GetStatusInfo(out NewConnectionStatus, out NewLastConnectionError, out NewUptime);
										Console.Out.WriteLine("Connection Status:\t\t" + NewConnectionStatus);
										Console.Out.WriteLine("Last Connection Error:\t\t" + NewLastConnectionError);
										Console.Out.WriteLine("Uptime:\t\t\t\t" + NewUptime.ToString());

										Console.Out.WriteLine("Port mappings:");
										Console.Out.WriteLine(new string('-', 100));
										Console.Out.WriteLine("Index\tExtPort\tProt\tIntPort\tIntClient\tEnabled\tLease\tDescription");

										PortMappingIndex = 0;

										try
										{
											while (true)
											{
												WANIPConnectionV1.GetGenericPortMappingEntry(PortMappingIndex, out NewRemoteHost,
													out NewExternalPort, out NewProtocol, out NewInternalPort, out NewInternalClient,
													out NewEnabled, out NewPortMappingDescription, out NewLeaseDuration);

												Console.Out.WriteLine(PortMappingIndex.ToString() + "\t" +
													NewExternalPort.ToString() + "\t" + NewProtocol + "\t" + NewInternalPort.ToString() + "\t" +
													NewInternalClient + "\t" + NewEnabled.ToString() + "\t" +
													NewLeaseDuration.ToString() + "\t" + NewPortMappingDescription);

												switch (NewProtocol)
												{
													case "TCP":
														TcpPortMapped[NewExternalPort] = true;
														break;

													case "UDP":
														UdpPortMapped[NewExternalPort] = true;
														break;
												}

												PortMappingIndex++;
											}
										}
										catch (UPnPException)
										{
											Console.Out.WriteLine("No more entries.");
										}

										TcpListener Listener;
										IPAddress LocalAddress = LocationEventArgs.LocalEndPoint.Address;
										ushort LocalPort;
										int i;

										do
										{
											Listener = new TcpListener(LocalAddress, 0);
											Listener.Start(10);


											i = ((IPEndPoint)Listener.LocalEndpoint).Port;
											LocalPort = (ushort)(i);
											if (i < 0 || i > ushort.MaxValue || TcpPortMapped.ContainsKey(LocalPort))
											{
												Listener.Stop();
												Listener = null;
											}
										}
										while (Listener == null);

										try
										{
											Console.Out.WriteLine("Adding port mapping.");
											WANIPConnectionV1.AddPortMapping(string.Empty, LocalPort, "TCP", LocalPort, LocalAddress.ToString(),
												true, "Retro P2P connection", 0);
											Console.Out.WriteLine("Port mapping added.");

											try
											{
												WANIPConnectionV1.GetGenericPortMappingEntry(PortMappingIndex, out NewRemoteHost,
													out NewExternalPort, out NewProtocol, out NewInternalPort, out NewInternalClient,
													out NewEnabled, out NewPortMappingDescription, out NewLeaseDuration);

												Console.Out.WriteLine(PortMappingIndex.ToString() + "\t" +
													NewExternalPort.ToString() + "\t" + NewProtocol + "\t" + NewInternalPort.ToString() + "\t" +
													NewInternalClient + "\t" + NewEnabled.ToString() + "\t" +
													NewLeaseDuration.ToString() + "\t" + NewPortMappingDescription);

											}
											finally
											{
												Console.Out.WriteLine("Deleting port mapping.");
												WANIPConnectionV1.DeletePortMapping(string.Empty, LocalPort, "TCP");
												Console.Out.WriteLine("Port mapping deleted.");
											}
										}
										finally
										{
											Listener.Stop();
										}

									});
								}
							}
						});
					};

					UPnP.StartSearch("urn:schemas-upnp-org:service:WANIPConnection:1", 3);
					UPnP.StartSearch("urn:schemas-upnp-org:service:WANIPConnection:2", 3);
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