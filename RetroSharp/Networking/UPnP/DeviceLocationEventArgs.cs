﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// UPnP Device Location event handler.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void UPnPDeviceLocationEventHandler(UPnPClient Sender, DeviceLocationEventArgs e);

	/// <summary>
	/// Event arguments for completion events when downloading device description documents.
	/// </summary>
	public class DeviceLocationEventArgs
	{
		private DeviceLocation location;
		private IPEndPoint localEndPoint;
		private IPEndPoint remoteEndPoint;

		internal DeviceLocationEventArgs(DeviceLocation Location, IPEndPoint LocalEndPoint, IPEndPoint RemoteEndPoint)
		{
			this.location = Location;
			this.localEndPoint = LocalEndPoint;
			this.remoteEndPoint = RemoteEndPoint;
		}

		/// <summary>
		/// Device Location information.
		/// </summary>
		public DeviceLocation Location
		{
			get { return this.location; }
		}

		/// <summary>
		/// Local End Point
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return this.localEndPoint; }
		}

		/// <summary>
		/// Remote End Point
		/// </summary>
		public IPEndPoint RemoteEndPoint
		{
			get { return this.remoteEndPoint; }
		}

	}
}
