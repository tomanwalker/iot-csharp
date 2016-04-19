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
using System.Text.RegularExpressions;
using log4net;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Exceptions;

namespace IBMWIoTP
{
	/// <summary>
	/// A client, used by Gateway, that simplifies the Gateway interactions with IBM Watson IoT Platform. 
	/// 
	/// Gateways are a specialized class of devices in Watson IoT Platform which serve as access points to the 
	/// Watson IoT Platform for other devices. Gateway devices have additional permission when compared to 
	/// regular devices and can perform the following  functions:
	/// 
	/// 1)Register new devices to Watson IoT Platform
	/// 2)Send and receive its own sensor data like a directly connected device,
	/// 3)Send and receive data on behalf of the devices connected to it
	/// 4)Run a device management agent, so that it can be managed, also manage the devices connected to it
	/// 
	/// Refer to the "https://docs.internetofthings.ibmcloud.com/gateways/mqtt.html"documentation for more information about the 
	/// Gateway support in Watson IoT Platform.
	/// </summary>
	public class GatewayClient : AbstractClient
	{
		protected string gatewayDeviceType ;
		protected string gatewayDeviceID;
		private string authtoken ;
		private string GATEWAY_COMMAND_PATTERN = "iot-2/type/(.+)/id/(.+)/cmd/(.+)/fmt/(.+)";
		private string GATEWAY_NOTIFICATION_PATTERN = "iot-2/type/(.+)/id/(.+)/notify";
		ILog log = log4net.LogManager.GetLogger(typeof(GatewayClient));
		
		public GatewayClient(string orgId, string gatewayDeviceType, string gatewayDeviceID, string authMethod, string authToken)
            : base(orgId, "g" + CLIENT_ID_DELIMITER + orgId + CLIENT_ID_DELIMITER + gatewayDeviceType + CLIENT_ID_DELIMITER + gatewayDeviceID, "use-token-auth", authToken)
		{
			this.gatewayDeviceID =gatewayDeviceID;
			this.gatewayDeviceType =gatewayDeviceType;
			this.authtoken = authtoken;
		}
		
		private void subscrieToGatewayCommands(){
			this.subscribeToDeviceCommands(this.gatewayDeviceType,this.gatewayDeviceID);
		}
		public void subscribeToDeviceCommands(string deviceType, string deviceId) {
			this.subscribeToDeviceCommands(deviceType,deviceId,"+");
		}
		public void subscribeToDeviceCommands(string deviceType, string deviceId, string command) {
			this.subscribeToDeviceCommands(deviceType,deviceId,command,0);
		}
		public void subscribeToDeviceCommands(string deviceType, string deviceId, string command, int qos) {
			try {
				mqttClient.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
				mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
				string newTopic = "iot-2/type/"+deviceType+"/id/"+deviceId+"/cmd/" + command + "/fmt/json";
				string[] topics = { newTopic };
				byte[] qosLevels = { (byte) qos };
		        mqttClient.Subscribe(topics, qosLevels);
		        
			} catch (Exception e) {
                log.Error("Execption has occer in subscribeToDeviceCommands",e);
			}
		}
		
		public override void connect()
		{
			base.connect();
			this.subscrieToGatewayCommands();
			this.subscribeToGatewayNotification();
		} 	
		
		public void subscribeToGatewayNotification() {
			mqttClient.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
			mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
			
			string newTopic = "iot-2/type/"+this.gatewayDeviceType +"/id/" +this.gatewayDeviceID + "/notify";
			string[] topics = { newTopic };
			byte[] qosLevels = { 2 };
			mqttClient.Subscribe(topics, qosLevels);

		}

		public bool publishGatewayEvent(string evt, object data) {
			return publishDeviceEvent(this.gatewayDeviceType, this.gatewayDeviceID, evt, data, 0);
		}
	
		public bool publishGatewayEvent(string evt, object data, int qos) {
			return publishDeviceEvent(this.gatewayDeviceType, this.gatewayDeviceID, evt, data, qos);
		}
		
	
		public bool publishDeviceEvent(string deviceType, string deviceId, string evt, object data) {
			return publishDeviceEvent(deviceType, deviceId, evt, data, 0);
		}
	
		public bool publishDeviceEvent(string deviceType, string deviceId, string evt, object data, int qos) {
			if (!isConnected()) {
				return false;
			}
			var payload = new{
				ts = DateTime.Now.ToString("o"),
				d = data
			};
			string topic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/evt/" + evt + "/fmt/json";
			var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(payload);
			mqttClient.Publish(topic, Encoding.UTF8.GetBytes(json), 0, false);
			mqttClient.MqttMsgPublished += client_MqttMsgPublished;
			return true;
		}
		private void MqttMsgReceived(MqttMsgPublishEventArgs e)
        {
          log.Info("*** Message Received.");
          log.Info("*** Topic: " + e.Topic);
          log.Info("*** Message: " + System.Text.UTF8Encoding.UTF8.GetString(e.Message));
          log.Info("");
        }
		 void client_EventPublished(Object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            try
            {
              log.Info("*** Message Received.");
              log.Info("*** Topic: " + e.Topic);
              log.Info("*** Message: " + System.Text.UTF8Encoding.UTF8.GetString(e.Message));
              log.Info("");
            }
            catch (Exception ex)
            {
                log.Error("Execption has occer in client_EventPublished",ex);
            }
        }

        void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
          log.Info("*** Message subscribed : " + e.MessageId);
        }

        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
          log.Info("*** Message published : " + e.MessageId);
        }
        
		public delegate void processCommand(string deviceType, string deviceId,string cmdName, string format, string data);
		
		public delegate void processErrorNotification(string deviceType, string deviceId,GatewayError err);

        public event processCommand commandCallback =null;
        public event processErrorNotification errorCallback =null;
        
        public void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
        	try
        	{
	            // handle message received
	            string result = System.Text.Encoding.UTF8.GetString(e.Message);
	            
	            string topic = e.Topic;
	           //log.Info(topic);
	            Match matchCmd = Regex.Match(topic, GATEWAY_COMMAND_PATTERN);
	            if (matchCmd.Success)
	            {
	            	string[] tokens = topic.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
	          		if(this.commandCallback != null)
	           			this.commandCallback(tokens[2], tokens[4],tokens[6],tokens[8], result);
	            }
	             Match matchNotification = Regex.Match(topic, GATEWAY_NOTIFICATION_PATTERN);
	            if (matchNotification.Success)
	            {
	            	string[] tokens = topic.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
          		 	var serializer  = new System.Web.Script.Serialization.JavaScriptSerializer();
        			GatewayError responce = serializer.Deserialize<GatewayError>(result);
	            	if(this.errorCallback != null)
	           			this.errorCallback(tokens[2], tokens[4],responce);
	            	
	            }
	            
            }
        	catch(Exception ex)
        	{
        		log.Error("Execption has occer in client_MqttMsgPublishReceived ",ex);
        	}
        }

	}
}
