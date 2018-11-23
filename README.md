# Masterloop Application Plugin

This repo contains source code to the Masterloop Application Plugin library.

## How to build

### Prerequisites
- Visual Studio
- Windows, Mac or Linux
- Git
- .NET Framework, Mono, .NET Core

## Feedback

File bugs on [Masterloop Home](https://github.com/orgs/Masterloop/projects/1).

## License

Unless explicitly stated otherwise all files in this repository are licensed under the License in the root repository.

## Using the Masterloop IoT Platform HTTP(S) REST interface

### Creating a server connection object
```
IMasterloopServerConnection mcs = new MasterloopServerConnection("hostname", "username", "password");
```
where
- "hostname" is the name of your Masterloop server
- "username" is your user name
- "password" is your password


### Get list of devices
```
Device[] devices = mcs.GetDevices();
```

### Get device history for a double observation
```
DoubleObservation[] observations = mcs.GetObservations("mid", "obsId", DataType.Double, fromDate, toDate) as DoubleObservation[];
```
where
- "mid" is the device identifier (short for Masterloop Id)
- "obsId" is the observation identifier (according to device template)
- fromDate is the start date in the history query
- toDate is the end date in the history query

The result will be all observations of id "obsId" between fromDate and toDate for device "mid", sorted chronologically.


## Using the Masterloop IoT Platform AMQP live messaging interface

### Creating a live connection object
```
IMasterloopLiveConnection mlc = new MasterloopLiveConnection("hostname", "username", "password", true);
```
where
- "hostname" is the name of your Masterloop server
- "username" is your user name
- "password" is your password

### Registering observation callback
As soon as a message is received, it is decoded and dispatched to any listening processes using callbacks.
Registration of callbacks are required in order to be informed when a message arrives.
```
mlc.RegisterObservationHandler("mid", obsId, handler);
```

A callback handler should have the signature of (example for Double observation type):
```
private void OnReceivedObservationValue(string mid, int observationId, DoubleObservation o)
{
  Console.WriteLine($"{mid} : {observationId} with value {o.Value} at {o.Timestamp}");
}
```

### Connecting
In order to connect to the live data stream, the application needs to call Connect.
This method also tries to re-connect if the connection is broken for some reason.
```
LiveAppRequest lar = new LiveAppRequest()
{
  MID = "mid",
  ObservationIds = new int[] { 100, 101, 102 },
  CommandIds = new int[] { 1, 2, 3 },
  InitObservationValues = true,
  ReceiveDevicePulse = true
};
bool success = mlc.Connect(new LiveAppRequest[] { lar });
```

### Disconnecting
When a connection should be closed, the following method should always be called:
```
mlc.Disconnect();
```

