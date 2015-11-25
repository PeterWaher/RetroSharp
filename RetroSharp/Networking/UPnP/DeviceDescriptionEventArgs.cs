﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RetroSharp.Networking.UPnP
{
	/// <summary>
	/// UPnP Device Description event handler.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void DeviceDescriptionEventHandler(object Sender, DeviceDescriptionEventArgs e);

	/// <summary>
	/// Event arguments for completion events when downloading device description documents.
	/// </summary>
	public class DeviceDescriptionEventArgs
	{
		private DeviceDescriptionDocument doc;
		private Exception ex;
		private UPnPClient client;

		internal DeviceDescriptionEventArgs(DeviceDescriptionDocument Doc, UPnPClient Client)
		{
			this.client = Client;
			this.doc = Doc;
			this.ex = null;
		}

		internal DeviceDescriptionEventArgs(Exception Ex, UPnPClient Client)
		{
			this.client = Client;
			this.doc = null;
			this.ex = Ex;
		}

		/// <summary>
		/// UPnP Client.
		/// </summary>
		public UPnPClient Client
		{
			get { return this.client; }
		}

		/// <summary>
		/// Underlying XML Document.
		/// </summary>
		public DeviceDescriptionDocument DeviceDescriptionDocument
		{
			get { return this.doc; }
		}

		/// <summary>
		/// Exception object, if an error occurred.
		/// </summary>
		public Exception Exception
		{
			get { return this.ex; }
		}
	}
}
