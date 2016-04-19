/*
 *  Copyright (c) 2016 IBM Corporation and other Contributors.
 *
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html 
 *
 * Contributors:
 *   Hari hara prasad Viswanathan  - Initial Contribution
 */
 
using System;

namespace IBMWIoTP
{
	/// <summary>
	/// Description of info.
	/// </summary>
	public class DeviceInfo
	{
		public DeviceInfo()
		{
		}
		public string serialNumber {get;set;}
		public string manufacturer {get;set;}
		public string model {get;set;}
		public string deviceClass {get;set;}
		public string description {get;set;}
		public string fwVersion {get;set;}
		public string hwVersion {get;set;}
		public string descriptiveLocation {get;set;}
	}
	
	public class DeviceFirmware
	{
		public DeviceFirmware(){
		}
		public string version{get;set;}
		public string name{get;set;}
		public string url{get;set;}
		public string verifier{get;set;}
		public string state{get;set;}
		public string updateStatus{get;set;}
		public string updatedDateTime{get;set;}
	}
	
	public class LocationInfo
	{
		public LocationInfo()
		{
			this.latitude = 0;
			this.longitude = 0;
			this.measuredDateTime="";
			this.elevation = 0;
			this.accuracy=0;
		}
		public double longitude {get;set;}
		public double latitude {get;set;}
		public double elevation {get;set;}
		public double accuracy {get;set;}
		public string measuredDateTime {get;set;}
		//public string updatedDateTime {get;set;}
	}
	
	public class GatewayError
	{
		public GatewayError()
		{
		}
		public string Request { get; set; }
		public string Time { get; set; }
		public string Topic { get; set; }
		public string Type { get; set; }
		public string Id { get; set; }
		public string Client { get; set; }
		public string RC { get; set; }
		public string Message { get; set; }

	}
}
