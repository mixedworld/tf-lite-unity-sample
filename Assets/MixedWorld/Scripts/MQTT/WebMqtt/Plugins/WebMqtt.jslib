var LibraryWebMqtt = {
	$webMqttInstances: [],
	$mqttLibLoaded: false,

	MqttInitLibrary: function(libUrlPtr)
	{
		var libUrl = Pointer_stringify(libUrlPtr);
		console.log("Loading MQTT library from " + libUrl);
		
		var mqttScript = document.createElement('script');
		mqttScript.onload = function () {
			console.log("MQTT library loaded.");
			mqttLibLoaded = true;
		};
		mqttScript.src = libUrl;
		document.head.appendChild(mqttScript);
	},

	MqttIsLibraryInitialized: function()
	{
		return mqttLibLoaded;
	},

	MqttClientCreate: function(hostPtr, port, pathPtr, clientIdPtr, useSsl)
	{
		var host = Pointer_stringify(hostPtr);
		var path = Pointer_stringify(pathPtr);
		var clientId = Pointer_stringify(clientIdPtr);

		if(host.length == 0)
			host = location.hostname;

		mqtt = new Paho.MQTT.Client(
			host,
			port,
			path,
			clientId);

		mqtt.customConnectState = 0;
		mqtt.customLastError = null;
		mqtt.customWillMessage = null;
		mqtt.customMessageQueue = [];
		mqtt.customMessageHandleToObject = {};
		mqtt.customDequeuedMessageCount = 0;
        mqtt.customUseSsl = !!useSsl;
        
        // Debug logging the value of useSSl
        console.log("Using SSL: " + mqtt.customUseSsl);

        mqtt.onConnectionLost = function (response) {
			console.log("Connection Lost: " + response.errorMessage);
			mqtt.customConnectState = -1;
			if (response.errorCode != 0) {
				mqtt.customLastError = response.errorMessage;
			}
		};
        mqtt.onMessageArrived = function (message) {
			mqtt.customMessageQueue.push(message);
		};
		
		var handle = webMqttInstances.push(mqtt) - 1;
		return handle;
	},

	MqttClientConnect: function (handle, usernamePtr, passwordPtr)
	{
		var username = usernamePtr ? Pointer_stringify(usernamePtr) : null;
		var password = passwordPtr ? Pointer_stringify(passwordPtr) : null;

		var mqtt = webMqttInstances[handle];
		var options = {
			timeout: 3,
			useSSL: mqtt.customUseSsl,
			cleanSession: true,
			onSuccess: function() {
				if(mqtt.customConnectState == 0)
					mqtt.disconnect();
				else
					mqtt.customConnectState = 2;
			},
			onFailure: function (message) {
				console.log("Connection Failure: " + message.errorMessage);
				mqtt.customConnectState = -1;
				mqtt.customLastError = message.errorMessage;
			}
		};

		if (mqtt.customWillMessage != null)
		{
			options.willMessage = mqtt.customWillMessage;
		}
		
		// Authenticate, if credentials were specified
		if (username && password)
		{
			options.userName = username;
			options.password = password;
		}

		mqtt.customConnectState = 1;
		mqtt.connect(options);

		return;
	},

	MqttClientDisconnect: function (handle)
	{
		var mqtt = webMqttInstances[handle];

		if(mqtt.customConnectState == 2)
				mqtt.disconnect();

		mqtt.customConnectState = 0;

		return;
	},

	MqttClientGetConnectState: function (handle)
	{
		var mqtt = webMqttInstances[handle];
		return mqtt.customConnectState;
	},

	MqttClientGetLastError: function (handle)
	{
		var mqtt = webMqttInstances[handle];

		if (mqtt.customLastError == null)
			return null;

		var bufferSize = lengthBytesUTF8(mqtt.customLastError) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(mqtt.customLastError, buffer, bufferSize);

		return buffer;
	},

	MqttClientClearLastError: function (handle)
	{
		var mqtt = webMqttInstances[handle];
		mqtt.customLastError = null;
	},

	MqttClientSubscribe: function (handle, topicPtr, qualityOfService)
	{
		var mqtt = webMqttInstances[handle];
		var topic = Pointer_stringify(topicPtr);
		var options = {
			qos: qualityOfService
		};

		mqtt.subscribe(topic, options);
		return;
	},

	MqttClientUnsubscribe: function (handle, topicPtr)
	{
		var mqtt = webMqttInstances[handle];
		var topic = Pointer_stringify(topicPtr);
		var options = {};

		mqtt.unsubscribe(topic, options);
		return;
	},
	
	MqttClientSetLastWillMessage: function (handle, topicPtr, payloadPtr, payloadLength, qos, retained)
	{
		var mqtt = webMqttInstances[handle];
		var topic = Pointer_stringify(topicPtr);
		var payload = HEAPU8.buffer.slice(payloadPtr, payloadPtr + payloadLength);

		// Make sure we have a boolean - C# bool values appear to be marshalled as ints.
		var retainedBool = !!retained;

		if (topic.length == 0)
		{
			mqtt.customWillMessage = null;
		}
		else
		{
			mqtt.customWillMessage = new Paho.MQTT.Message(payload);
			mqtt.customWillMessage.destinationName = topic;
			mqtt.customWillMessage.qos = qos;
			mqtt.customWillMessage.retained = retainedBool;
		}
	},

	MqttClientPublish: function (handle, topicPtr, payloadPtr, payloadLength, qos, retained)
	{
		var mqtt = webMqttInstances[handle];
		var topic = Pointer_stringify(topicPtr);
		var payload = HEAPU8.buffer.slice(payloadPtr, payloadPtr + payloadLength);

		// Make sure we have a boolean - C# bool values appear to be marshalled as ints.
		var retainedBool = !!retained;

		mqtt.send(topic, payload, qos, retainedBool);
		return;
	},
	
	MqttClientGetQueuedMessageCount: function (handle)
	{
		var mqtt = webMqttInstances[handle];
		return mqtt.customMessageQueue.length;
	},

	MqttClientDequeueMessage: function (handle)
	{
		var mqtt = webMqttInstances[handle];
		if (mqtt.customMessageQueue.length < 1)
			return -1;

		var message = mqtt.customMessageQueue[0];
		var messageHandle = mqtt.customDequeuedMessageCount;
		var messageKey = String(messageHandle);

		mqtt.customDequeuedMessageCount++;
		mqtt.customMessageHandleToObject[messageKey] = message;
		mqtt.customMessageQueue.shift();
		
		return messageHandle;
	},

	MqttClientGetMessagePayloadLength: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];

		return message.payloadBytes.length;
	},

	MqttClientGetMessagePayload: function (handle, messageHandle, payloadBufferPtr, payloadBufferLength)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];

		var bytes = message.payloadBytes.length;
		if (bytes > payloadBufferLength)
			return -1;

		HEAPU8.set(message.payloadBytes, payloadBufferPtr);
		return bytes;
	},
	
	MqttClientGetMessageChannel: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];

		var bufferSize = lengthBytesUTF8(message.destinationName) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(message.destinationName, buffer, bufferSize);

		return buffer;
	},
	
	MqttClientGetMessageRetained: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];

		return message.retained;
	},
	
	MqttClientGetMessageQualityOfService: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];
		
		return message.qos;
	},
	
	MqttClientGetMessageDuplicate: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		var message = mqtt.customMessageHandleToObject[messageKey];
		
		return message.duplicate;
	},
	
	MqttClientDeleteMessage: function (handle, messageHandle)
	{
		var mqtt = webMqttInstances[handle];
		var messageKey = String(messageHandle);
		delete mqtt.customMessageHandleToObject[messageKey];
	}
};

autoAddDeps(LibraryWebMqtt, '$webMqttInstances');
autoAddDeps(LibraryWebMqtt, '$mqttLibLoaded');
mergeInto(LibraryManager.library, LibraryWebMqtt);
