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
    ///    A device class that connects the device as managed device to IBM Watson IoT Platform Connect and enables the device to perform one or more Device Management operations. Also the DeviceManagement instance can be used to do normal device operations like publishing device events and listening for commands from application.
    /// </summary>
	public class DeviceManagement : DeviceClient
	{
		// Publish MQTT topics
		string MANAGE_TOPIC = "iotdevice-1/mgmt/manage";
		string UNMANAGE_TOPIC = "iotdevice-1/mgmt/unmanage";
		string UPDATE_LOCATION_TOPIC = "iotdevice-1/device/update/location";
		string ADD_ERROR_CODE_TOPIC = "iotdevice-1/add/diag/errorCodes";
		string CLEAR_ERROR_CODES_TOPIC = "iotdevice-1/clear/diag/errorCodes";
		string NOTIFY_TOPIC = "iotdevice-1/notify";
		string RESPONSE_TOPIC = "iotdevice-1/response";
		string ADD_LOG_TOPIC = "iotdevice-1/add/diag/log";
		string CLEAR_LOG_TOPIC = "iotdevice-1/clear/diag/log";
		
		public DeviceInfo deviceInfo = new DeviceInfo();
		public LocationInfo locationInfo = new LocationInfo();
		List<DMRequest> collection = new List<IBMWIoTP.DeviceManagement.DMRequest>();
		ManualResetEvent oSignalEvent = new ManualResetEvent(false);
		bool isSync = false;
		ILog log = log4net.LogManager.GetLogger(typeof(DeviceManagement));
		
		
		public DeviceManagement(string orgId, string deviceType, string deviceID, string authmethod, string authtoken):
			base(orgId,deviceType,deviceID,authmethod,authtoken)
		{
			
		}
		public DeviceManagement(string orgId, string deviceType, string deviceID, string authmethod, string authtoken,bool isSync):
			base(orgId,deviceType,deviceID,authmethod,authtoken)
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
		private void publishDM (string topic , object message,string reqId)
		{
			var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(message);
			log.Info("Device Management Request For Topic " + topic +" with payload " +json);
			collection.Add(new DMRequest(reqId,topic,json));
			mqttClient.Publish(topic, Encoding.UTF8.GetBytes(json), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE , false);
			if(this.isSync)
				oSignalEvent.WaitOne();
		}
		public string manage(int lifeTime,bool supportDeviceActions,bool supportFirmwareActions)
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
				publishDM(MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		
		}
		
		public string manage(int lifeTime,bool supportDeviceActions,bool supportFirmwareActions, Object metaData)
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
				publishDM(MANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
			
		}
		
		public string unmanage()
		{
			try{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid };
				publishDM(UNMANAGE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addErrorCode(int errorCode)
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var payload = new{
					errorCode = errorCode
				};
				var message = new { reqId =uid ,d = payload};
				publishDM(ADD_ERROR_CODE_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearErrorCode()
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(CLEAR_ERROR_CODES_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		public string addLog(string msg, string dataAsString,int severityValue)
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
				publishDM(ADD_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string clearLog()
		{
			try
			{
				string uid =Guid.NewGuid().ToString();
				var message = new { reqId =uid};
				publishDM(CLEAR_LOG_TOPIC,message,uid);
				return uid;
			}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in manage ",e);
        		return "";
        	}
		}
		
		public string setLocation( double  longitude,double latitude,double elevation,double accuracy)
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
				publishDM(UPDATE_LOCATION_TOPIC,message,uid);
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
