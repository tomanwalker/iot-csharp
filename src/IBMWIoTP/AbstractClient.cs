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
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
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
    ///     A client that handles connections with the IBM Watson IoT Platform. <br>
    ///     This is an abstract class which has to be extended
    /// </summary>
    public abstract class AbstractClient
    {
        private string clientUsername;
        private string clientPassword;
        private string clientId;
        private string orgId;
        private ILog log = log4net.LogManager.GetLogger(typeof(DeviceManagement));
        
        protected static readonly String CLIENT_ID_DELIMITER = ":";
        protected static readonly String DOMAIN = ".messaging.internetofthings.ibmcloud.com";
        protected static readonly int MQTTS_PORT = 8883;
        protected MqttClient mqttClient;                                                             
        
        /// <summary>
        ///     Note that this class does not have a default constructor <br>
	    /// </summary>
        /// <param name="orgid"></param>
        ///     object of String which denotes OrgId 
        /// <param name="clientId"></param>
        ///     object of String which denotes clientId 
        /// <param name="userName"></param>
        ///     object of String which denotes userName 
        /// <param name="password"></param>
        ///     object of String which denotes password 
        public AbstractClient(string orgid, string clientId, string userName, string password)
        {
            this.clientId = clientId;
            this.clientUsername = userName;
            this.clientPassword = password;
            this.orgId = orgid;
            String now = DateTime.Now.ToString(".yyyy.MM.dd-THH.mm.fff");

            string hostName = orgid + DOMAIN;

            log.Info("hostname is :" + hostName);

            if (orgid == "quickstart")
            {
                mqttClient = new MqttClient(hostName);
            }
            else
            {
                mqttClient = new MqttClient(hostName);
            }
        }

       

        /// <summary>
        ///     Connect the device from the IBM Watson IoT Platform
        /// </summary>
        public virtual void connect()
        {
            try 
            {
                
                if(orgId == "quickstart"){
                    mqttClient.Connect(clientId);
                }
                else
                {
                	mqttClient.Connect(clientId, clientUsername, clientPassword);
                }
                log.Info("Device Connected to IBM Watson IoT Platform");
            }
            catch (Exception e)
            {
            	log.Error("Execption has occer in connecting to MQTT",e);
            }
        }

        /// <summary>
        ///     Disconnect the device from the IBM Watson IoT Platform
        /// </summary>
        public void disconnect()
        {
             log.Info("Disconnecting from the IBM Watson IoT Platform");
            try
            {
                mqttClient.Disconnect();

                log.Info("Successfully disconnected from the IBM Watson IoT Platform");
            }
            catch (Exception e)
            {
            	log.Error("Execption has occer in disconnecting from MQTT",e);
            }
        }

        /// <summary>
        ///     Determine whether this device or application is currently connected to the IBM Internet
        ///     of Things Foundation.
        /// </summary>
        /// <returns></returns>
        ///     Whether the device or application is connected to the IBM Watson IoT Platform
        public bool isConnected()
        {
            return mqttClient.IsConnected;
        }

        /// <summary>
        ///     Provides a human readable String representation of this Device, including the number
        ///     of messages sent and the current connect status.
        /// </summary>
        /// <returns></returns>
        ///     String representation of the Device status
        public String toString()
        {
            return "[" + clientId + "] " +  "Connected = " + isConnected();
        }

    }
}