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
using System.Text;
using System.Dynamic;
using System.Threading;
using System.Collections.Generic;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Exceptions;

using log4net;

namespace IBMWIoTP
{
	/// <summary>
	/// A managed Gateway class, used by Gateway, to connect the Gateway and devices behind the Gateway as managed devices 
	/// to IBM Watson IoT Platform and enables the Gateway to perform one or more Device Management operations.
	/// 
	/// The device management feature enhances the IBM Watson IoT Platform service with new capabilities 
	/// for managing devices and Gateways.
	///
	/// 
	/// 1)Connect devices behind the Gateway to IBM Watson IoT Platform
	/// 2)Send and receive its own sensor data like a directly connected device,
	/// 3)Send and receive data on behalf of the devices connected to it
	/// 4)Perform Device management operations like, manage, unmanage, firmware update, reboot, 
	///    update location, Diagnostics informations, Factory Reset and etc.. 
	///    for both Gateway and devices connected to the Gateway
	/// 
	/// </summary>
	public class GatewayManagement : GatewayClient
	{
		string MANAGE_TOPIC = "iotdevice-1/type/{0}/id/{1}/mgmt/manage";
		string UNMANAGE_TOPIC = "iotdevice-1/type/{0}/id/{1}/mgmt/unmanage";
		string UPDATE_LOCATION_TOPIC = "iotdevice-1/type/{0}/id/{1}/device/update/location"; 
		string ADD_ERROR_CODE_TOPIC = "iotdevice-1/type/{0}/id/{1}/add/diag/errorCodes";
		string CLEAR_ERROR_CODES_TOPIC = "iotdevice-1/type/{0}/id/{1}/clear/diag/errorCodes";
		string NOTIFY_TOPIC = "iotdevice-1/type/{0}/id/{1}/notify";
		string RESPONSE_TOPIC = "iotdevice-1/type/{0}/id/{1}/response";
		string ADD_LOG_TOPIC = "iotdevice-1/type/{0}/id/{1}/add/diag/log";
		string CLEAR_LOG_TOPIC = "iotdevice-1/type/{0}/id/{1}/clear/diag/log";

		public DeviceInfo deviceInfo = new DeviceInfo();
		public LocationInfo locationInfo = new LocationInfo();
		
		List<DMRequest> collection = new List<DMRequest>();
		ManualResetEvent oSignalEvent = new ManualResetEvent(false);
		bool isSync = false;
		ILog log = log4net.LogManager.GetLogger(typeof(GatewayManagement));
		
		
		public GatewayManagement(string orgId, string gatewayDeviceType, string gatewayDeviceID, string authmethod, string authtoken):
			base(orgId,gatewayDeviceType,gatewayDeviceID,authmethod,authtoken)
		{
			MANAGE_TOPIC = string.Format(MANAGE_TOPIC,gatewayDeviceType,gatewayDeviceID);
			                             
		}
		public GatewayManagement(string orgId, string gatewayDeviceType, string gatewayDeviceID, string authmethod, string authtoken,bool isSync):
			base(orgId,gatewayDeviceType,gatewayDeviceID,authmethod,authtoken)
		{
			this.isSync = isSync;
		}
		public override void connect()
		{
			base.connect();
			suscribeTOManagedTopics();
		}
		
	class DMRequest
		{
			public  DMRequest()
			{
			}
			public  DMRequest(string reqId, string topic ,string json)
			{
				this.reqID = reqId;
				this.topic = topic;
				this.json =json;
			}
			public string reqID {get;set;}
			public string topic {get;set;}
			public string json {get;set;}
		}
		
		class DMResponce
		{
			public DMResponce()
			{
			}
			public string reqId {get;set;}
			public string rc {get;set;}
			
		}


		
		private void suscribeTOManagedTopics(){
			if(mqttClient.IsConnected)
			{
				
				string[] topics = { "iotdm-1/#"};
				byte[] qos = {1};
				mqttClient.Subscribe(topics, qos);
				mqttClient.MqttMsgPublishReceived += subscriptionHandler;
				log.Info("Subscribes to topic [" +topics[0] + "]");
			}
		}		
		
		public void subscriptionHandler(object sender, MqttMsgPublishEventArgs e)
        {
			try{
	            string result = System.Text.Encoding.UTF8.GetString(e.Message);
	            var serializer  = new System.Web.Script.Serialization.JavaScriptSerializer();
	            var responce = serializer.Deserialize<DMResponce>(result);
	            var itm =  collection.Find( x => x.reqID == responce.reqId );
	            if( itm is DMRequest)
	            {
	            	log.Info("["+responce.rc+"]  "+itm.topic+" of ReqId  "+ itm.reqID);
	            	if(this.mgmtCallback !=null)
	            		this.mgmtCallback(itm.reqID,responce.rc);
	            	collection.Remove(itm);
	            }
	             if(this.isSync){
		            oSignalEvent.Set();
	            	oSignalEvent.Dispose();
	            	oSignalEvent = new ManualResetEvent(false);
	            }
			}
        	catch(Exception ex)
        	{
        		log.Error("Execption has occer in subscriptionHandler ",ex);
        	}

        }
		public bool connectionStatus()
		{
			return mqttClient.IsConnected;
		}
		private void publishDM (string type,string id,string topic , object message,string reqId)
		{
			var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(message);
			topic =  string.Format(topic,type,id);
			log.Info("Device Management Request For Topic " + topic +" with payload " +json);
			collection.Add(new DMRequest(reqId,topic,json));
			mqttClient.Publish(topic, Encoding.UTF8.GetBytes(json), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE , false);
			if(this.isSync)
				oSignalEvent.WaitOne();
		}
		public string managedGateway(int lifeTime,bool supportDeviceActions,bool supportFirmwareActions)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var payload = new {
					lifetime =  lifeTime,
					supports = new {
						deviceActions =  supportDeviceActions,
						firmwareActions = supportFirmwareActions
					},
					deviceInfo = this.deviceInfo ,
					metadata = new {}
				};
				var message = new { reqId = uid , d = payload };
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		
		}
		
		public string managedGateway(int lifeTime,bool supportDeviceActions,bool supportFirmwareActions, Object metaData)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var payload = new {
					lifetime =  lifeTime,
					supports = new {
						deviceActions =  supportDeviceActions,
						firmwareActions = supportFirmwareActions
					},
					deviceInfo = this.deviceInfo ,
					metadata = metaData
				};
				var message = new { reqId =uid , d = payload };
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
			
		}
		
		public string unmanagedGateway()
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid };
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,UNMANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addGatewayErrorCode(int errorCode)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var payload = new{
					errorCode = errorCode
				};
				var message = new { reqId =uid ,d = payload};
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,ADD_ERROR_CODE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearGatewayErrorCode()
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,CLEAR_ERROR_CODES_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addGatewayLog(string msg, string dataAsString,int severityValue)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var payload = new{
					message = msg,
					timestamp = DateTime.UtcNow.ToString("o"),
					data = dataAsString,
					severity = severityValue
				};
				var message = new { reqId =uid ,d = payload};
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,ADD_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearGatewayLog()
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,CLEAR_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string setGatewayLocation( double  longitude,double latitude,double elevation,double accuracy)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				this.locationInfo.longitude = longitude;
				this.locationInfo.latitude = latitude;
				this.locationInfo.elevation = elevation;
				this.locationInfo.accuracy =accuracy;
				this.locationInfo.measuredDateTime = DateTime.Now.ToString("o");
				//this.locationInfo.updatedDateTime = this.locationInfo.measuredDateTime;
				var message = new { reqId =uid ,d = this.locationInfo };
				publishDM(this.gatewayDeviceType,this.gatewayDeviceID,UPDATE_LOCATION_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string managedDevice(string deviceType,string deviceID,int lifeTime,bool supportDeviceActions,bool supportFirmwareActions)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var payload = new {
					lifetime =  lifeTime,
					supports = new {
						deviceActions =  supportDeviceActions,
						firmwareActions = supportFirmwareActions
					},
					deviceInfo = new {} ,
					metadata = new {}
				};
				var message = new { reqId = uid , d = payload };
				publishDM(deviceType,deviceID,MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		
		}
		public string managedDevice(string deviceType,string deviceID,int lifeTime,bool supportDeviceActions,bool supportFirmwareActions,DeviceInfo info)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var payload = new {
					lifetime =  lifeTime,
					supports = new {
						deviceActions =  supportDeviceActions,
						firmwareActions = supportFirmwareActions
					},
					deviceInfo = info ,
					metadata = new {}
				};
				var message = new { reqId = uid , d = payload };
				publishDM(deviceType,deviceID,MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		
		}
		public string managedDevice(string deviceType,string deviceID,int lifeTime,bool supportDeviceActions,bool supportFirmwareActions,DeviceInfo info, Object metaData)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var payload = new {
					lifetime =  lifeTime,
					supports = new {
						deviceActions =  supportDeviceActions,
						firmwareActions = supportFirmwareActions
					},
					deviceInfo = info ,
					metadata = metaData
				};
				var message = new { reqId =uid , d = payload };
				publishDM(deviceType,deviceID,MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
			
		}
		
		public string unmanagedDevice(string deviceType,string deviceID)
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid };
				publishDM(deviceType,deviceID,UNMANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addDeviceErrorCode(string deviceType,string deviceID,int errorCode)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var payload = new{
					errorCode = errorCode
				};
				var message = new { reqId =uid ,d = payload};
				publishDM(deviceType,deviceID,ADD_ERROR_CODE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearDeviceErrorCode(string deviceType,string deviceID)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(deviceType,deviceID,CLEAR_ERROR_CODES_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addDeviceLog(string deviceType,string deviceID,string msg, string dataAsString,int severityValue)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var payload = new{
					message = msg,
					timestamp = DateTime.UtcNow.ToString("o"),
					data = dataAsString,
					severity = severityValue
				};
				var message = new { reqId =uid ,d = payload};
				publishDM(deviceType,deviceID,ADD_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearDeviceLog(string deviceType,string deviceID)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(deviceType,deviceID,CLEAR_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string setDeviceLocation(string deviceType,string deviceID,double  longitude,double latitude,double elevation,double accuracy)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				this.locationInfo.longitude = longitude;
				this.locationInfo.latitude = latitude;
				this.locationInfo.elevation = elevation;
				this.locationInfo.accuracy =accuracy;
				this.locationInfo.measuredDateTime = DateTime.Now.ToString("o");
				//this.locationInfo.updatedDateTime = this.locationInfo.measuredDateTime;
				var message = new { reqId =uid ,d = this.locationInfo };
				publishDM(deviceType,deviceID,UPDATE_LOCATION_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
        public delegate void processMgmtResponce( string reqestId, string responceCode);

        public event processMgmtResponce mgmtCallback;
    
		
	}
	
}
