using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;
using Masterloop.Core.Types.Pulse;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Masterloop.Plugin.Application
{
    public class MasterloopLiveConnection : IMasterloopLiveConnection
    {
        #region PrivateMembers
        private MasterloopServerConnection _apiServerConnection;
        private LiveConnectionDetails _liveConnectionDetails;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumer;
        private string _consumerTag;
        private List<ObservationSubscription<Observation>> _observationSubscriptions;
        private List<ObservationSubscription<BooleanObservation>> _booleanSubscriptions;
        private List<ObservationSubscription<DoubleObservation>> _doubleSubscriptions;
        private List<ObservationSubscription<IntegerObservation>> _integerSubscriptions;
        private List<ObservationSubscription<PositionObservation>> _positionSubscriptions;
        private List<ObservationSubscription<StringObservation>> _stringSubscriptions;
        private List<CommandSubscription<Command>> _commandSubscriptions;
        private List<CommandSubscription<CommandResponse>> _commandResponseSubscriptions;
        private List<PulseSubscription> _pulseSubscriptions;
        private ushort _heartbeatInterval = 60;
        private bool _disposed;
        private List<LiveAppRequest> _liveRequests;
        private readonly object _modelLock;
        private Dictionary<int, DataType> _observationType;
        private Queue<BasicDeliverEventArgs> _queue;
        #endregion // PrivateMembers

        #region Properties
        /// <summary>
        /// True ignores any SSL certificate errors, False does not ignore any SSL certificate errors.
        /// </summary>
        public bool IgnoreSslCertificateErrors { get; set; } = false;

        /// <summary>
        /// Specifies the requested heartbeat interval in seconds. Must be within the range of [60, 3600] seconds. Use 0 to disable heartbeats.
        /// More info can be found here: https://www.rabbitmq.com/heartbeats.html
        /// </summary>
        public ushort HeartbeatInterval
        {
            get
            {
                return _heartbeatInterval;
            }
            set
            {
                if (value == 0 || (value >= 60 && value <= 3600))
                {
                    _heartbeatInterval = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("HeartbeatInterval", value, "Heartbeat interval must be between 60 and 3600 seconds (or 0 for disabled).");
                }
            }
        }

        /// <summary>
        /// Network timeout in seconds.
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Last error message as text string in english.
        /// </summary>
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// Live connection details.
        /// </summary>
        public LiveConnectionDetails ConnectionDetails
        {
            get
            {
                return _liveConnectionDetails;
            }
        }

        /// <summary>
        /// Live connection key.
        /// </summary>
        public string ConnectionKey
        {
            get
            {
                if (_liveConnectionDetails != null && _liveConnectionDetails.QueueName != null)
                {
                    // <userid>@@@<key>.Q
                    string[] subStrings = _liveConnectionDetails.QueueName.Split(new[] { "@@@", "." }, StringSplitOptions.None);
                    if (subStrings != null && subStrings.Length == 3)
                    {
                        return subStrings[1];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Set to True for automatic callbacks to be called (default), or False to control callback using Fetch method.
        /// </summary>
        public bool UseAutomaticCallbacks { get; set; } = true;

        /// <summary>
        /// Set to True to quickly acknowledge all incoming messages (default), or False for acknowledge only messages with registered handlers.
        /// </summary>
        public bool UseAutomaticAcknowledgement { get; set; } = true;
        #endregion

        #region Construction
        /// <summary>
        /// Constructs a new live connection using MCS credentials.
        /// </summary>
        /// <param name="hostName">Host to connect to, typically "api.masterloop.net".</param>
        /// <param name="username">MCS username.</param>
        /// <param name="password">MCSLogin password.</param> 
        /// <param name="useEncryption">True if using encryption, False if not using encryption.</param>
        public MasterloopLiveConnection(string hostName, string username, string password, bool useEncryption)
        {
            _modelLock = new object();
            Init();
            _apiServerConnection = new MasterloopServerConnection(hostName, username, password, useEncryption);
            _observationType = new Dictionary<int, DataType>();
            _queue = new Queue<BasicDeliverEventArgs>();
        }

        /// <summary>
        /// Constructs a new live connection object using live message server credentials.
        /// </summary>
        /// <param name="liveConnectionDetails">Live message server connection details.</param>
        public MasterloopLiveConnection(LiveConnectionDetails liveConnectionDetails)
        {
            _modelLock = new object();
            Init();
            _liveConnectionDetails = liveConnectionDetails;
            _observationType = new Dictionary<int, DataType>();
            _queue = new Queue<BasicDeliverEventArgs>();
        }

        /// <summary>
        /// Destroys this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposed) return;
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
        }
        #endregion

        #region Connection
        /// <summary>
        /// Connects to live server using default devices. Use if object is constructed using LiveConnectionDetails object. Does not offer re-connect.
        /// </summary>
        /// <returns>True if connection was successful, False otherwise. Note: If the initial call to Connect fails, re-connection will not be started.</returns>
        public bool Connect()
        {
            Disconnect();  // Remove any existing connection objects

            return OpenConnection();
        }

        /// <summary>
        /// Connects to specified devices or templates. Can only be used if object has been constructed using MCS credentials due to built-in re-connect feature.
        /// </summary>
        /// <param name="liveRequests">Array of LiveAppRequest objects containing device or template connection arguments.</param>
        /// <returns>True if connection was successful, False otherwise. Note: If the initial call to Connect fails, re-connection will not be started.</returns>
        public bool Connect(LiveAppRequest[] liveRequests)
        {
            if (Connect())  // Recycle existing connection if possible
            {
                return true;
            }
            else if (_apiServerConnection != null)
            {
                // Recycling failed, request a new live connection
                _liveRequests = new List<LiveAppRequest>(liveRequests);
                _apiServerConnection.Timeout = Timeout;
                _liveConnectionDetails = _apiServerConnection.RequestLiveConnection(_liveRequests.ToArray());

                return OpenConnection();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Connects to specified devices or templates. Can only be used if object has been constructed using MCS credentials due to built-in re-connect feature.
        /// </summary>
        /// <param name="liveRequests">Array of LiveAppRequest objects containing device or template connection arguments.</param>
        /// <returns>True if connection was successful, False otherwise. Note: If the initial call to Connect fails, re-connection will not be started.</returns>
        public async Task<bool> ConnectAsync(LiveAppRequest[] liveRequests)
        {
            if (Connect())  // Recycle existing connection if possible
            {
                return true;
            }
            else if (_apiServerConnection != null)
            {
                // Recycling failed, request a new live connection
                _liveRequests = new List<LiveAppRequest>(liveRequests);
                _apiServerConnection.Timeout = Timeout;
                _liveConnectionDetails = await _apiServerConnection.RequestLiveConnectionAsync(_liveRequests.ToArray());

                return OpenConnection();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects from MCS Live Server. 
        /// </summary>
        public void Disconnect()
        {
            if (_consumer != null)
            {
                lock (_consumer)
                {
                    _consumer.Received -= ConsumerReceived;
                    _consumer = null;
                }
            }

            if (_model != null)
            {
                lock (_modelLock)
                {
                    if (_model.IsOpen)
                    {
                        try
                        {
                            _model.Close(200, "Goodbye");
                        }
                        catch (Exception) { }
                    }
                    _model.Dispose();
                    _model = null;
                }
            }

            if (_connection != null)
            {
                lock (_connection)
                {
                    if (_connection.IsOpen)
                    {
                        try
                        {
                            _connection.Close();
                        }
                        catch (Exception) { }
                    }
                    _connection.Dispose();
                    _connection = null;
                }
            }

            if (_connectionFactory != null)
            {
                lock (_connectionFactory)
                {
                    _connectionFactory = null;
                }
            }
        }

        /// <summary>
        /// Reports connection status to MCS Live Server.
        /// </summary>
        /// <returns>True if connected, False otherwise.</returns>
        public bool IsConnected()
        {
            if (_connectionFactory == null) return false;
            if (_connection == null) return false;
            if (!_connection.IsOpen) return false;
            if (_model == null) return false;
            lock (_modelLock)
            {
                return _model.IsOpen;
            }
        }

        /// <summary>
        /// Pauses listening for incoming messages.
        /// </summary>
        /// <returns></returns>
        public bool PauseIncoming()
        {
            bool success = false;
            if (_consumer != null && _consumerTag != string.Empty)
            {
                lock (_model)
                {
                    try
                    {
                        _consumer.Received -= ConsumerReceived;
                        _model.BasicCancel(_consumerTag);
                        _consumerTag = string.Empty;
                        success = true;
                    }
                    catch (Exception e)
                    {
                        LastErrorMessage = e.Message;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Resumes listening for incoming messages.
        /// </summary>
        /// <returns></returns>
        public bool ResumeIncoming()
        {
            bool success = false;
            if (_consumer != null && _consumerTag != string.Empty)
            {
                lock (_model)
                {
                    try
                    {
                        _consumer.Received += ConsumerReceived;
                        _consumerTag = _model.BasicConsume(_liveConnectionDetails.QueueName, false, _consumer);
                        success = true;
                    }
                    catch (Exception e)
                    {
                        LastErrorMessage = e.Message;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Fetch next 1 incoming message in queue and dispatch if event handler is associated with it.
        /// </summary>
        /// <returns>True if message was received, false otherwise.</returns>
        public bool Fetch()
        {
            if (_queue.Count > 0)
            {
                lock (_queue)
                {
                    BasicDeliverEventArgs args = _queue.Dequeue();
                    return Dispatch(args.RoutingKey, GetMessageHeader(args), args.Body, args.DeliveryTag);
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        /// <summary>
        /// Synchronously publishes a new command and optionally waits for acceptance.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="command">Command object.</param>
        public bool SendCommand(string MID, Command command)
        {
            if (IsConnected())
            {
                IBasicProperties properties = GetMessageProperties(2);
                if (command.ExpiresAt.HasValue)
                {
                    TimeSpan ts = command.ExpiresAt.Value - DateTime.UtcNow;
                    if (ts.TotalMilliseconds > 0)
                    {
                        properties.Expiration = ts.TotalMilliseconds.ToString("F0");
                    }
                }
                string routingKey = MessageRoutingKey.GenerateDeviceCommandRoutingKey(MID, command.Id, command.Timestamp);
                string json = JsonConvert.SerializeObject(command);
                byte[] body = Encoding.UTF8.GetBytes(json);
                try
                {
                    lock (_modelLock)
                    {
                        _model.BasicPublish(_liveConnectionDetails.ExchangeName, routingKey, true, properties, body);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    LastErrorMessage = e.Message;
                }
            }
            return false;
        }

        /// <summary>
        /// Sends an application pulse to the server for a specified device.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="pulseId">Application pulse identifier.</param>
        /// <param name="timestamp">Timestamp in UTC indicating the time of the pulse. null for current time.</param>
        /// <param name="expiryMilliseconds">Expiry time of pulse signal in milli seconds. Use 0 to never expire. Default 300000 (5 minutes).</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool SendPulse(string MID, int pulseId, DateTime? timestamp, int expiryMilliseconds = 300000)
        {
            if (IsConnected())
            {
                if (!timestamp.HasValue)
                {
                    timestamp = DateTime.UtcNow;
                }

                Pulse pulse = new Pulse()
                {
                    Timestamp = timestamp.Value,
                    MID = MID,
                    PulseId = pulseId
                };

                IBasicProperties properties = GetMessageProperties(1);
                if (expiryMilliseconds > 0)
                {
                    properties.Expiration = expiryMilliseconds.ToString("F0");
                }
                string routingKey = MessageRoutingKey.GeneratePulseRoutingKey(pulse.MID, pulse.PulseId);
                string json = JsonConvert.SerializeObject(pulse);
                byte[] body = Encoding.UTF8.GetBytes(json);
                try
                {
                    lock (_modelLock)
                    {
                        _model.BasicPublish(_liveConnectionDetails.ExchangeName, routingKey, false, properties, body);
                    }
                }
                catch (Exception e)
                {
                    LastErrorMessage = e.Message;
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sends an application pulse to the server for all explicitly connected devices. Device identifies application by PulseId specified in the request array in Connect().
        /// </summary>
        /// <param name="timestamp">Timestamp in UTC indicating the time of the pulse. null for current time.</param>
        /// <param name="expiryMilliseconds">Expiry time of pulse signal in milli seconds. Use 0 to never expire. Default 300000 (5 minutes).</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool SendPulse(DateTime? timestamp, int expiryMilliseconds = 300000)
        {
            foreach (LiveAppRequest request in _liveRequests)
            {
                if (!request.PulseId.HasValue)
                {
                    throw new ArgumentNullException("LiveAppRequest structure to Connect() must specify a valid PulseId value.");
                }

                if (!SendPulse(request.MID, request.PulseId.Value, timestamp, expiryMilliseconds))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, Observation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, Observation> observationHandler, DataType dataType)
        {
            ObservationSubscription<Observation> observationSubscription = new ObservationSubscription<Observation>(MID, observationId, observationHandler);
            _observationSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, dataType);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified boolean observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, BooleanObservation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, BooleanObservation> observationHandler)
        {
            ObservationSubscription<BooleanObservation> observationSubscription = new ObservationSubscription<BooleanObservation>(MID, observationId, observationHandler);
            _booleanSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, DataType.Boolean);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified double observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, DoubleObservation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, DoubleObservation> observationHandler)
        {
            ObservationSubscription<DoubleObservation> observationSubscription = new ObservationSubscription<DoubleObservation>(MID, observationId, observationHandler);
            _doubleSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, DataType.Double);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified integer observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, IntegerObservation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, IntegerObservation> observationHandler)
        {
            ObservationSubscription<IntegerObservation> observationSubscription = new ObservationSubscription<IntegerObservation>(MID, observationId, observationHandler);
            _integerSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, DataType.Integer);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified position observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, PositionObservation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, PositionObservation> observationHandler)
        {
            ObservationSubscription<PositionObservation> observationSubscription = new ObservationSubscription<PositionObservation>(MID, observationId, observationHandler);
            _positionSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, DataType.Position);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified string observation id is received.
        /// </summary>
        /// <param name="MID">Device identifier, all devices must be of the same template.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="observationHandler">Callback method with signature "void Callback(string MID, int observationId, StringObservation o) { ... }"</param>
        public void RegisterObservationHandler(string MID, int observationId, Action<string, int, StringObservation> observationHandler)
        {
            ObservationSubscription<StringObservation> observationSubscription = new ObservationSubscription<StringObservation>(MID, observationId, observationHandler);
            _stringSubscriptions.Add(observationSubscription);
            if (!_observationType.ContainsKey(observationId))
            {
                _observationType.Add(observationId, DataType.String);
            }
        }

        /// <summary>
        /// Removes all callback methods for a specified observation id.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        public void UnregisterObservationHandler(string MID, int observationId)
        {
            RemoveHandler<Observation>(_observationSubscriptions, MID, observationId);
            RemoveHandler<BooleanObservation>(_booleanSubscriptions, MID, observationId);
            RemoveHandler<DoubleObservation>(_doubleSubscriptions, MID, observationId);
            RemoveHandler<IntegerObservation>(_integerSubscriptions, MID, observationId);
            RemoveHandler<PositionObservation>(_positionSubscriptions, MID, observationId);
            RemoveHandler<StringObservation>(_stringSubscriptions, MID, observationId);

            if (ActiveObservationHandlers(observationId) == 0)
            {
                _observationType.Remove(observationId);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified command is received.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="commandId">Command identifier.</param>
        /// <param name="commandHandler">Callback method with argument Command (e.g. "void Callback(Command cmd) { ... }"</param>
        public void RegisterCommandHandler(string MID, int commandId, Action<string, Command> commandHandler)
        {
            CommandSubscription<Command> commandSubscription = new CommandSubscription<Command>(MID, commandId, commandHandler);
            _commandSubscriptions.Add(commandSubscription);
        }

        /// <summary>
        /// Removes all callback methods for a specified command.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="commandId">Command identifier.</param>
        public void UnregisterCommandHandler(string MID, int commandId)
        {
            List<CommandSubscription<Command>> handlersToRemove = new List<CommandSubscription<Command>>();
            foreach (CommandSubscription<Command> s in _commandSubscriptions)
            {
                if (s.MID == MID && s.CommandId == commandId)
                {
                    handlersToRemove.Add(s);
                }
            }
            for (int i = 0; i < handlersToRemove.Count; i++)
            {
                _commandSubscriptions.Remove(handlersToRemove[i]);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when a specified command response is received.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="commandId">Command identifier.</param>
        /// <param name="commandResponseHandler">Callback method with argument CommandResponse (e.g. "void Callback(CommandResponse cmdResponse) { ... }"</param>
        public void RegisterCommandResponseHandler(string MID, int commandId, Action<string, CommandResponse> commandResponseHandler)
        {
            CommandSubscription<CommandResponse> commandSubscription = new CommandSubscription<CommandResponse>(MID, commandId, commandResponseHandler);
            _commandResponseSubscriptions.Add(commandSubscription);
        }

        /// <summary>
        /// Removes all response callback methods for a specified command.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="commandId">Command identifier.</param>
        public void UnregisterCommandResponseHandler(string MID, int commandId)
        {
            List<CommandSubscription<CommandResponse>> handlersToRemove = new List<CommandSubscription<CommandResponse>>();
            foreach (CommandSubscription<CommandResponse> s in _commandResponseSubscriptions)
            {
                if (s.MID == MID && s.CommandId == commandId)
                {
                    handlersToRemove.Add(s);
                }
            }
            for (int i = 0; i < handlersToRemove.Count; i++)
            {
                _commandResponseSubscriptions.Remove(handlersToRemove[i]);
            }
        }

        /// <summary>
        /// Registers a new callback method that is called when pulse from a specified MID is received.
        /// </summary>
        /// <param name="MID">Hearteat device identifier.</param>
        /// <param name="pulseHandler">Callback method with argument Pulse (e.g. "void MyCallback(string MID, int pulseId, Pulse pulse) { ... }"</param>
        public void RegisterPulseHandler(string MID, Action<string, int, Pulse> pulseHandler)
        {
            PulseSubscription pulseSubscription = new PulseSubscription(MID, 0, pulseHandler);
            _pulseSubscriptions.Add(pulseSubscription);
        }

        /// <summary>
        /// Removes all callback methods from a specified device identifier.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        public void UnregisterPulseHandler(string MID)
        {
            List<PulseSubscription> handlersToRemove = new List<PulseSubscription>();
            foreach (PulseSubscription s in _pulseSubscriptions)
            {
                if (s.MID == MID)
                {
                    handlersToRemove.Add(s);
                }
            }
            for (int i = 0; i < handlersToRemove.Count; i++)
            {
                _pulseSubscriptions.Remove(handlersToRemove[i]);
            }
        }
        #region InternalMethods
        private void Init()
        {
            _liveRequests = new List<LiveAppRequest>();
            _disposed = false;
            _observationSubscriptions = new List<ObservationSubscription<Observation>>();
            _booleanSubscriptions = new List<ObservationSubscription<BooleanObservation>>();
            _doubleSubscriptions = new List<ObservationSubscription<DoubleObservation>>();
            _integerSubscriptions = new List<ObservationSubscription<IntegerObservation>>();
            _positionSubscriptions = new List<ObservationSubscription<PositionObservation>>();
            _stringSubscriptions = new List<ObservationSubscription<StringObservation>>();
            _commandSubscriptions = new List<CommandSubscription<Command>>();
            _commandResponseSubscriptions = new List<CommandSubscription<CommandResponse>>();
            _pulseSubscriptions = new List<PulseSubscription>();
        }

        private void RemoveHandler<T>(List<ObservationSubscription<T>> table, string MID, int observationId)
        {
            List<ObservationSubscription<T>> handlersToRemove = new List<ObservationSubscription<T>>();
            foreach (ObservationSubscription<T> s in table)
            {
                if (s.MID == MID && s.ObservationId == observationId)
                {
                    handlersToRemove.Add(s);
                }
            }
            for (int i = 0; i < handlersToRemove.Count; i++)
            {
                table.Remove(handlersToRemove[i]);
            }
        }

        private bool DispatchObservation(string MID, int observationId, string json, DataType dataType)
        {
            int count = 0;
            // Handle base observation subscriptions
            ObservationSubscription<Observation> subscription = _observationSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
            if (subscription != null)
            {
                switch (dataType)
                {
                    case DataType.Boolean:
                        subscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<BooleanObservation>(json));
                        count++;
                        break;
                    case DataType.Double:
                        subscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<DoubleObservation>(json));
                        count++;
                        break;
                    case DataType.Integer:
                        subscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<IntegerObservation>(json));
                        count++;
                        break;
                    case DataType.Position:
                        subscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<PositionObservation>(json));
                        count++;
                        break;
                    case DataType.String:
                        subscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<StringObservation>(json));
                        count++;
                        break;
                }
            }
            else  // Handle specific observation type subscriptions
            {
                switch (dataType)
                {
                    case DataType.Boolean:
                        ObservationSubscription<BooleanObservation> boolSubscription = _booleanSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
                        if (boolSubscription != null)
                        {
                            boolSubscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<BooleanObservation>(json));
                            count++;
                        }
                        break;
                    case DataType.Double:
                        ObservationSubscription<DoubleObservation> dblSubscription = _doubleSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
                        if (dblSubscription != null)
                        {
                            dblSubscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<DoubleObservation>(json));
                            count++;
                        }
                        break;
                    case DataType.Integer:
                        ObservationSubscription<IntegerObservation> intSubscription = _integerSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
                        if (intSubscription != null)
                        {
                            intSubscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<IntegerObservation>(json));
                            count++;
                        }
                        break;
                    case DataType.Position:
                        ObservationSubscription<PositionObservation> posSubscription = _positionSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
                        if (posSubscription != null)
                        {
                            posSubscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<PositionObservation>(json));
                            count++;
                        }
                        break;
                    case DataType.String:
                        ObservationSubscription<StringObservation> strSubscription = _stringSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.ObservationId == observationId);
                        if (strSubscription != null)
                        {
                            strSubscription.ObservationHandler(MID, observationId, JsonConvert.DeserializeObject<StringObservation>(json));
                            count++;
                        }
                        break;
                }
            }
            return count > 0;
        }

        private bool DispatchCommand(string MID, int commandId, string json, DateTime timestamp)
        {
            CommandSubscription<Command> subscription = _commandSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.CommandId == commandId);
            if (subscription != null)
            {
                subscription.CommandHandler(MID, JsonConvert.DeserializeObject<Command>(json));
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool DispatchCommandResponse(string MID, int commandId, string json, DateTime timestamp)
        {
            CommandSubscription<CommandResponse> subscription = _commandResponseSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.CommandId == commandId);
            if (subscription != null)
            {
                subscription.CommandHandler(MID, JsonConvert.DeserializeObject<CommandResponse>(json));
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool DispatchPulse(string MID, int pulseId, string json)
        {
            PulseSubscription subscription = _pulseSubscriptions.Find(s => (s.MID == MID || s.MID == null) && s.PulseId == pulseId);
            if (subscription != null)
            {
                subscription.PulseHandler(MID, pulseId, JsonConvert.DeserializeObject<Pulse>(json));
                return true;
            }
            else
            {
                return false;
            }
        }

        private IBasicProperties GetMessageProperties(byte deliveryMode)
        {
            lock (_modelLock)
            {
                IBasicProperties properties = _model.CreateBasicProperties();
                properties.ContentType = "application/json";
                properties.DeliveryMode = deliveryMode;
                return properties;
            }
        }

        private IDictionary<string, object> GetMessageHeader(BasicDeliverEventArgs args)
        {
            if (args == null) return new Dictionary<string, object>();
            if (args.BasicProperties == null) return new Dictionary<string, object>();
            return args.BasicProperties.Headers;
        }

        private bool Open()
        {
            if (_liveConnectionDetails.UseSsl)
            {
                var ssl = new SslOption();
                ssl.Enabled = true;
                if (IgnoreSslCertificateErrors)
                {
                    ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors;
                }
                ssl.ServerName = _liveConnectionDetails.Server;
                _connectionFactory = new ConnectionFactory
                {
                    HostName = _liveConnectionDetails.Server,
                    VirtualHost = _liveConnectionDetails.VirtualHost,
                    UserName = _liveConnectionDetails.Username,
                    Password = _liveConnectionDetails.Password,
                    RequestedHeartbeat = _heartbeatInterval,
                    Port = _liveConnectionDetails.Port,
                    Ssl = ssl
                };
            }
            else
            {
                _connectionFactory = new ConnectionFactory
                {
                    HostName = _liveConnectionDetails.Server,
                    VirtualHost = _liveConnectionDetails.VirtualHost,
                    UserName = _liveConnectionDetails.Username,
                    Password = _liveConnectionDetails.Password,
                    RequestedHeartbeat = _heartbeatInterval,
                    Port = _liveConnectionDetails.Port
                };
            }

            try
            {
                _connection = _connectionFactory.CreateConnection();

                if (_connection != null && _connection.IsOpen)
                {
                    lock (_modelLock)
                    {
                        _model = _connection.CreateModel();
                        return _model != null && _model.IsOpen;
                    }
                }
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
            }
            return false;  // Failed to create connection
        }

        private void ConsumerReceived(object sender, BasicDeliverEventArgs args)
        {
            if (UseAutomaticCallbacks)
            {
                Dispatch(args.RoutingKey, GetMessageHeader(args), args.Body, args.DeliveryTag);
            }
            else
            {
                lock (_queue)
                {
                    _queue.Enqueue(args);
                }
            }
        }

        private bool Dispatch(string routingKey, IDictionary<string, object> headers, byte[] body, ulong deliveryTag)
        {
            string MID = MessageRoutingKey.ParseMID(routingKey);

            if (MID != null && MID.Length > 0 && body != null && body.Length > 0)
            {
                if (MessageRoutingKey.IsDeviceObservation(routingKey))
                {
                    int observationId = MessageRoutingKey.ParseObservationId(routingKey);
                    if (observationId != 0 && _observationType.ContainsKey(observationId))
                    {
                        string json = Encoding.UTF8.GetString(body);
                        if (DispatchObservation(MID, observationId, json, _observationType[observationId]))
                        {
                            if (!UseAutomaticAcknowledgement)
                            {
                                lock (_modelLock)
                                {
                                    _model.BasicAck(deliveryTag, false);
                                }
                            }
                            return true;
                        }
                    }
                }
                else if (MessageRoutingKey.IsDeviceCommand(routingKey))
                {
                    int commandId = MessageRoutingKey.ParseCommandId(routingKey);
                    if (commandId != 0)
                    {
                        string json = Encoding.UTF8.GetString(body);
                        DateTime timestamp = MessageRoutingKey.ParseCommandTimestamp(routingKey);
                        if (DispatchCommand(MID, commandId, json, timestamp))
                        {
                            if (!UseAutomaticAcknowledgement)
                            {
                                lock (_modelLock)
                                {
                                    _model.BasicAck(deliveryTag, false);
                                }
                            }
                            return true;
                        }
                    }
                }
                else if (MessageRoutingKey.IsDeviceCommandResponse(routingKey))
                {
                    int commandId = MessageRoutingKey.ParseCommandId(routingKey);
                    if (commandId != 0)
                    {
                        string json = Encoding.UTF8.GetString(body);
                        DateTime timestamp = MessageRoutingKey.ParseCommandTimestamp(routingKey);
                        if (DispatchCommandResponse(MID, commandId, json, timestamp))
                        {
                            if (!UseAutomaticAcknowledgement)
                            {
                                lock (_modelLock)
                                {
                                    _model.BasicAck(deliveryTag, false);
                                }
                            }
                            return true;
                        }
                    }
                }
                else if (MessageRoutingKey.IsDevicePulse(routingKey))
                {
                    string json = Encoding.UTF8.GetString(body);
                    if (MessageRoutingKey.IsDevicePulse(routingKey))
                    {
                        if (DispatchPulse(MID, 0, json))
                        {
                            if (!UseAutomaticAcknowledgement)
                            {
                                lock (_modelLock)
                                {
                                    _model.BasicAck(deliveryTag, false);
                                }
                            }
                            return true;
                        }
                    }
                    else if (MessageRoutingKey.IsApplicationPulse(routingKey))
                    {
                        int pulseId = MessageRoutingKey.ParsePulseId(routingKey);
                        if (DispatchPulse(MID, pulseId, json))
                        {
                            if (!UseAutomaticAcknowledgement)
                            {
                                lock (_modelLock)
                                {
                                    _model.BasicAck(deliveryTag, false);
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            if (!UseAutomaticAcknowledgement)
            {
                lock (_modelLock)
                {
                    _model.BasicNack(deliveryTag, false, false);
                }
            }
            return false;
        }

        private bool OpenConnection()
        {
            if (_liveConnectionDetails != null)
            {
                if (Open())
                {
                    // Enable automatic callback handler if specified.
                    lock (_modelLock)
                    {
                        _consumer = new EventingBasicConsumer(_model);
                        _consumer.Received += ConsumerReceived;
                        try
                        {
                            _consumerTag = _model.BasicConsume(_liveConnectionDetails.QueueName, UseAutomaticAcknowledgement, _consumer);
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private int ActiveObservationHandlers(int observationId)
        {
            int handlerCount = 0;
            handlerCount += _observationSubscriptions.Count(s => observationId == s.ObservationId);
            handlerCount += _booleanSubscriptions.Count(s => observationId == s.ObservationId);
            handlerCount += _doubleSubscriptions.Count(s => observationId == s.ObservationId);
            handlerCount += _integerSubscriptions.Count(s => observationId == s.ObservationId);
            handlerCount += _positionSubscriptions.Count(s => observationId == s.ObservationId);
            handlerCount += _stringSubscriptions.Count(s => observationId == s.ObservationId);

            return handlerCount;
        }
        #endregion //InternalMethods
    }
}