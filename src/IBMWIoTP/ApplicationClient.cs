/*
 *  Copyright (c) 2016 IBM Corporation and other Contributors.
 *
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html 
 *
 * Contributors:
 * 	 kaberi Singh - Initial Contribution
 *   Hari hara prasad Viswanathan  - updations
 */
 
using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

using uPLibrary.Networking.M2Mqtt.Messages;
using log4net;

namespace IBMWIoTP
{
    /// <author>Kaberi Singh, kabsingh@in.ibm.com</author>
    /// <date>28/08/2015 09:05:05 </date>
    /// <summary>
    ///     A client, used by application, that handles connections with the IBM Internet of Things Foundation. <br>
    ///     This is a derived class from AbstractClient and can be used by end-applications to handle connections with IBM Internet of Things Foundation.
    /// </summary>
    public class ApplciationClient : AbstractClient
    {
        ILog log = log4net.LogManager.GetLogger(typeof(ApplciationClient));
        /// <summary>
        ///     Delagate that defines command handler for arrived message 
        /// </summary>
        public delegate void processCommand(String cmdName, string format, string data);
        // event for command callback
        public event processCommand commandCallback;

        /// <summary>
        ///     Delagate that defines event handler for arrived message 
        /// </summary>
        public delegate void processEvent(String evtName, string format, string data);
        // event for event callback
        public event processEvent eventCallback;

        /// <summary>
        ///     Delagate that defines status handler for device status 
        /// </summary>
        public delegate void processDeviceStatus(String deviceType, string deviceId, string data);
        // event for device status callback
        public event processDeviceStatus deviceStatusCallback;

        /// <summary>
        ///     Delagate that defines status handler for application status 
        /// </summary>
        public delegate void processAppStatus(String appId, string data);
        // event for application status callback
        public event processAppStatus appStatusCallback;

        private static string DEVICE_EVENT_PATTERN = "iot-2/type/(.+)/id/(.+)/evt/(.+)/fmt/(.+)";
        private static string DEVICE_STATUS_PATTERN = "iot-2/type/(.+)/id/(.+)/mon";
        private static string APP_STATUS_PATTERN = "iot-2/app/(.+)/mon";
        private static string DEVICE_COMMAND_PATTERN = "iot-2/type/(.+)/id/(.+)/cmd/(.+)/fmt/(.+)";
        
        /// <summary>
        ///     A client, used by application, that handles connections with the IBM Internet of Things Foundation. <br>
        ///     This is a derived class from AbstractClient and can be used by end-applications to handle connections with IBM Internet of Things Foundation.
        /// </summary>
        public ApplciationClient(string OrgId, string appID, string apiKey, string authToken)
            : base(OrgId, "a" + CLIENT_ID_DELIMITER + OrgId + CLIENT_ID_DELIMITER + appID, apiKey, authToken)
        {

        }

        public ApplciationClient(string appID)
            : base("quickstart", "a" + CLIENT_ID_DELIMITER + "quickstart" + CLIENT_ID_DELIMITER + appID, null, null)
        {

        }

        /// <summary>
        ///     Subscribe to device status of the IBM Internet of Things Foundation. <br>
        ///     All the devices, for an org, are monitored <br>
        /// </summary>
        public void subscribeToDeviceStatus()
        {
            subscribeToDeviceStatus("+", "+");
        }

        /// <summary>
        ///     Subscribe to device status of the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="deviceType"></param>
        ///         object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///         object of String which denotes deviceId
        ///
        public void subscribeToDeviceStatus(String deviceType, String deviceId)
        {
        	try
        	{
	            String newTopic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/mon";
	            string[] topics = { newTopic };
	            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
	            mqttClient.Subscribe(topics, qosLevels);
	            mqttClient.MqttMsgPublishReceived -= client_MqttMsgArrived;
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgArrived;
	         }
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeToDeviceStatus ",e);
        	}
        }

        /// <summary>
        ///     Subscribe to device status of the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="deviceType"></param>
        ///         object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///         object of String which denotes deviceId
        /// <param name="qos"></param>
        ///     Quality of Service, in int - can have values 0,1,2
        public void subscribeToDeviceStatus(String deviceType, String deviceId, int qos)
        {
        	try
        	{
	            String newTopic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/mon";
	            string[] topics = { newTopic };
	            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
	            mqttClient.Subscribe(topics, qosLevels);
	            mqttClient.MqttMsgPublishReceived -= client_MqttMsgArrived;
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgArrived;
             }
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeToDeviceStatus ",e);
        	}
        }

        /// <summary>
        ///     Subscribe to application status of the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="appId"></param>
        ///     object of String which denotes appId
        public void subscribeToApplicationStatus(String appId)
        {
        	try
        	{
	            String newTopic = "iot-2/app/" + appId + "/mon";
	            string[] topics = { newTopic };
	            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
	            mqttClient.Subscribe(topics, qosLevels);
	            mqttClient.MqttMsgPublishReceived -= client_MqttMsgArrived;
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgArrived;
        	}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeToApplicationStatus ",e);
        	}
        }
        /// <summary>
        ///     Subscribe to application status of the IBM Internet of Things Foundation. <br>
        /// </summary>
        public void subscribeToApplicationStatus()
        {
            subscribeToApplicationStatus("+");
        }

      
        public void subscribeToDeviceEvents()
        {
            subscribeToDeviceEvents("+", "+", "+", "+", 0);
        }

        public void subscribeToDeviceEvents(String deviceType)
        {
            subscribeToDeviceEvents(deviceType, "+", "+", "+", 0);
        }

        public void subscribeToDeviceEvents(String deviceType, String deviceId, String evt)
        {
            subscribeToDeviceEvents(deviceType, deviceId, evt, "+", 0);
        } 


        /// <summary>
        ///     Subscribe to device events of the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="deviceType"></param>
        ///         object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///         object of String which denotes deviceId
        /// <param name="evt"></param>
        ///         object of String which denotes event name
        /// <param name="format"></param>
        ///         object of String which denotes format
        /// <param name="qos"></param>
        ///         Quality of Service, in int - can have values 0,1,2
        public void subscribeToDeviceEvents(string deviceType, string deviceId, string evt, string format, byte qos)
        {
        	try
        	{
	            String newTopic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/evt/" + evt + "/fmt/" + format;
	            string[] topics = { newTopic };
	            byte[] qosLevels = { qos };
	            mqttClient.Subscribe(topics, qosLevels);
	            mqttClient.MqttMsgPublishReceived -= client_MqttMsgArrived;
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgArrived;
	            mqttClient.MqttMsgSubscribed += client_MqttMsgSubscribed;
        	}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeToDeviceEvents ",e);
        	}
        }

        /// <summary>
        ///     Subscribe to device commands of the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="deviceType"></param>
        ///         object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///         object of String which denotes deviceId
        /// <param name="cmd"></param>
        ///         object of String which denotes command name
        /// <param name="format"></param>
        ///         object of String which denotes format
        /// <param name="qos"></param>
        ///         Quality of Service, in int - can have values 0,1,2
        public void subscribeToDeviceCommands(string deviceType, string deviceId, string cmd, string format, byte qos)
        {
        	try
        	{
	            String newTopic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/cmd/" + cmd + "/fmt/" + format;
	            string[] topics = { newTopic };
	            byte[] qosLevels = { qos };
	            mqttClient.Subscribe(topics, qosLevels);
	            mqttClient.MqttMsgPublishReceived -= client_MqttMsgArrived;
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgArrived;
	            mqttClient.MqttMsgSubscribed += client_MqttMsgSubscribed;
	        }
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeToDeviceEvents ",e);
        	}    
        }

        /// <summary>
        ///     Message subscription when subscribed event occurs or subscribed command executes <br>
        /// </summary>
        void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            log.Info("*** Message subscribed : " + e.MessageId);
        }

        /// <summary>
        ///     Message publish when subscribed event occurs or subscribed command executes <br>
        /// </summary>
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            log.Info("*** Message published : " + e.MessageId);
        }

        /// <summary>
        ///     Handles received message. Based on the topic received, calls respective callbacks.
        /// </summary>
        public void client_MqttMsgArrived(object sender, MqttMsgPublishEventArgs e)
        {
        	try
        	{
	            log.Info("Message published received [" + System.Text.Encoding.UTF8.GetString(e.Message) + "]");
	            string result = System.Text.Encoding.UTF8.GetString(e.Message);
	            string topic = e.Topic;
	            string[] tokens = topic.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
	           
	            Match matchEvent = Regex.Match(topic, DEVICE_EVENT_PATTERN);
	            if (matchEvent.Success)
	            {
	                log.Info("Match event..." + matchEvent.Groups[3].Value);
	                if (eventCallback != null)
	                {
	                    this.eventCallback(matchEvent.Groups[3].Value, matchEvent.Groups[4].Value, result);
	                }
	                return;
	            }
	
	            Match matchDeviceStatus = Regex.Match(topic, DEVICE_STATUS_PATTERN);
	            if (matchDeviceStatus.Success)
	            {
	                log.Info("Match device Status..." + matchDeviceStatus.Groups[1].Value);
	                if (deviceStatusCallback != null)
	                {
	                    this.deviceStatusCallback(matchDeviceStatus.Groups[1].Value, matchDeviceStatus.Groups[2].Value, result);
	                }
	                return;
	            }
	
	            Match matchAppStatus = Regex.Match(topic, APP_STATUS_PATTERN);
	            if (matchAppStatus.Success)
	            {
	                log.Info("Match app Status..." + matchAppStatus.Groups);
	                if (appStatusCallback != null)
	                {
	                    this.appStatusCallback(matchAppStatus.Groups[1].Value, result);
	                }
	                return;
	            }
	
	            Match matchCommand = Regex.Match(topic, DEVICE_COMMAND_PATTERN);
	            if (matchCommand.Success)
	            {
	                log.Info("Match command..." + matchCommand.Groups);
	                if (commandCallback != null)
	                {
	                    this.commandCallback(matchCommand.Groups[3].Value, matchCommand.Groups[4].Value, result);
	                }
	                return;
	            }
	        }
        	catch(Exception ex)
        	{
        		log.Error("Execption has occer in client_MqttMsgArrived ",ex);
        	}
        }
  
        /// <summary>
        ///     Subscribe to device events of the IBM Internet of Things Foundation. <br>
        ///     Quality of Service is set to 0 <br>
        ///     All events, of a given device type and device id, are subscribed to.
        /// </summary>
        /// <param name="deviceType"></param>
        ///     object of String which denotes deviceType 
        /// <param name="deviceId"></param>
        ///     object of String which denotes deviceId
        public void subscribeToDeviceEvents(String deviceType, String deviceId)
        {
            subscribeToDeviceEvents(deviceType, deviceId, "+", "+", 0);
        }

        /// <summary>
        ///     Publish command to the IBM Internet of Things Foundation. <br>
	    ///     Note that data is published
	    ///     at Quality of Service (QoS) 0, which means that a successful send does not guarantee
	    ///     receipt even if the publish is successful.
        /// </summary>
        /// <param name="deviceType"></param>
        ///     object of String which denotes deviceType 
        /// <param name="deviceId"></param>
        ///     object of String which denotes deviceId
        /// <param name="command"></param>
        ///     object of String which denotes command
        /// <param name="format"></param>
        ///     object of String which denotes format
        /// <param name="data"></param>
        ///     Payload data
        /// <returns></returns>
        ///     Whether the send was successful
        public bool publishCommand(String deviceType, String deviceId, String command, string format, string data)
        {
            return publishCommand(deviceType, deviceId, command, format, data, 0);
        }

        /// <summary>
        ///     Publish command to the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="deviceType"></param>
        ///     object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///     object of String which denotes deviceId
        /// <param name="command"></param>
        ///     object of String which denotes command
        /// <param name="format"></param>
        ///     object of String which denotes format
        /// <param name="data"></param>
        ///     Payload data
        /// <param name="qos"></param>
        ///     Quality of Service, in int - can have values 0,1,2
        /// <returns></returns>
        ///     Whether the send was successful.
        public bool publishCommand(String deviceType, String deviceId, String command, string format, string data, int qos)
        {
        	try
        	{
	            if (!isConnected())
	            {
	                return false;
	            }
	           
	            String timestamp = (new DateTime()).ToString();
	            
	            String topic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/cmd/" + command + "/fmt/" + format;
	
	            mqttClient.MqttMsgPublished += client_MqttMsgPublished;
	            mqttClient.Publish(topic, System.Text.Encoding.UTF8.GetBytes(data));
	
	            log.Info("Published payload [" + data + "] to topic [" + topic + "]");
	            
	            return true;
            }
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in publishCommand ",e);
        		return false;
        	}
        }

        /// <summary>
        ///     Publish event, on the behalf of a device, to the IBM Internet of Things Foundation. <br> 
        ///     Note that data is published
        ///     at Quality of Service (QoS) 0, which means that a successful send does not guarantee
        ///     receipt even if the publish is successful.
        /// </summary>
        /// <param name="deviceType"></param>
        ///     object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///     object of String which denotes deviceId
        /// <param name="evt"></param>
        ///     object of String which denotes event
        /// <param name="data"></param>
        ///     Payload data
        /// <returns></returns>
        ///     Whether the send was successful.
        public bool publishEvent(String deviceType, String deviceId, String evt, Object data)
        {
            return publishEvent(deviceType, deviceId, evt, data, 0);
        }

        /// <summary>
        ///     Publish event, on the behalf of a device, to the IBM Internet of Things Foundation. <br>
        ///     This method will attempt to create a JSON obejct out of the payload
        /// </summary>
        /// <param name="deviceType"></param>
        ///      object of String which denotes deviceType
        /// <param name="deviceId"></param>
        ///         object of String which denotes deviceId
        /// <param name="evt"></param>
        ///     object of String which denotes event
        /// <param name="data"></param>
        ///     Payload data
        /// <param name="qos"></param>
        ///     Quality of Service, in int - can have values 0,1,2
        /// <returns></returns>
        /// Whether the send was successful.

        public bool publishEvent(String deviceType, String deviceId, String evt, Object data, int qos)
        {
        	try
        	{
	            if (!isConnected())
	            {
	                return false;
	            }
	           
	            String timestamp = (new DateTime()).ToString();
	
	            String payload = "{ts:" + timestamp + "," + data + "}";
	
	            String topic = "iot-2/type/" + deviceType + "/id/" + deviceId + "/evt/" + evt + "/fmt/json";
	
	            log.Info("Published Payload [" + payload + "] to topic [" + topic + "]");
	            mqttClient.Publish(topic, System.Text.Encoding.UTF8.GetBytes(payload));
	
	            return true;
	        }
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in publishEvent ",e);
        		return false;
        	}
        }

    }

}