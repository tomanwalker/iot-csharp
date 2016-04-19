======================================
C# Client Library  - Managed Gateway
======================================
- See `iot-csharp <https://github.com/ibm-messaging/iot-csharp>`_ in GitHub


----

Introduction
-------------

Gateway plays an important role in the device management of devices connected to it. Many of these devices will be too basic to be managed. For a managed device, the device management agent on the gateway acts as a proxy for the connected device. The protocol used by the gateway to manage the connected devices is arbitrary, and there need not be a device management agent on the connected devices. It is only necessary for the gateway to ensure that devices connected to it perform their responsibilities as managed devices, performing any translation and processing required to achieve this. The management agent in gateway will act as more than a transparent tunnel between the attached device and the Watson IoT Platform.

For example, It is unlikely that a device connected to a gateway will be able to download the firmware itself. In this case, the gateway’s device management agent will intercept the request to download the firmware and perform the download to its own storage. Then, when the device is instructed to perform the upgrade, the gateway’s device management agent will push the firmware to the device and update it.

This section contains information on how gateways (and attached devices) can connect to the Internet of Things Platform Device Management service using C# and perform device management operations like firmware update, location update, and diagnostics update.
Create DeviceData
------------------------------------------------------------------------
The `device model <https://docs.internetofthings.ibmcloud.com/reference/device_model.html>`__ describes the metadata and management characteristics of a device. The device database in the IBM Watson IoT Platform Connect is the master source of device information. Applications and managed devices are able to send updates to the database such as a location or the progress of a firmware update. Once these updates are received by the IBM Watson IoT Platform Connect, the device database is updated, making the information available to applications.

The device model in the IBMWIoTP client library is represented as DeviceInfo.

The following code snippet shows how to create the mandatory object DeviceInfo along with the DeviceManagement:

.. code:: C#

  DeviceInfo simpleDeviceInfo = new DeviceInfo();
  simpleDeviceInfo.description = "My device";
  simpleDeviceInfo.deviceClass = "My device class";
  simpleDeviceInfo.manufacturer = "My device manufacturer";
  simpleDeviceInfo.fwVersion = "Device Firmware Version";
  simpleDeviceInfo.hwVersion = "Device HW Version";
  simpleDeviceInfo.model = "My device model";
  simpleDeviceInfo.serialNumber = "12345";

  GatewayManagement gwMgmtClient =new GatewayManagement(orgId,gatewayDeviceType,gatewayDeviceID,authmethod,authtoken,isSync);
	gwMgmtClient.deviceInfo = simpleDeviceInfo;
	gwMgmtClient.connect();

Each gateway and attached devices must have its own DeviceData to represent itself in the Platform. In the case of gateway, the DeviceData will be passed to the library as part of constructing the ManagedGateway instance, and in the case of attached device, the DeviceData will be passed as part of the managedDevice().

----

Construct ManagedGateway
-------------------------------------------------------------------------------
ManagedGateway - A gateway class that connects the gateway as managed gateway to IBM Watson Internet of Things Platform and enables the gateway to perform one or more Device Management operations for itself and attached devices. Also the ManagedGateway instance can be used to do normal gateway operations like publishing gateway events, attached device events and listening for commands from application.

ManagedGateway exposes 2 different constructors to support different user patterns,

**Constructor One**

Constructs a ManagedGateway instance by accepting the DeviceData and the following properties,

* Organization-ID - Your organization ID.
* Gateway-Type - The type of your gateway device.
* Gateway-ID - The ID of your gateway device.
* Authentication-Method - Method of authentication (The only value currently supported is "token").
* Authentication-Token - API key token

And isSync as optional parameters which in case of true all managed request will be performed synchronously.

All these properties are required to interact with the IBM Watson Internet of Things Platform.

The following code shows how to create a ManagedGateway instance:

.. code:: C#

  string orgId = "<your org id>";
  string gatewayDeviceType = "<your gateway device type>";
  string gatewayDeviceID = "<your gateway id>";
  string authmethod = "token";
  string authtoken = "<your gateway auth token>";
  bool isSync = true;

  GatewayManagement gwMgmtClient =new GatewayManagement(orgId,gatewayDeviceType,gatewayDeviceID,authmethod,authtoken,isSync);
  gwMgmtClient.deviceInfo = simpleDeviceInfo;
  gwMgmtClient.mgmtCallback += processMgmtResponce;
  gwMgmtClient.connect();

Register Callback
------------------------------------------------
In order to track the response of the management request we need to register a call back method.When ever an response for the manage request comes this call back function is called with two parameters

* request Id - To identify the management request
* response status - status of response

Each device management request method will return an unique request id which helps to identify the corresponding response.
Following are the status code for the device Management response,

* 200: The operation was successful.
* 400: The input message does not match the expected format, or one of the values is out of the valid range.
* 404: The topic name is incorrect, or the device is not in the database.
* 409: A conflict occurred during the device database update. To resolve this, simplify the operation is necessary.

The following code shows how to create a callback instance:

.. code:: C#

  GatewayManagement gwMgmtClient =new GatewayManagement(orgId,gatewayDeviceType,gatewayDeviceID,authmethod,authtoken,isSync);
  gwMgmtClient.deviceInfo = simpleDeviceInfo;
  gwMgmtClient.mgmtCallback += processMgmtResponce;
  gwMgmtClient.connect();

  .........
  .........
  .........

  public static void processMgmtResponce( string reqestId, string responceCode){
  			Console.WriteLine("req Id:" + reqestId +"	responceCode:"+ responceCode);
  		}

----

Manage request - gateway
-------------------------------------------------------

The gateway can invoke managedGateway() method to participate in device management activities. The manage request will initiate a connect request internally if the device is not connected to the IBM Watson Internet of Things Platform already.

Manage method will take following parameters,
* *lifetime* The length of time in seconds within which the gateway must send another **Manage** request in order to avoid being reverted to an unmanaged device and marked as dormant. If set to 0, the managed gateway will not become dormant. When set, the minimum supported setting is 3600 (1 hour).
* *supportFirmwareActions* Tells whether the gateway supports firmware actions or not. The gateway must add a firmware handler to handle the firmware requests.
* *supportDeviceActions* Tells whether the gateway supports Device actions or not. The gateway must add a Device action handler to handle the reboot and factory reset requests.
* metaData(Optional) - meta data object of the device that provide device meta information.

.. code:: C#

    gwMgmtClient.managedGateway(4000,true,true);

with meta data object:

.. code:: C#

      gwMgmtClient.managedGateway(4000,true,true,new{Key=""});


Manage request - attached devices
--------------------------------------

The gateway can invoke managedDevice() method to make the attached devices participate in the device management activities.

.. code:: C#

  gwMgmtClient.managedDevice(deviceType,deviceId,4000,true,true);

As shown, this method accepts the details of the attached device apart from the lifetime and device/firmware support parameters. The gateway can also use the overloaded managedDevice() method to specify the DeviceInfo for the attached device and method to specify the Device info and meta data Object for the attached device.

.. code:: C#

  DeviceInfo attachedDeviceInfo = new DeviceInfo();
  attachedDeviceInfo.description = "My device";
  attachedDeviceInfo.deviceClass = "My device class";
  attachedDeviceInfo.manufacturer = "My device manufacturer";
  attachedDeviceInfo.fwVersion = "Device Firmware Version";
  attachedDeviceInfo.hwVersion = "Device HW Version";
  attachedDeviceInfo.model = "My device model";
  attachedDeviceInfo.serialNumber = "1432";
  attachedDeviceInfo.descriptiveLocation ="My device location";

  gwMgmtClient.managedDevice(deviceType,deviceId,4000,true,true,attachedDeviceInfo,new{Key=""});


Refer to the `documentation <https://docs.internetofthings.ibmcloud.com/devices/device_mgmt/index.html#/manage-device#manage-device>`__ for more information about the manage operation.

----

Unmanage request - gateway
-----------------------------------------------------

A gateway can invoke unmanagedGateway() method when it no longer needs to be managed. The IBM Watson Internet of Things Platform will no longer send new device management requests for this gateway and all device management requests from the gateway (only for the gateway and not for the attached devices) will be rejected other than a **Manage** request.

.. code:: C#

	gwMgmtClient.unmanagedGateway();

Unmanage request - attached devices
-----------------------------------------------------

The gateway can invoke unmanagedDevice() method to move the attached device from managed state to unmanaged state. The IBM Watson Internet of Things Platform will no longer send new device management requests for this device and all device management requests from the gateway for this attached device will be rejected other than a **Manage** request.

.. code:: C#

	gwMgmtClient.unmanagedDevice(deviceType,deviceId);

Refer to the `documentation <https://docs.internetofthings.ibmcloud.com/devices/device_mgmt/index.html#/unmanage-device#unmanage-device>`__ for more information about the Unmanage operation.

----

Location update - gateway
-----------------------------------------------------

Gateways that can determine their location can choose to notify the IBM Watson Internet of Things Platform about location changes. The gateway can invoke one of the overloaded updateLocation() method to update the location of the device.

.. code:: C#

    double longitude = 77.5667;
    double latitude =12.9667;
    double elevation=0;
    double accuracy =10;
    gwMgmtClient.setGatewayLocation(longitude,latitude,elevation,accuracy);

Location update - attached devices
---------------------------------------

The gateway can invoke corresponding device method updateDeviceLocation() to update the location of the attached devices. The overloaded method can be used to specify the measuredDateTime and etc..

.. code:: C#

  gwMgmtClient.setDeviceLocation(deviceType,deviceId,longitude,latitude,elevation,accuracy);


Refer to the `documentation <https://docs.internetofthings.ibmcloud.com/devices/device_mgmt/index.html#/update-location#update-location>`__ for more information about the Location update.

----

Append/Clear ErrorCodes - gateway
-----------------------------------------------

Gateways can choose to notify the IBM Watson Internet of Things Platform about changes in their error status. The gateway can invoke  addErrorCode() method to add the current errorcode to Watson IoT Platform.

.. code:: C#

  gwMgmtClient.addGatewayErrorCode(12);

Also, the ErrorCodes of gateway can be cleared from IBM Watson Internet of Things Platform by calling the clearErrorCodes() method as follows:

.. code:: C#

  gwMgmtClient.clearGatewayErrorCode();



Append/Clear ErrorCodes - attached devices
-----------------------------------------------

Similarly, the gateway can invoke the corresponding device method to add/clear the errorcodes of the attached devices,

.. code:: C#

  gwMgmtClient.addDeviceErrorCode(deviceType,deviceId,12);
  gwMgmtClient.clearDeviceErrorCode(deviceType,deviceId);


----

Append/Clear Log messages - gateway
--------------------------------------
Gateways can choose to notify the IBM Watson Internet of Things Platform about changes by adding a new log entry. Log entry includes a log messages, its timestamp and severity, as well as an optional base64-encoded binary diagnostic data. The gateways can invoke addGatewayLog() method to send log messages,

.. code:: C#

  string message = "test";
  string data="data";
  int severity= 1;
  gwMgmtClient.addGatewayLog(message,data,severity);

Also, the log messages can be cleared from IBM Watson Internet of Things Platform by calling the clearLogs() method as follows:

.. code:: C#

  gwMgmtClient.clearGatewayLog();

Append/Clear Logs - attached devices
-----------------------------------------------

Similarly, the gateway can invoke the corresponding device method to add/clear the Logs of the attached devices,

.. code:: C#

 gwMgmtClient.addDeviceLog(deviceType,deviceId,message,data,severity);

and to clear the Logs of attached devices, invoke the clearDeviceLogs() method with the details of the attached device,

.. code:: C#

  gwMgmtClient.clearDeviceLog(deviceType,deviceId);


The device diagnostics operations are intended to provide information on gateway/device errors, and does not provide diagnostic information relating to the devices connection to the IBM Watson Internet of Things Platform.

Refer to the `documentation <https://docs.internetofthings.ibmcloud.com/devices/device_mgmt/index.html#/update-location#update-location>`__ for more information about the Diagnostics operation.
