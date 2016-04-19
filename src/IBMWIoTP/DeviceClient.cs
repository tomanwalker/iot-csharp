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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Configuration;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using log4net;

namespace IBMWIoTP
{
    /// <author>Kaberi Singh, kabsingh@in.ibm.com</author>
    /// <date>28/08/2015 09:05:05 </date>
    /// <summary>
    ///     A client, used by device, that handles connections with the IBM Internet of Things Foundation. <br>
    ///     This is a derived class from AbstractClient and can be used by embedded devices to handle connections with IBM Internet of Things Foundation.
    /// </summary>
    public class DeviceClient : AbstractClient
    {
        ILog log = log4net.LogManager.GetLogger(typeof(DeviceClient));
        public DeviceClient(string orgId, string deviceType, string deviceID, string authmethod, string authtoken)
            : base(orgId, "d" + CLIENT_ID_DELIMITER + orgId + CLIENT_ID_DELIMITER + deviceType + CLIENT_ID_DELIMITER + deviceID, "use-token-auth", authtoken)
        {

        }

        public DeviceClient(string deviceType, string deviceID)
            : base("quickstart", "d" + CLIENT_ID_DELIMITER + "quickstart" + CLIENT_ID_DELIMITER + deviceType + CLIENT_ID_DELIMITER + deviceID, null, null)
        {

        }
        
        private void MqttMsgReceived(MqttMsgPublishEventArgs e)
        {
            log.Info("*** Message Received.");
            log.Info("*** Topic: " + e.Topic);
            log.Info("*** Message: " + System.Text.UTF8Encoding.UTF8.GetString(e.Message));
            log.Info("");
        }

        /// <summary>
        ///     Publish event to the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="evt"></param>
        ///      object of String which denotes event 
        /// <param name="format"></param>
        ///      object of String which denotes format 
        /// <param name="msg"></param>
        ///      object of String which denotes message 
        /// <param name="qosLevel"></param>
        ///     Quality of Service, in int - can have values 0,1,2
        public void publishEvent(String evt, String format, String msg, byte qosLevel)
        {
        	try{
	           	mqttClient.MqttMsgPublished += client_MqttMsgPublished;
	            string topic = "iot-2/evt/" + evt + "/fmt/" + format;
	            mqttClient.Publish(topic, Encoding.UTF8.GetBytes(msg), qosLevel, false);
	            log.Info("Published to topic [" + topic + "] msg[" + msg + "]");
        	}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in Publish Event ",e);
        	}
        	
        }

        /// <summary>
        ///     Publish event to the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="evt"></param>
        ///      object of String which denotes event 
        /// <param name="format"></param>
        ///      object of String which denotes format 
        /// <param name="msg"></param>
        ///      object of String which denotes message 
        public void publishEvent(String evt, String format, String msg)
        {
            publishEvent(evt, format, msg, 0);
        }

        /// <summary>
        ///     Subscribe command to the IBM Internet of Things Foundation. <br>
        /// </summary>
        /// <param name="cmd"></param>
        ///      object of String which denotes command 
        /// <param name="format"></param>
        ///      object of String which denotes format 
        /// <param name="qosLevel"></param>
        ///     Quality of Service, in int - can have values 0,1,2
        public void subscribeCommand(String cmd, String format, byte qosLevel)
        {
        	try{
	        	string topic = "iot-2/cmd/" + cmd + "/fmt/" + format;
	            string[] topics = { topic };
	            byte[] qos = { qosLevel };
	            mqttClient.Subscribe(topics, qos);
	
	            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
	
	            log.Info("Subscribes to topic to topic [" + topic + "]");
        	}
        	catch(Exception e)
        	{
        		log.Error("Execption has occer in subscribeCommand ",e);
        	}
            
        }

        /// <summary>
        ///     Publish command to the IBM Internet of Things Foundation. <br>
        /// </summary>
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

        public void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
        	try
        	{
	            // handle message received
	            string result = System.Text.Encoding.UTF8.GetString(e.Message);
	            
	            string topic = e.Topic;
	            string[] tokens = topic.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
	            log.Info("tokens from Device Client : " + tokens);
	            
	            this.commandCallback(tokens[2], tokens[4], result);
            }
        	catch(Exception ex)
        	{
        		log.Error("Execption has occer in client_MqttMsgPublishReceived ",ex);
        	}
        }

        public delegate void processCommand(string cmdName, string format, string data);

        public event processCommand commandCallback;
    
    }
}
