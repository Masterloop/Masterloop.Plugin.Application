using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Masterloop.Core.Types.Firmware;
using Masterloop.Core.Types.ImportExport;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;
using Masterloop.Core.Types.Pulse;
using Masterloop.Core.Types.Settings;
using Newtonsoft.Json;

namespace Masterloop.Plugin.Application
{
    /// <summary>
    ///     New Masterloop Server Connection - Alpha
    /// </summary>
    public class MasterloopApiConnection : IMasterloopApiConnection
    {
        private const string OriginAddressHeader = "OriginAddress";
        private const string OriginApplicationHeader = "OriginApplication";
        private const string OriginReferenceHeader = "OriginReference";
        private const string DefaultAcceptHeader = "application/json";
        private const string DefaultContentType = "application/json";
        private const string ServerConnectivityResponse = "PONG";

        private readonly HttpClient _httpClient;
        private readonly MasterloopApiUrlHelper _urlHelper;

        /// <summary>
        /// </summary>
        /// <param name="masterloopApiOptions">Instance of MasterloopApiOptions. Connection details</param>
        /// <param name="applicationMetadata">Instance of ApplicationMetadata. Information about the client</param>
        /// <param name="httpClient">Instance of HttpClient.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public MasterloopApiConnection(MasterloopApiOptions masterloopApiOptions, ApplicationMetadata applicationMetadata, HttpClient httpClient)
        {
            ApplicationMetadata = applicationMetadata;
            MasterloopApiOptions = masterloopApiOptions ?? throw new ArgumentNullException(nameof(masterloopApiOptions));

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _urlHelper = new MasterloopApiUrlHelper(masterloopApiOptions.Host, masterloopApiOptions.UseHttps);

            // Set authorization headers
            if (!string.IsNullOrEmpty(masterloopApiOptions.Username) && !string.IsNullOrEmpty(masterloopApiOptions.Password))
            {
                //request.Credentials = new NetworkCredential(this.Username, this.Password);
                var authInfo = $"{masterloopApiOptions.Username}:{masterloopApiOptions.Password}";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Base64Encode(authInfo));
            }

            // Set application metadata headers
            OriginAddress = GetLocalIpAddress();
            if (ApplicationMetadata == null)
            {
                // Set default metadata to calling application.
                var callingAssembly = Assembly.GetCallingAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(callingAssembly.Location);
                ApplicationMetadata = new ApplicationMetadata
                {
                    Application = callingAssembly.GetName().Name,
                    Reference = fileVersionInfo.FileVersion
                };
            }
            if (!string.IsNullOrEmpty(OriginAddress))
                _httpClient.DefaultRequestHeaders.Add(OriginAddressHeader, OriginAddress);
            if (!string.IsNullOrEmpty(applicationMetadata.Application))
                _httpClient.DefaultRequestHeaders.Add(OriginApplicationHeader, applicationMetadata.Application);
            if (!string.IsNullOrEmpty(applicationMetadata.Reference))
                _httpClient.DefaultRequestHeaders.Add(OriginReferenceHeader, applicationMetadata.Reference);

            // Set accept header
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(DefaultAcceptHeader));
        }

        /// <summary>
        ///     Get local IP Address
        /// </summary>
        public string OriginAddress { get; }

        /// <summary>
        ///     Get Masterloop API options
        /// </summary>
        public MasterloopApiOptions MasterloopApiOptions { get; }

        /// <summary>
        ///     Get application metadata information
        /// </summary>
        public ApplicationMetadata ApplicationMetadata { get; }

        /// <summary>
        ///     Get or set HttpClient timeout in seconds
        /// </summary>
        public int Timeout
        {
            get => (int)_httpClient.Timeout.TotalSeconds;
            set => _httpClient.Timeout = TimeSpan.FromSeconds(value);
        }

        #region Snapshot

        /// <summary>
        ///     Get snapshot for multiple devices.
        /// </summary>
        /// <param name="snapshotRequest">SnapshotRequest object containing devices, observation, setting and pulse specification.</param>
        /// <returns>Array of SnapshotItem objects according to request and existence..</returns>
        public Task<SnapshotItem[]> GetCurrentSnapshotAsync(SnapshotRequest snapshotRequest)
        {
            return PostSerializedAsync<SnapshotRequest, SnapshotItem[]>(_urlHelper.SnapshotCurrentUrl(),
                snapshotRequest);
        }

        #endregion

        #region Connectivity

        public async Task<bool> CanPingAsync()
        {
            var response = await DownloadContentAsString(_urlHelper.PingUrl());
            return response == ServerConnectivityResponse;
        }

        #endregion

        #region Templates

        public Task<DeviceTemplate[]> GetTemplatesAsync()
        {
            return GetDeserializedAsync<DeviceTemplate[]>(_urlHelper.TemplatesUrl());
        }

        public Task<DeviceTemplate> GetTemplateAsync(string TID)
        {
            return GetDeserializedAsync<DeviceTemplate>(_urlHelper.TemplateUrl(TID));
        }

        public Task<Device[]> GetTemplateDevicesAsync(string TID)
        {
            return GetDeserializedAsync<Device[]>(_urlHelper.TemplateUrl(TID));
        }

        public Task<bool> CreateTemplateAsync(int tenantId, DeviceTemplate template)
        {
            return PostSerializedAsync(_urlHelper.TenantTemplatesUrl(tenantId), template);
        }

        #endregion

        #region Devices

        public Devicelet[] GetDevicelets(bool includeDetails = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <returns>Array of Masterloop Device objects.</returns>
        public Task<Device[]> GetDevicesAsync(bool includeMetadata = false)
        {
            return GetDeserializedAsync<Device[]>(_urlHelper.DevicesWithMetadataUrl(includeMetadata));
        }

        /// <summary>
        ///     Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <param name="includeDetails">True to include details, False to skip details.</param>
        /// <returns>Array of Masterloop DetailedDevice objects.</returns>
        public Task<DetailedDevice[]> GetDevicesAsync(bool includeMetadata = false, bool includeDetails = false)
        {
            return GetDeserializedAsync<DetailedDevice[]>(
                _urlHelper.DevicesWithMetadataAndDetailsUrl(includeMetadata, includeDetails));
        }

        /// <summary>
        ///     Returns device details for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device details structure.</returns>
        public Task<DetailedDevice> GetDeviceDetailsAsync(string MID)
        {
            return GetDeserializedAsync<DetailedDevice>(_urlHelper.DeviceDetailsUrl(MID));
        }

        /// <summary>
        ///     Returns secure device details for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device secure details structure.</returns>
        public Task<SecureDetailedDevice> GetSecureDeviceDetailsAsync(string MID)
        {
            return GetDeserializedAsync<SecureDetailedDevice>(_urlHelper.DeviceSecureDetailsUrl(MID));
        }

        /// <summary>
        ///     Returns device template for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device template structure.</returns>
        public Task<DeviceTemplate> GetDeviceTemplateAsync(string MID)
        {
            return GetDeserializedAsync<DeviceTemplate>(_urlHelper.DeviceTemplateUrl(MID));
        }

        /// <summary>
        ///     Create a new device in the cloud service.
        /// </summary>
        /// <param name="newDevice">Structure for new device request.</param>
        /// <returns>DetailedDevice object for newly created device.</returns>
        public Task<DetailedDevice> CreateDeviceAsync(NewDevice newDevice)
        {
            return PostSerializedAsync<NewDevice, DetailedDevice>(_urlHelper.DevicesUrl(), newDevice);
        }

        /// <summary>
        ///     Deletes an existing device in the cloud service.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <returns>True if delete was successful, False otherwise. LastErrorMessage will contain reason in case of error.</returns>
        public Task<bool> DeleteDeviceAsync(string MID)
        {
            return DeleteAsync(_urlHelper.DeviceUrl(MID));
        }

        /// <summary>
        ///     Get devices latest activity timestamp.
        /// </summary>
        /// <returns>DateTime object or null if no activity exists.</returns>
        public async Task<DateTime?> GetLatestLoginTimestampAsync(string MID)
        {
            var timestamp = await DownloadContentAsString(_urlHelper.DeviceLatestActivityTimestampUrl(MID));
            if (DateTime.TryParse(timestamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var latestLoginTimestamp))
                return latestLoginTimestamp.ToUniversalTime();
            return null;
        }

        /// <summary>
        ///     Get current firmware details for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware release information.</returns>
        public Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateFirmwareDetailsAsync(string TID)
        {
            return GetDeserializedAsync<FirmwareReleaseDescriptor>(_urlHelper.DeviceTemplateFirmwareCurrentUrl(TID));
        }

        /// <summary>
        ///     Get firmware variants for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware variant information.</returns>
        public Task<FirmwareVariant[]> GetDeviceTemplateFirmwareVariantsAsync(string TID)
        {
            return GetDeserializedAsync<FirmwareVariant[]>(_urlHelper.DeviceTemplateFirmwareVariantsUrl(TID));
        }

        /// <summary>
        ///     Get current firmware details for template firmware variant.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <param name="firmwareVariantId">Variant identifier.</param>
        /// <returns>Firmware release information.</returns>
        public Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateVariantFirmwareDetailsAsync(string TID,
            int firmwareVariantId)
        {
            return GetDeserializedAsync<FirmwareReleaseDescriptor>(
                _urlHelper.DeviceTemplateFirmwareVariantsCurrentUrl(TID, firmwareVariantId));
        }

        #endregion

        #region Observations

        /// <summary>
        ///     Get the latest observation value for specified device and observation identifier.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <returns>Observation with type according to observation data type or NULL if no current observations exist.</returns>
        public async Task<Observation> GetCurrentObservationAsync(string MID, int observationId, DataType dataType)
        {
            // TODO: Check return type with 404 Not Found
            var serializedObservation =
                await DownloadContentAsString(_urlHelper.DeviceObservationCurrentUrl(MID, observationId));
            if (string.IsNullOrEmpty(serializedObservation))
                return null;
            return MasterloopObservationHelper.DeserializeObservation(serializedObservation, dataType);
        }

        /// <summary>
        ///     Get the latest observation values for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Observations or NULL if no current observations exist.</returns>
        public async Task<IdentifiedObservation[]> GetCurrentObservationsAsync(string MID)
        {
            // TODO: 1. Check return type with 404 Not Found. 2. Return empty array?
            var serializedObservations = await DownloadContentAsString(_urlHelper.DeviceObservationsCurrentUrl(MID));
            if (string.IsNullOrEmpty(serializedObservations))
                return null;
            return MasterloopObservationHelper.DeserializeIdentifiedObservations(serializedObservations);
        }

        /// <summary>
        ///     Get observations for specified device and observation, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of observation objects with type according to observation data type.</returns>
        public async Task<Observation[]> GetObservationsAsync(string MID, int observationId, DataType dataType,
            DateTime from,
            DateTime to)
        {
            // TODO: 1. Check return type with 404 Not Found. 2. Return empty array?
            var serializedObservations =
                await DownloadContentAsString(_urlHelper.DeviceObservationsUrl(MID, observationId, from, to));
            if (string.IsNullOrEmpty(serializedObservations))
                return null;
            return MasterloopObservationHelper.DeserializeObservations(serializedObservations, dataType);
        }

        /// <summary>
        ///     Deletes all observations within a specified time interval, for a given device and observation identifier.
        ///     Please ensure to use long timeouts in case of many observations need to be deleted.
        ///     NOTE: This function in irreversible. Deleting observations has no undo functionality.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="from">Observations with timestamp greater than or equal will be delete.</param>
        /// <param name="to">Observations with timestamp less than or equal will be deleted.</param>
        /// <returns>Number of observations deleted or 0 if none or failure.</returns>
        public Task<int> DeleteObservationsAsync(string MID, int observationId, DateTime from, DateTime to)
        {
            return DeleteAsync<int>(_urlHelper.DeviceObservationsUrl(MID, observationId, from, to));
        }

        #endregion

        #region Commands

        /// <summary>
        ///     Sends a command to a device.
        /// </summary>
        /// <param name="command">Command object of type Command.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> SendDeviceCommandAsync(string MID, Command command)
        {
            return PostSerializedAsync(_urlHelper.DeviceCreateCommandUrl(MID, command.Id), command);
        }

        /// <summary>
        ///     Sends one or more command(s) to one or more device(s).
        /// </summary>
        /// <param name="commandPackages">Command package of type CommandsPackage[].</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> SendMultipleDeviceCommandAsync(CommandsPackage[] commandPackages)
        {
            return PostSerializedAsync(_urlHelper.ToolsMultiCommandsUrl(), commandPackages);
        }

        /// <summary>
        ///     Get device command queue from server.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Array of Command objects in queue, or null if queue is empty.</returns>
        public Task<Command[]> GetDeviceCommandQueueAsync(string MID)
        {
            return GetDeserializedAsync<Command[]>(_urlHelper.DeviceCommandQueueUrl(MID));
        }

        /// <summary>
        ///     Get device command history from server for a specified time range.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of CommandHistory objects in history, or null if command history is empty.</returns>
        public Task<CommandHistory[]> GetDeviceCommandHistoryAsync(string MID, DateTime from, DateTime to)
        {
            return GetDeserializedAsync<CommandHistory[]>(_urlHelper.DeviceCommandHistoryUrl(MID, from, to));
        }

        #endregion

        #region Settings

        /// <summary>
        ///     Sets settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="values">Array of setting values.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public Task<bool> SetSettingsAsync(string MID, SettingValue[] values)
        {
            return PostSerializedAsync(_urlHelper.DeviceSettingsUrl(MID), values);
        }

        /// <summary>
        ///     Sets settings for multiple devices.
        /// </summary>
        /// <param name="values">Array of setting packages.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public Task<bool> SetMultipleSettingsAsync(SettingsPackage[] values)
        {
            return PostSerializedAsync(_urlHelper.ToolsMultiSettingsUrl(), values);
        }

        /// <summary>
        ///     Gets expanded settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier</param>
        /// <returns>ExpandedSettingsPackage object for device or null if no settings are defined.</returns>
        public Task<ExpandedSettingsPackage> GetSettingsAsync(string MID)
        {
            return GetDeserializedAsync<ExpandedSettingsPackage>(_urlHelper.DeviceSettingsExpandedUrl(MID));
        }

        #endregion

        #region Live Temporary Connection

        /// <summary>
        ///     Request live app connection information for multiple devices or templates. AMQP sessions are started using the
        ///     MasterloopLiveConnection class.
        /// </summary>
        /// <param name="liveAppRequests">Array of LiveAppRequest objects containing connection arguments.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public Task<LiveConnectionDetails> RequestLiveConnectionAsync(LiveAppRequest[] liveAppRequests)
        {
            return PostSerializedAsync<LiveAppRequest[], LiveConnectionDetails>(_urlHelper.ToolsLiveConnectUrl(),
                liveAppRequests);
        }

        /// <summary>
        ///     Delete live temporary connection.
        /// </summary>
        /// <param name="temporaryKey">Temporary key (guid).</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> DeleteLiveTemporaryConnectionAsync(string temporaryKey)
        {
            return DeleteAsync(_urlHelper.ToolsLiveConnectTemporaryIdentifiedUrl(temporaryKey));
        }

        #endregion

        #region Live Persistent Connection

        /// <summary>
        ///     Creates a live persistent subscription for a template or multiple devices.
        /// </summary>
        /// <param name="livePersistentSubscriptionRequest">
        ///     LivePersistentSubscriptionRequest object containing connection
        ///     arguments.
        /// </param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> CreateLivePersistentSubscriptionAsync(
            LivePersistentSubscriptionRequest livePersistentSubscriptionRequest)
        {
            return PostSerializedAsync(_urlHelper.ToolsLiveConnectPersistentUrl(), livePersistentSubscriptionRequest);
        }

        /// <summary>
        ///     Creates a live persistent subscription for a template or multiple devices. AMQP sessions are started using the
        ///     MasterloopLiveConnection class.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public Task<LiveConnectionDetails> GetLivePersistentSubscriptionConnectionAsync(string subscriptionKey)
        {
            return GetDeserializedAsync<LiveConnectionDetails>(
                _urlHelper.ToolsLiveConnectPersistentIdentifiedUrl(subscriptionKey));
        }

        /// <summary>
        ///     Adds a device to an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> AddLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid)
        {
            return PostSerializedAsync(_urlHelper.ToolsLiveConnectPersistentAddDeviceUrl(subscriptionKey), mid);
        }

        /// <summary>
        ///     Removes a device from an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> RemoveLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid)
        {
            return DeleteAsync(_urlHelper.ToolsLiveConnectPersistentRemoveDeviceUrl(subscriptionKey, mid));
        }

        /// <summary>
        ///     Deletes an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public Task<bool> DeleteLivePersistentSubscriptionAsync(string subscriptionKey)
        {
            return DeleteAsync(_urlHelper.ToolsLiveConnectPersistentIdentifiedUrl(subscriptionKey));
        }

        public Task<string[]> GetPersistentSubscriptionWhitelistAsync(string subscriptionKey)
        {
            return GetDeserializedAsync<string[]>(_urlHelper.ToolsLiveConnectPersistentAddDeviceUrl(subscriptionKey));
        }

        public Task<bool> CreatePersistentSubscriptionWhitelistAsync(string subscriptionKey)
        {
            return PostSerializedAsync(_urlHelper.ToolsLiveConnectPersistentCreateWhitelistUrl(subscriptionKey),
                string.Empty);
        }

        #endregion

        #region Pulses

        /// <summary>
        ///     Get pulse periods for specified device and pulse id, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of pulse period objects or null in case of failure or if no pulse periods were found.</returns>
        public Task<PulsePeriod[]> GetPulsePeriodAsync(string MID, int pulseId, DateTime from, DateTime to)
        {
            return GetDeserializedAsync<PulsePeriod[]>(_urlHelper.DevicePulsePeriodUrl(MID, pulseId, from, to));
        }

        /// <summary>
        ///     Get current pulse periods for specified device and pulse id.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <returns>Current pulse period object or null in case of failure or if no pulse periods were found.</returns>
        public Task<PulsePeriod> GetCurrentPulsePeriodAsync(string MID, int pulseId)
        {
            return GetDeserializedAsync<PulsePeriod>(_urlHelper.DevicePulsePeriodCurrentUrl(MID, pulseId));
        }

        #endregion

        #region Internal Methods

        private string GetLocalIpAddress()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            return ip.ToString();
                }
            }
            catch (NetworkInformationException)
            {
                // Running on platform where LocalIP is not available
            }

            return null;
        }

        private static string Base64Encode(string textToEncode)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(textToEncode));
        }

        private async Task<TOutput> GetDeserializedAsync<TOutput>(string url)
        {
            var responseBody = await DownloadContentAsString(url);
            if (!string.IsNullOrEmpty(responseBody))
                return JsonConvert.DeserializeObject<TOutput>(responseBody);
            return default;
        }

        private async Task<bool> PostSerializedAsync<TInput>(string url, TInput data)
        {
            var json = JsonConvert.SerializeObject(data);
            await UploadAndGetResultAsStringAsync(url, json);
            return true;
        }

        private async Task<TOutput> PostSerializedAsync<TInput, TOutput>(string url, TInput data)
        {
            var json = JsonConvert.SerializeObject(data);
            var responseBody = await UploadAndGetResultAsStringAsync(url, json);
            if (!string.IsNullOrEmpty(responseBody))
                return JsonConvert.DeserializeObject<TOutput>(responseBody);
            return default;
        }

        private async Task<bool> DeleteAsync(string url)
        {
            await DeleteAndGetResultAsStringAsync(url);
            return true;
        }

        private async Task<TOutput> DeleteAsync<TOutput>(string url)
        {
            var responseBody = await DeleteAndGetResultAsStringAsync(url);
            if (!string.IsNullOrEmpty(responseBody))
                return JsonConvert.DeserializeObject<TOutput>(responseBody);
            return default;
        }

        private async Task<string> DownloadContentAsString(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> UploadAndGetResultAsStringAsync(string url, string data)
        {
            var content = new StringContent(data, Encoding.UTF8, DefaultContentType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> DeleteAndGetResultAsStringAsync(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        #endregion
    }
}