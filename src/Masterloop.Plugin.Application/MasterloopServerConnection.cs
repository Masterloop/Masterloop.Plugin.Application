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
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net.Http;
using Masterloop.Codecs;
using System.Reflection;

namespace Masterloop.Plugin.Application
{
    public class MasterloopServerConnection : IMasterloopServerConnection
    {
        #region PrivateMembers
        private readonly string _baseAddress;
        private readonly string _username;
        private readonly string _password;
        private string _localAddress;
        private int _timeout;
        private ApplicationMetadata _metadata;
        private readonly ExtendedHttpClient _extendedHttpClient;
        #endregion

        #region Constants
        private const string _addressTemplates = "/api/templates";
        private const string _addressTemplate = "/api/templates/{0}";
        private const string _addressTemplateDevices = "/api/templates/{0}/devices";
        private const string _addressTenantTemplates = "/api/tenants/{0}/templates";
        private const string _addressDevices = "/api/devices";
        private const string _addressDevice = "/api/devices/{0}";
        private const string _addressDevicesWithMetadata = "/api/devices?includeMetadata={0}";
        private const string _addressDevicesWithMetadataAndDetails = "/api/devices?includeMetadata={0}&includeDetails={1}";
        private const string _addressDeviceDetails = "/api/devices/{0}/details";
        private const string _addressDeviceSecureDetails = "/api/devices/{0}/securedetails";
        private const string _addressDeviceTemplate = "/api/devices/{0}/template";
        private const string _addressDeviceObservationsCurrent = "/api/devices/{0}/observations/current2";
        private const string _addressDeviceObservationCurrent = "/api/devices/{0}/observations/{1}/current";
        private const string _addressDeviceObservations = "/api/devices/{0}/observations/{1}/observations?fromTimestamp={2}&toTimestamp={3}";
        private const string _addressDeviceCreateCommand = "/api/devices/{0}/commands/{1}";
        private const string _addressDeviceCommandQueue = "/api/devices/{0}/commands/queue";
        private const string _addressDeviceCommandHistory = "/api/devices/{0}/commands?fromTimestamp={1}&toTimestamp={2}";
        private const string _addressDeviceSettings = "/api/devices/{0}/settings";
        private const string _addressDeviceSettingsExpanded = "/api/devices/{0}/settings/expanded";
        private const string _addressDeviceLatestActivityTimestamp = "/api/devices/{0}/activity/latest/timestamp";
        private const string _addressDeviceTemplateFirmwareCurrent = "/api/templates/{0}/firmware/current";
        private const string _addressDeviceTemplateFirmwareVariants = "/api/templates/{0}/firmware/variants";
        private const string _addressDeviceTemplateFirmwareVariantsCurrent = "/api/templates/{0}/firmware/{1}/current";
        private const string _addressToolsLiveConnect = "/api/tools/liveconnect";
        private const string _addressToolsLiveConnectTemporaryIdentified = "/api/tools/liveconnect/{0}";
        private const string _addressToolsLiveConnectPersistent = "/api/tools/liveconnect/persistent";
        private const string _addressToolsLiveConnectPersistentIdentified = "/api/tools/liveconnect/persistent/{0}";
        private const string _addressToolsLiveConnectPersistentAddDevice = "/api/tools/liveconnect/persistent/{0}/devices";
        private const string _addressToolsLiveConnectPersistentCreateWhitelist = "/api/tools/liveconnect/persistent/{0}/whitelist";
        private const string _addressToolsLiveConnectPersistentRemoveDevice = "/api/tools/liveconnect/persistent/{0}/devices/{1}";
        private const string _addressDevicePulsePeriod = "/api/devices/{0}/pulse/{1}?fromTimestamp={2}&toTimestamp={3}";
        private const string _addressDevicePulsePeriodCurrent = "/api/devices/{0}/pulse/{1}/current";
        private const string _addressSnapshotCurrent = "/api/tools/snapshot/current";
        private const string _addressToolsMultiSettings = "/api/tools/multisettings";
        private const string _addressToolsMultiCommands = "/api/tools/multicommands";
        private const string _addressPing = "/api/tools/ping";

        private const string _MIME_TYPE_MASTERLOOP_DEVICES = "application/vnd.masterloop.devices";

        private const int _defaultTimeout = 30; // 30 seconds

        private const string DefaultAcceptHeader = "application/json";
        private const string DefaultContentType = "application/json";
        #endregion

        #region Configuration

        /// <summary>
        /// Network timeout in seconds.
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                _extendedHttpClient?.SetTimeout(_timeout);
            }
        }

        /// <summary>
        /// Use HTTP traffic compression (gzip).
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// Application metadata used in server api interactions for improved tracability (optional).
        /// </summary>
        public ApplicationMetadata Metadata
        {
            get => _metadata;
            set
            {
                _metadata = value;
                _extendedHttpClient?.SetMetaData(value);
            }
        }

        public bool UseHttpClientInsteadOfWebRequests { get; set; }

        #endregion

        #region State
        /// <summary>
        /// Last error message after an error, or null if no errors have occured.
        /// </summary>
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// HTTP status code received from last API request.
        /// </summary>
        public System.Net.HttpStatusCode LastHttpStatusCode { get; set; }
        #endregion

        #region LifeCycle
        /// <summary>
        /// Constructs a new BasicApplication object.
        /// </summary>
        /// <param name="hostName">Host to connect to, e.g. "myserver.example.com" or "10.0.0.2".</param>
        /// <param name="username">Login username.</param>
        /// <param name="password">Login password.</param> 
        /// <param name="useHttps">True if using HTTPS (SSL/TLS), False if using HTTP (unencrypted).</param>
        public MasterloopServerConnection(string hostName, string username, string password, bool useHttps = true)
        {
            _username = username;
            _password = password;
            if (useHttps)
            {
                _baseAddress = string.Format("https://{0}", hostName);
            }
            else
            {
                _baseAddress = string.Format("http://{0}", hostName);
            }
            Timeout = _defaultTimeout;
            _localAddress = GetLocalIPAddress();
            UseCompression = true;

            // Set default metadata to calling application.
            Assembly calling = Assembly.GetCallingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(calling.Location);
            Metadata = new ApplicationMetadata()
            {
                Application = calling.GetName().Name,
                Reference = fvi.FileVersion
            };
            _extendedHttpClient = new ExtendedHttpClient(_username, _password, UseCompression, _localAddress, Metadata);
        }

        /// <summary>
        /// Destroys this object.
        /// </summary>
        public void Dispose()
        {
        }
        #endregion

        #region Templates
        public DeviceTemplate[] GetTemplates()
        {
            return GetDeserialized<DeviceTemplate[]>(_addressTemplates);
        }

        public DeviceTemplate GetTemplate(string TID)
        {
            string url = string.Format(_addressTemplate, TID);
            return GetDeserialized<DeviceTemplate>(url);
        }

        public Device[] GetTemplateDevices(string TID)
        {
            string url = string.Format(_addressTemplateDevices, TID);
            return GetDeserialized<Device[]>(url);
        }

        public bool CreateTemplate(int tenantId, DeviceTemplate template)
        {
            string url = string.Format(_addressTenantTemplates, tenantId);
            string body = JsonConvert.SerializeObject(template);
            Tuple<bool, string> result = Post(url, body);
            if (result != null)
            {
                return result.Item1;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Devices
        public Devicelet[] GetDevicelets(bool includeDetails = false)
        {
            return GetDeviceletsAsync(includeDetails).Result;
        }

        private async Task<Devicelet[]> GetDeviceletsAsync(bool includeDetails = false)
        {
            string url = string.Format(_addressDevicesWithMetadataAndDetails, false.ToString().ToLower(), includeDetails.ToString().ToLower());
            Tuple<bool, byte[]> result = await GetBytesAsync(url, _MIME_TYPE_MASTERLOOP_DEVICES);

            if (result.Item1 && result.Item2 != null)
            {
                using (MemoryStream stream = new MemoryStream(result.Item2))
                {
                    using (BigEndianReader reader = new BigEndianReader(new BinaryReader(stream)))
                    {
                        byte version = reader.ReadByte();
                        if (version == 1)
                        {
                            List<Devicelet> devicelets = new List<Devicelet>();
                            UInt32 templateCount = reader.ReadUInt32();
                            for (int i = 0; i < templateCount; i++)
                            {
                                byte tidLength = reader.ReadByte();
                                byte[] tidBinary = reader.ReadBytes(tidLength);
                                string tid = Encoding.UTF8.GetString(tidBinary);
                                UInt32 deviceCount = reader.ReadUInt32();
                                for (int j = 0; j < deviceCount; j++)
                                {
                                    Devicelet devicelet = new Devicelet();
                                    devicelet.TID = tid;
                                    byte midLength = reader.ReadByte();
                                    byte[] midBinary = reader.ReadBytes(midLength);
                                    devicelet.MID = Encoding.UTF8.GetString(midBinary);
                                    byte nameLength = reader.ReadByte();
                                    byte[] nameBinary = reader.ReadBytes(nameLength);
                                    devicelet.Name = Encoding.UTF8.GetString(nameBinary);
                                    UInt32 latestPulseUnix = reader.ReadUInt32();
                                    if (latestPulseUnix > 0)
                                    {
                                        devicelet.LatestPulse = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(latestPulseUnix);
                                    }
                                    devicelets.Add(devicelet);
                                }
                            }
                            return devicelets.ToArray();
                        }
                        else
                        {
                            this.LastErrorMessage = $"Unsupported devicelet version number: {version}";
                            return null;
                        }
                    }
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <returns>Array of Masterloop Device objects.</returns>
        public Device[] GetDevices(bool includeMetadata = false)
        {
            string url = string.Format(_addressDevicesWithMetadata, includeMetadata);
            return GetDeserialized<Device[]>(url);
        }

        /// <summary>
        /// Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <returns>Array of Masterloop Device objects.</returns>
        public async Task<Device[]> GetDevicesAsync(bool includeMetadata = false)
        {
            string url = string.Format(_addressDevicesWithMetadata, includeMetadata);
            return await GetDeserializedAsync<Device[]>(url);
        }

        /// <summary>
        /// Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <param name="includeDetails">True to include details, False to skip details.</param>
        /// <returns>Array of Masterloop DetailedDevice objects.</returns>
        public DetailedDevice[] GetDevices(bool includeMetadata = false, bool includeDetails = false)
        {
            string url = string.Format(_addressDevicesWithMetadataAndDetails, includeMetadata, includeDetails);
            return GetDeserialized<DetailedDevice[]>(url);
        }

        /// <summary>
        /// Returns device catalog for current user.
        /// </summary>
        /// <param name="includeMetadata">True to include metadata, False to skip metadata.</param>
        /// <param name="includeDetails">True to include details, False to skip details.</param>
        /// <returns>Array of Masterloop DetailedDevice objects.</returns>
        public async Task<DetailedDevice[]> GetDevicesAsync(bool includeMetadata = false, bool includeDetails = false)
        {
            string url = string.Format(_addressDevicesWithMetadataAndDetails, includeMetadata, includeDetails);
            return await GetDeserializedAsync<DetailedDevice[]>(url);
        }

        /// <summary>
        /// Returns device details for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device details structure.</returns>
        public DetailedDevice GetDeviceDetails(string MID)
        {
            string url = string.Format(_addressDeviceDetails, MID);
            return GetDeserialized<DetailedDevice>(url);
        }

        /// <summary>
        /// Returns device details for specified device asynchronously.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device details structure.</returns>
        public async Task<DetailedDevice> GetDeviceDetailsAsync(string MID)
        {
            string url = string.Format(_addressDeviceDetails, MID);
            return await GetDeserializedAsync<DetailedDevice>(url);
        }

        /// <summary>
        /// Returns secure device details for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device secure details structure.</returns>
        public SecureDetailedDevice GetSecureDeviceDetails(string MID)
        {
            string url = string.Format(_addressDeviceSecureDetails, MID);
            return GetDeserialized<SecureDetailedDevice>(url);
        }

        /// <summary>
        /// Returns secure device details for specified device asynchronously.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device secure details structure.</returns>
        public async Task<SecureDetailedDevice> GetSecureDeviceDetailsAsync(string MID)
        {
            string url = string.Format(_addressDeviceSecureDetails, MID);
            return await GetDeserializedAsync<SecureDetailedDevice>(url);
        }

        /// <summary>
        /// Returns device template for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device template structure.</returns>
        public DeviceTemplate GetDeviceTemplate(string MID)
        {
            string url = string.Format(_addressDeviceTemplate, MID);
            return GetDeserialized<DeviceTemplate>(url);
        }

        /// <summary>
        /// Returns device template for specified device asynchronously.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Device template structure.</returns>
        public async Task<DeviceTemplate> GetDeviceTemplateAsync(string MID)
        {
            string url = string.Format(_addressDeviceTemplate, MID);
            return await GetDeserializedAsync<DeviceTemplate>(url);
        }

        /// <summary>
        /// Create a new device in the cloud service.
        /// </summary>
        /// <param name="newDevice">Structure for new device request.</param>
        /// <returns>DetailedDevice object for newly created device.</returns>
        public DetailedDevice CreateDevice(NewDevice newDevice)
        {
            string body = JsonConvert.SerializeObject(newDevice);
            return PostDeserialized<DetailedDevice>(_addressDevices, body);
        }

        /// <summary>
        /// Create a new device in the cloud service asynchronously.
        /// </summary>
        /// <param name="newDevice">Structure for new device request.</param>
        /// <returns>DetailedDevice object for newly created device.</returns>
        public async Task<DetailedDevice> CreateDeviceAsync(NewDevice newDevice)
        {
            string body = JsonConvert.SerializeObject(newDevice);
            return await PostDeserializedAsync<DetailedDevice>(_addressDevices, body);
        }

        /// <summary>
        /// Updates an existing device in the cloud service.
        /// </summary>
        /// <param name="updatedDevice">Structure for the updated device request.</param>
        /// <returns>DetailedDevice object for the updated device.</returns>
        public DetailedDevice UpdateDevice(Device updatedDevice)
        {
            string body = JsonConvert.SerializeObject(updatedDevice);
            string url = string.Format(_addressDevice, updatedDevice.MID);
            return PostDeserialized<DetailedDevice>(url, body);
        }

        /// <summary>
        /// Updates an existing device in the cloud service asynchronously.
        /// </summary>
        /// <param name="updatedDevice">Structure for the updated device request.</param>
        /// <returns>DetailedDevice object for the updated device.</returns>
        public async Task<DetailedDevice> UpdateDeviceAsync(Device updatedDevice)
        {
            string body = JsonConvert.SerializeObject(updatedDevice);
            string url = string.Format(_addressDevice, updatedDevice.MID);
            return await PostDeserializedAsync<DetailedDevice>(url, body);
        }

        /// <summary>
        /// Deletes an existing device in the cloud service.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <returns>True if delete was successful, False otherwise. LastErrorMessage will contain reason in case of error.</returns>
        public bool DeleteDevice(string MID)
        {
            string url = string.Format(_addressDevice, MID);
            Tuple<bool, string> result = Delete(url);
            return result.Item1;
        }

        /// <summary>
        /// Deletes an existing device in the cloud service asynchronous.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <returns>True if delete was successful, False otherwise. LastErrorMessage will contain reason in case of error.</returns>
        public async Task<bool> DeleteDeviceAsync(string MID)
        {
            string url = string.Format(_addressDevice, MID);
            Tuple<bool, string> result = await DeleteAsync(url);
            return result.Item1;
        }

        /// <summary>
        /// Get devices latest acitivity timestamp.
        /// </summary>
        /// <returns>DateTime object or null if no activity exists.</returns>
        public DateTime? GetLatestLoginTimestamp(string MID)
        {
            string url = string.Format(_addressDeviceLatestActivityTimestamp, MID);
            string timestamp = GetDeserialized<string>(url);
            try
            {
                return DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get devices latest acitivity timestamp.
        /// </summary>
        /// <returns>DateTime object or null if no activity exists.</returns>
        public async Task<DateTime?> GetLatestLoginTimestampAsync(string MID)
        {
            string url = string.Format(_addressDeviceLatestActivityTimestamp, MID);
            string timestamp = await GetDeserializedAsync<string>(url);
            try
            {
                return DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get current firmware details for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware release information.</returns>
        public FirmwareReleaseDescriptor GetCurrentDeviceTemplateFirmwareDetails(string TID)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareCurrent, TID);
            return GetDeserialized<FirmwareReleaseDescriptor>(url);
        }

        /// <summary>
        /// Get current firmware details for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware release information.</returns>
        public async Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateFirmwareDetailsAsync(string TID)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareCurrent, TID);
            return await GetDeserializedAsync<FirmwareReleaseDescriptor>(url);
        }

        /// <summary>
        /// Get firmware variants for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware variant information.</returns>
        public FirmwareVariant[] GetDeviceTemplateFirmwareVariants(string TID)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareVariants, TID);
            return GetDeserialized<FirmwareVariant[]>(url);
        }

        /// <summary>
        /// Get firmware variants for template.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <returns>Firmware variant information.</returns>
        public async Task<FirmwareVariant[]> GetDeviceTemplateFirmwareVariantsAsync(string TID)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareVariants, TID);
            return await GetDeserializedAsync<FirmwareVariant[]>(url);
        }

        /// <summary>
        /// Get current firmware details for template firmware variant.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <param name="firmwareVariantId">Variant identifier.</param>
        /// <returns>Firmware release information.</returns>
        public FirmwareReleaseDescriptor GetCurrentDeviceTemplateVariantFirmwareDetails(string TID, int firmwareVariantId)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareVariantsCurrent, TID, firmwareVariantId);
            return GetDeserialized<FirmwareReleaseDescriptor>(url);
        }

        /// <summary>
        /// Get current firmware details for template firmware variant.
        /// </summary>
        /// <param name="TID">Template identifier.</param>
        /// <param name="firmwareVariantId">Variant identifier.</param>
        /// <returns>Firmware release information.</returns>
        public async Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateVariantFirmwareDetailsAsync(string TID, int firmwareVariantId)
        {
            string url = string.Format(_addressDeviceTemplateFirmwareVariantsCurrent, TID, firmwareVariantId);
            return await GetDeserializedAsync<FirmwareReleaseDescriptor>(url);
        }
        
        
        #endregion

        #region Observations
        /// <summary>
        /// Get the latest observation value for specified device and observation identifier.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <returns>Observation with type according to observation data type or NULL if no current observations exist.</returns>
        public Observation GetCurrentObservation(string MID, int observationId, DataType dataType)
        {
            string url = string.Format(_addressDeviceObservationCurrent, MID, observationId);
            Tuple<bool, string> result = GetString(url);
            if (result.Item1)
            {
                return DeserializeObservation(result.Item2, dataType);
            }
            return null;
        }

        /// <summary>
        /// Get the latest observation value for specified device and observation identifier.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <returns>Observation with type according to observation data type or NULL if no current observations exist.</returns>
        public async Task<Observation> GetCurrentObservationAsync(string MID, int observationId, DataType dataType)
        {
            string url = string.Format(_addressDeviceObservationCurrent, MID, observationId);
            Tuple<bool, string> result = await GetStringAsync(url);
            if (result.Item1)
            {
                return DeserializeObservation(result.Item2, dataType);
            }
            return null;
        }

        /// <summary>
        /// Get the latest observation values for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Observations or NULL if no current observations exist.</returns>
        public IdentifiedObservation[] GetCurrentObservations(string MID)
        {
            string url = string.Format(_addressDeviceObservationsCurrent, MID);
            Tuple<bool, string> result = GetString(url);
            if (result.Item1)
            {
                return DeserializeIdentifiedObservations(result.Item2);
            }
            return null;
        }

        /// <summary>
        /// Get the latest observation values for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Observations or NULL if no current observations exist.</returns>
        public async Task<IdentifiedObservation[]> GetCurrentObservationsAsync(string MID)
        {
            string url = string.Format(_addressDeviceObservationsCurrent, MID);
            Tuple<bool, string> result = await GetStringAsync(url);
            if (result.Item1)
            {
                return DeserializeIdentifiedObservations(result.Item2);
            }
            return null;
        }

        /// <summary>
        /// Get observations for specified device and observation, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of observation objects with type according to observation data type.</returns>
        public Observation[] GetObservations(string MID, int observationId, DataType dataType, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceObservations, MID, observationId, from.ToString("o"), to.ToString("o"));
            Tuple<bool, string> result = GetString(url);
            if (result.Item1)
            {
                return DeserializeObservations(result.Item2, dataType);
            }
            return null;
        }

        /// <summary>
        /// Get observations for specified device and observation, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="dataType">Data type of observation.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of observation objects with type according to observation data type.</returns>
        public async Task<Observation[]> GetObservationsAsync(string MID, int observationId, DataType dataType, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceObservations, MID, observationId, from.ToString("o"), to.ToString("o"));
            Tuple<bool, string> result = await GetStringAsync(url);
            if (result.Item1)
            {
                return DeserializeObservations(result.Item2, dataType);
            }
            return null;
        }

        /// <summary>
        /// Deletes all observations within a specified time interval, for a given device and observation identifier.
        /// Please ensure to use long timeouts in case of many observations need to be deleted.
        /// NOTE: This function in irreversible. Deleting observations has no undo functionality.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="from">Observations with timestamp greater than or equal will be delete.</param>
        /// <param name="to">Observations with timestamp less than or equal will be deleted.</param>
        /// <returns>Number of observations deleted or 0 if none or failure.</returns>
        public int DeleteObservations(string MID, int observationId, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceObservations, MID, observationId, from.ToString("o"), to.ToString("o"));
            Tuple<bool, string> result = Delete(url);
            if (result.Item1)
            {
                if (result.Item2 != String.Empty && result.Item2.Length > 0)
                {
                    return JsonConvert.DeserializeObject<int>(result.Item2);
                }
            }
            return 0;
        }

        /// <summary>
        /// Deletes all observations within a specified time interval, for a given device and observation identifier.
        /// Please ensure to use long timeouts in case of many observations need to be deleted.
        /// NOTE: This function in irreversible. Deleting observations has no undo functionality.
        /// </summary>
        /// <param name="MID">Device identifier.</param>
        /// <param name="observationId">Observation identifier.</param>
        /// <param name="from">Observations with timestamp greater than or equal will be delete.</param>
        /// <param name="to">Observations with timestamp less than or equal will be deleted.</param>
        /// <returns>Number of observations deleted or 0 if none or failure.</returns>
        public async Task<int> DeleteObservationsAsync(string MID, int observationId, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceObservations, MID, observationId, from.ToString("o"), to.ToString("o"));
            Tuple<bool, string> result = await DeleteAsync(url);
            if (result.Item1)
            {
                if (result.Item2 != String.Empty && result.Item2.Length > 0)
                {
                    return JsonConvert.DeserializeObject<int>(result.Item2);
                }
            }
            return 0;
        }
        #endregion

        #region Commands
        /// <summary>
        /// Sends a command to a device.
        /// </summary>
        /// <param name="command">Command object of type Command.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool SendDeviceCommand(string MID, Command command)
        {
            string url = string.Format(_addressDeviceCreateCommand, MID, command.Id);
            string body = JsonConvert.SerializeObject(command);
            Tuple<bool, string> result = Post(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sends a command to a device.
        /// </summary>
        /// <param name="command">Command object of type Command.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> SendDeviceCommandAsync(string MID, Command command)
        {
            string url = string.Format(_addressDeviceCreateCommand, MID, command.Id);
            string body = JsonConvert.SerializeObject(command);
            Tuple<bool, string> result = await PostAsync(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sends one or more command(s) to one or more device(s).
        /// </summary>
        /// <param name="commandPackages">Command package of type CommandsPackage[].</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool SendMultipleDeviceCommand(CommandsPackage[] commandPackages)
        {
            string url = _addressToolsMultiCommands;
            string body = JsonConvert.SerializeObject(commandPackages);
            Tuple<bool, string> result = Post(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sends one or more command(s) to one or more device(s).
        /// </summary>
        /// <param name="commandPackages">Command package of type CommandsPackage[].</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> SendMultipleDeviceCommandAsync(CommandsPackage[] commandPackages)
        {
            string url = _addressToolsMultiCommands;
            string body = JsonConvert.SerializeObject(commandPackages);
            Tuple<bool, string> result = await PostAsync(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Get device command queue from server.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Array of Command objects in queue, or null if queue is empty.</returns>
        public Command[] GetDeviceCommandQueue(string MID)
        {
            string url = string.Format(_addressDeviceCommandQueue, MID);
            return GetDeserialized<Command[]>(url);
        }

        /// <summary>
        /// Get device command queue from server.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <returns>Array of Command objects in queue, or null if queue is empty.</returns>
        public async Task<Command[]> GetDeviceCommandQueueAsync(string MID)
        {
            string url = string.Format(_addressDeviceCommandQueue, MID);
            return await GetDeserializedAsync<Command[]>(url);
        }

        /// <summary>
        /// Get device command history from server for a specified time range.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of CommandHistory objects in history, or null if command history is empty.</returns>
        public CommandHistory[] GetDeviceCommandHistory(string MID, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceCommandHistory, MID, from.ToString("o"), to.ToString("o"));
            return GetDeserialized<CommandHistory[]>(url);
        }

        /// <summary>
        /// Get device command history from server for a specified time range.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of CommandHistory objects in history, or null if command history is empty.</returns>
        public async Task<CommandHistory[]> GetDeviceCommandHistoryAsync(string MID, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDeviceCommandHistory, MID, from.ToString("o"), to.ToString("o"));
            return await GetDeserializedAsync<CommandHistory[]>(url);
        }
        #endregion

        #region Settings
        /// <summary>
        /// Sets settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="value">Array of setting values.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetSettings(string MID, SettingValue[] values)
        {
            string url = string.Format(_addressDeviceSettings, MID);
            string body = JsonConvert.SerializeObject(values);
            Tuple<bool, string> result = Post(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sets settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="value">Array of setting values.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> SetSettingsAsync(string MID, SettingValue[] values)
        {
            string url = string.Format(_addressDeviceSettings, MID);
            string body = JsonConvert.SerializeObject(values);
            Tuple<bool, string> result = await PostAsync(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sets settings for multiple devices.
        /// </summary>
        /// <param name="value">Array of setting packages.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetMultipleSettings(SettingsPackage[] values)
        {
            string url = _addressToolsMultiSettings;
            string body = JsonConvert.SerializeObject(values);
            Tuple<bool, string> result = Post(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Sets settings for multiple devices.
        /// </summary>
        /// <param name="value">Array of setting packages.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> SetMultipleSettingsAsync(SettingsPackage[] values)
        {
            string url = _addressToolsMultiSettings;
            string body = JsonConvert.SerializeObject(values);
            Tuple<bool, string> result = await PostAsync(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Gets expanded settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier</param>
        /// <returns>ExpandedSettingsPackage object for device or null if no settings are defined.</returns>
        public ExpandedSettingsPackage GetSettings(string MID)
        {
            string url = string.Format(_addressDeviceSettingsExpanded, MID);
            return GetDeserialized<ExpandedSettingsPackage>(url);
        }

        /// <summary>
        /// Gets expanded settings for specified device.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier</param>
        /// <returns>ExpandedSettingsPackage object for device or null if no settings are defined.</returns>
        public async Task<ExpandedSettingsPackage> GetSettingsAsync(string MID)
        {
            string url = string.Format(_addressDeviceSettingsExpanded, MID);
            return await GetDeserializedAsync<ExpandedSettingsPackage>(url);
        }
        #endregion

        #region LiveTemporaryConnection
        /// <summary>
        /// Request live app connection information for multiple devices or templates. AMQP sessions are started using the MasterloopLiveConnection class.
        /// </summary>
        /// <param name="liveAppRequests">Array of LiveAppRequest objects containing connection arguments.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public LiveConnectionDetails RequestLiveConnection(LiveAppRequest[] liveAppRequests)
        {
            string body = JsonConvert.SerializeObject(liveAppRequests);
            return PostDeserialized<LiveConnectionDetails>(_addressToolsLiveConnect, body);
        }

        /// <summary>
        /// Request live app connection information for multiple devices or templates. AMQP sessions are started using the MasterloopLiveConnection class.
        /// </summary>
        /// <param name="liveAppRequests">Array of LiveAppRequest objects containing connection arguments.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public async Task<LiveConnectionDetails> RequestLiveConnectionAsync(LiveAppRequest[] liveAppRequests)
        {
            string body = JsonConvert.SerializeObject(liveAppRequests);
            return await PostDeserializedAsync<LiveConnectionDetails>(_addressToolsLiveConnect, body);
        }

        /// <summary>
        /// Delete live temporary connection.
        /// </summary>
        /// <param name="temporaryKey">Temporary key (guid).</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool DeleteLiveTemporaryConnction(string temporaryKey)
        {
            string url = string.Format(_addressToolsLiveConnectTemporaryIdentified, temporaryKey);
            Tuple<bool, string> result = Delete(url);
            return result.Item1;
        }

        /// <summary>
        /// Delete live temporary connection.
        /// </summary>
        /// <param name="temporaryKey">Temporary key (guid).</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> DeleteLiveTemporaryConnctionAsync(string temporaryKey)
        {
            string url = string.Format(_addressToolsLiveConnectTemporaryIdentified, temporaryKey);
            Tuple<bool, string> result = await DeleteAsync(url);
            return result.Item1;
        }
        #endregion

        #region LivePersistentSubscriptionConnection
        /// <summary>
        /// Creates a live persistent subscription for a template or multiple devices.
        /// </summary>
        /// <param name="livePersistentSubscriptionRequest">LivePersistentSubscriptionRequest object containing connection arguments.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool CreateLivePersistentSubscription(LivePersistentSubscriptionRequest livePersistentSubscriptionRequest)
        {
            string body = JsonConvert.SerializeObject(livePersistentSubscriptionRequest);
            Tuple<bool, string> result = Post(_addressToolsLiveConnectPersistent, body);
            return result.Item1;
        }

        /// <summary>
        /// Create a live persistent subscription for a template or multiple devices.
        /// </summary>
        /// <param name="livePersistentSubscriptionRequest">LivePersistentSubscriptionRequest object containing connection arguments.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> CreateLivePersistentSubscriptionAsync(LivePersistentSubscriptionRequest livePersistentSubscriptionRequest)
        {
            string body = JsonConvert.SerializeObject(livePersistentSubscriptionRequest);
            Tuple<bool, string> result = await PostAsync(_addressToolsLiveConnectPersistent, body);
            return result.Item1;
        }

        /// <summary>
        /// Creates a live persistent subscription for a template or multiple devices. AMQP sessions are started using the MasterloopLiveConnection class.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public LiveConnectionDetails GetLivePersistentSubscriptionConnection(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentIdentified, subscriptionKey);
            return GetDeserialized<LiveConnectionDetails>(url);
        }

        /// <summary>
        /// Creates a live persistent subscription for a template or multiple devices. AMQP sessions are started using the MasterloopLiveConnection class.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <returns>Connection details in the form of a LiveConnectionDetails structure.</returns>
        public async Task<LiveConnectionDetails> GetLivePersistentSubscriptionConnectionAsync(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentIdentified, subscriptionKey);
            return await GetDeserializedAsync<LiveConnectionDetails>(url);
        }

        /// <summary>
        /// Adds a device to an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool AddLivePersistentSubscriptionDevice(string subscriptionKey, string mid)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentAddDevice, subscriptionKey);
            string body = JsonConvert.SerializeObject(mid);
            Tuple<bool, string> result = Post(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Adds a device to an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> AddLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentAddDevice, subscriptionKey);
            string body = JsonConvert.SerializeObject(mid);
            Tuple<bool, string> result = await PostAsync(url, body);
            return result.Item1;
        }

        /// <summary>
        /// Removes a device from an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool RemoveLivePersistentSubscriptionDevice(string subscriptionKey, string mid)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentRemoveDevice, subscriptionKey, mid);
            Tuple<bool, string> result = Delete(url);
            return result.Item1;
        }

        /// <summary>
        /// Removes a device from an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key to live persistent subscription.</param>
        /// <param name="mid">Device identifier.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> RemoveLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentRemoveDevice, subscriptionKey, mid);
            Tuple<bool, string> result = await DeleteAsync(url);
            return result.Item1;
        }

        /// <summary>
        /// Deletes an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public bool DeleteLivePersistentSubscription(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentIdentified, subscriptionKey);
            Tuple<bool, string> result = Delete(url);
            return result.Item1;
        }

        /// <summary>
        /// Deletes an existing live persistent subscription.
        /// </summary>
        /// <param name="subscriptionKey">Subscription key.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> DeleteLivePersistentSubscriptionAsync(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentIdentified, subscriptionKey);
            Tuple<bool, string> result = await DeleteAsync(url);
            return result.Item1;
        }

        public string[] GetPersistentSubscriptionWhitelist(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentAddDevice, subscriptionKey);
            return GetDeserialized<string[]>(url);
        }

        public async Task<string[]> GetPersistentSubscriptionWhitelistAsync(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentAddDevice, subscriptionKey);
            return await GetDeserializedAsync<string[]>(url);
        }

        public bool CreatePersistentSubscriptionWhitelist(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentCreateWhitelist, subscriptionKey);
            Tuple<bool, string> result = Post(url, string.Empty);
            return result.Item1;
        }

        public async Task<bool> CreatePersistentSubscriptionWhitelistAsync(string subscriptionKey)
        {
            string url = string.Format(_addressToolsLiveConnectPersistentCreateWhitelist, subscriptionKey);
            Tuple<bool, string> result = await PostAsync(url, string.Empty);
            return result.Item1;
        }


        #endregion

        #region Pulse
        /// <summary>
        /// Get pulse periods for specified device and pulse id, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of pulse period objects or null in case of failure or if no pulse periods were found.</returns>
        public PulsePeriod[] GetPulsePeriod(string MID, int pulseId, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDevicePulsePeriod, MID, pulseId, from.ToString("o"), to.ToString("o"));
            return GetDeserialized<PulsePeriod[]>(url);
        }

        /// <summary>
        /// Get pulse periods for specified device and pulse id, for a given time interval.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <param name="from">From timestamp.</param>
        /// <param name="to">To timestamp.</param>
        /// <returns>Array of pulse period objects or null in case of failure or if no pulse periods were found.</returns>
        public async Task<PulsePeriod[]> GetPulsePeriodAsync(string MID, int pulseId, DateTime from, DateTime to)
        {
            string url = string.Format(_addressDevicePulsePeriod, MID, pulseId, from.ToString("o"), to.ToString("o"));
            return await GetDeserializedAsync<PulsePeriod[]>(url);
        }

        /// <summary>
        /// Get current pulse periods for specified device and pulse id.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <returns>Current pulse period object or null in case of failure or if no pulse periods were found.</returns>
        public PulsePeriod GetCurrentPulsePeriod(string MID, int pulseId)
        {
            string url = string.Format(_addressDevicePulsePeriodCurrent, MID, pulseId);
            return GetDeserialized<PulsePeriod>(url);
        }

        /// <summary>
        /// Get current pulse periods for specified device and pulse id.
        /// </summary>
        /// <param name="MID">Masterloop Device Identifier.</param>
        /// <param name="pulseId">Pulse identifier.</param>
        /// <returns>Current pulse period object or null in case of failure or if no pulse periods were found.</returns>
        public async Task<PulsePeriod> GetCurrentPulsePeriodAsync(string MID, int pulseId)
        {
            string url = string.Format(_addressDevicePulsePeriodCurrent, MID, pulseId);
            return await GetDeserializedAsync<PulsePeriod>(url);
        }
        #endregion

        #region Snapshot
        /// <summary>
        /// Get snapshot for multiple devices.
        /// </summary>
        /// <param name="snapshotRequest">SnapshotRequest object containing devices, observation, setting and pulse specification.</param>
        /// <returns>Array of SnapshotItem objects according to request and existence..</returns>
        public SnapshotItem[] GetCurrentSnapshot(SnapshotRequest snapshotRequest)
        {
            string body = JsonConvert.SerializeObject(snapshotRequest);
            return PostDeserialized<SnapshotItem[]>(_addressSnapshotCurrent, body);
        }

        /// <summary>
        /// Get snapshot for multiple devices.
        /// </summary>
        /// <param name="snapshotRequest">SnapshotRequest object containing devices, observation, setting and pulse specification.</param>
        /// <returns>Array of SnapshotItem objects according to request and existence..</returns>
        public async Task<SnapshotItem[]> GetCurrentSnapshotAsync(SnapshotRequest snapshotRequest)
        {
            string body = JsonConvert.SerializeObject(snapshotRequest);
            return await PostDeserializedAsync<SnapshotItem[]>(_addressSnapshotCurrent, body);
        }
        #endregion

        #region Connectivity
        public bool CanPing()
        {
            string result = GetDeserialized<string>(_addressPing);
            return result == "PONG";
        }
        public async Task<bool> CanPingAsync()
        {
            string result = await GetDeserializedAsync<string>(_addressPing);
            return result == "PONG";
        }
        #endregion

        #region InternalMethods
        private Observation DeserializeObservation(string json, DataType dataType)
        {
            if (json != string.Empty)
            {
                switch (dataType)
                {
                    case DataType.Boolean:
                        return JsonConvert.DeserializeObject<BooleanObservation>(json);
                    case DataType.Double:
                        return JsonConvert.DeserializeObject<DoubleObservation>(json);
                    case DataType.Integer:
                        return JsonConvert.DeserializeObject<IntegerObservation>(json);
                    case DataType.Position:
                        return JsonConvert.DeserializeObject<PositionObservation>(json);
                    case DataType.String:
                        return JsonConvert.DeserializeObject<StringObservation>(json);
                    case DataType.Statistics:
                        return JsonConvert.DeserializeObject<StatisticsObservation>(json);
                    default:
                        throw new NotSupportedException("Unsupported data type: " + dataType.ToString());
                }
            }
            return null;
        }

        private IdentifiedObservation[] DeserializeIdentifiedObservations(string json)
        {
            if (json != string.Empty && json.Length > 0)
            {
                ExpandedObservationValue[] values = JsonConvert.DeserializeObject<ExpandedObservationValue[]>(json);
                if (values != null && values.Length > 0)
                {
                    List<IdentifiedObservation> ios = new List<IdentifiedObservation>();
                    foreach (ExpandedObservationValue v in values)
                    {
                        IdentifiedObservation io = new IdentifiedObservation()
                        {
                            ObservationId = v.Id,
                            Observation = ObservationStringConverter.StringToObservation(v.Timestamp, v.Value, v.DataType)
                        };
                        ios.Add(io);
                    }
                    return ios.ToArray();
                }
            }
            return null;
        }

        private Observation[] DeserializeObservations(string json, DataType dataType)
        {
            if (json != string.Empty)
            {
                switch (dataType)
                {
                    case DataType.Boolean:
                        return JsonConvert.DeserializeObject<BooleanObservation[]>(json);
                    case DataType.Double:
                        return JsonConvert.DeserializeObject<DoubleObservation[]>(json);
                    case DataType.Integer:
                        return JsonConvert.DeserializeObject<IntegerObservation[]>(json);
                    case DataType.Position:
                        return JsonConvert.DeserializeObject<PositionObservation[]>(json);
                    case DataType.String:
                        return JsonConvert.DeserializeObject<StringObservation[]>(json);
                    case DataType.Statistics:
                        return JsonConvert.DeserializeObject<StatisticsObservation[]>(json);
                    default:
                        throw new NotSupportedException("Unsupported data type: " + dataType.ToString());
                }
            }
            return null;
        }

        private Tuple<bool, string> GetString(string addressExtension, string accept = DefaultAcceptHeader)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return GetStringHttpClientAsync(addressExtension, accept).Result;

            Tuple<bool, byte[]> result = GetBytes(addressExtension, accept);
            if (result.Item1 && result.Item2 != null)
            {
                return new Tuple<bool, string>(result.Item1, Encoding.UTF8.GetString(result.Item2));
            }
            else
            {
                return new Tuple<bool, string>(result.Item1, string.Empty);
            }
        }

        private async Task<Tuple<bool, string>> GetStringAsync(string addressExtension, string accept = DefaultAcceptHeader)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return await GetStringHttpClientAsync(addressExtension, accept);

            Tuple<bool, byte[]> result = await GetBytesAsync(addressExtension, accept);
            if (result.Item1 && result.Item2 != null)
            {
                return new Tuple<bool, string>(result.Item1, Encoding.UTF8.GetString(result.Item2));
            }
            else
            {
                return new Tuple<bool, string>(result.Item1, string.Empty);
            }
        }

        private Tuple<bool, byte[]> GetBytes(string addressExtension, string accept = DefaultAcceptHeader)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return GetBytesHttpClientAsync(addressExtension, accept).Result;

            var webClient = new ExtendedWebClient();
            webClient.Accept = accept;
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            bool success = false;
            byte[] result = null;
            try
            {
                result = webClient.DownloadBytes(url);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    LastHttpStatusCode = HttpStatusCode.RequestTimeout;
                    LastErrorMessage = e.Message;
                }
                else
                {
                    LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                    LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, byte[]>(success, result);
        }

        private async Task<Tuple<bool, byte[]>> GetBytesAsync(string addressExtension, string accept = DefaultAcceptHeader)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return await GetBytesHttpClientAsync(addressExtension, accept);

            ExtendedWebClient webClient = new ExtendedWebClient();
            webClient.Accept = accept;
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            bool success = false;
            byte[] result = null;
            try
            {
                result = await webClient.DownloadBytesAsync(url);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    LastHttpStatusCode = HttpStatusCode.RequestTimeout;
                    LastErrorMessage = e.Message;
                }
                else
                {
                    LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                    LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, byte[]>(success, result);
        }

        private Tuple<bool, string> Post(string addressExtension, string body, string contentType = DefaultContentType)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return PostHttpClientAsync(addressExtension, body, DefaultAcceptHeader, contentType).Result;

            ExtendedWebClient webClient = new ExtendedWebClient();
            webClient.ContentType = contentType;
            webClient.Accept = DefaultAcceptHeader;
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            string result = string.Empty;
            bool success = false;
            try
            {
                result = webClient.UploadString(url, body);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    LastHttpStatusCode = HttpStatusCode.RequestTimeout;
                    LastErrorMessage = e.Message;
                }
                else
                {
                    LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                    LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(success, result);
        }

        private async Task<Tuple<bool, string>> PostAsync(string addressExtension, string body, string contentType = DefaultContentType)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return await PostHttpClientAsync(addressExtension, body, DefaultAcceptHeader, contentType);

            ExtendedWebClient webClient = new ExtendedWebClient();
            webClient.ContentType = contentType;
            webClient.Accept = DefaultAcceptHeader;
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            string result = string.Empty;
            bool success = false;
            try
            {
                result = await webClient.UploadStringAsync(url, body);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    LastHttpStatusCode = HttpStatusCode.RequestTimeout;
                    LastErrorMessage = e.Message;
                }
                else
                {
                    LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                    LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(success, result);
        }

        private Tuple<bool, string> Delete(string addressExtension)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return DeleteHttpClientAsync(addressExtension).Result;

            ExtendedWebClient webClient = new ExtendedWebClient();
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            string result = string.Empty;
            try
            {
                result = webClient.Delete(url);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    return new Tuple<bool, string>(true, result);
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    LastHttpStatusCode = HttpStatusCode.RequestTimeout;
                    LastErrorMessage = e.Message;
                }
                else
                {
                    LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                    LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(false, result);
        }

        private async Task<Tuple<bool, string>> DeleteAsync(string addressExtension)
        {
            if (UseHttpClientInsteadOfWebRequests)
                return await DeleteHttpClientAsync(addressExtension);

            ExtendedWebClient webClient = new ExtendedWebClient();
            webClient.Username = _username;
            webClient.Password = _password;
            webClient.Timeout = Timeout;
            webClient.Metadata = this.Metadata;
            webClient.OriginAddress = _localAddress;
            webClient.UseCompression = this.UseCompression;
            string url = _baseAddress + addressExtension;
            string result = string.Empty;
            try
            {
                result = await webClient.DeleteAsync(url);
                LastHttpStatusCode = webClient.StatusCode;
                if (webClient.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    return new Tuple<bool, string>(true, result);
                }
                else
                {
                    LastErrorMessage = webClient.StatusDescription;
                }
            }
            catch (WebException e)
            {
                LastHttpStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                LastErrorMessage = ((HttpWebResponse)e.Response).StatusDescription;
            }
            catch (Exception e)
            {
                LastHttpStatusCode = webClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(false, result);
        }

        #region Http methods using HttpClient

        private async Task<Tuple<bool, string>> GetStringHttpClientAsync(string addressExtension, string accept = DefaultAcceptHeader)
        {
            var url = _baseAddress + addressExtension;
            var success = false;
            string result = null;
            try
            {
                var response = await _extendedHttpClient.DownloadStringAsync(url, accept);
                result = response.Content;
                LastHttpStatusCode = response.StatusCode;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = response.StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = _extendedHttpClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(success, result);
        }

        private async Task<Tuple<bool, byte[]>> GetBytesHttpClientAsync(string addressExtension, string accept = DefaultAcceptHeader)
        {
            var url = _baseAddress + addressExtension;
            var success = false;
            byte[] result = null;
            try
            {
                var response = await _extendedHttpClient.DownloadBytesAsync(url, accept);
                result = response.Content;
                LastHttpStatusCode = response.StatusCode;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = response.StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = _extendedHttpClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, byte[]>(success, result);
        }

        private async Task<Tuple<bool, string>> PostHttpClientAsync(string addressExtension, string body, string accept = DefaultAcceptHeader, string contentType = DefaultContentType)
        {
            var url = _baseAddress + addressExtension;
            var success = false;
            string result = null;
            try
            {
                var response = await _extendedHttpClient.UploadStringAsync(url, body, accept, contentType);
                result = response.Content;
                LastHttpStatusCode = response.StatusCode;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = response.StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = _extendedHttpClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(success, result);
        }

        private async Task<Tuple<bool, string>> DeleteHttpClientAsync(string addressExtension)
        {
            var url = _baseAddress + addressExtension;
            var success = false;
            string result = null;
            try
            {
                var response = await _extendedHttpClient.DeleteAsync(url);
                result = response.Content;
                LastHttpStatusCode = response.StatusCode;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LastErrorMessage = string.Empty;
                    success = true;
                }
                else
                {
                    LastErrorMessage = response.StatusDescription;
                }
            }
            catch (Exception e)
            {
                LastHttpStatusCode = _extendedHttpClient.StatusCode;
                LastErrorMessage = e.Message;
            }
            return new Tuple<bool, string>(success, result);
        }

        #endregion

        private T GetDeserialized<T>(string url)
        {
            Tuple<bool, string> result = GetString(url);
            if (result.Item1)
            {
                if (result.Item2 != string.Empty)
                {
                    return JsonConvert.DeserializeObject<T>(result.Item2);
                }
            }
            return default(T);
        }

        private async Task<T> GetDeserializedAsync<T>(string url)
        {
            Tuple<bool, string> result = await GetStringAsync(url);
            if (result.Item1)
            {
                if (result.Item2 != string.Empty)
                {
                    return JsonConvert.DeserializeObject<T>(result.Item2);
                }
            }
            return default(T);
        }

        private T PostDeserialized<T>(string url, string body, string contentType = DefaultContentType)
        {
            Tuple<bool, string> result = Post(url, body, contentType);
            if (result.Item1)
            {
                if (result.Item2 != string.Empty)
                {
                    return JsonConvert.DeserializeObject<T>(result.Item2);
                }
            }
            return default(T);
        }

        private async Task<T> PostDeserializedAsync<T>(string url, string body, string contentType = DefaultContentType)
        {
            Tuple<bool, string> result = await PostAsync(url, body, contentType);
            if (result.Item1)
            {
                if (result.Item2 != string.Empty)
                {
                    return JsonConvert.DeserializeObject<T>(result.Item2);
                }
            }
            return default(T);
        }

        private string GetLocalIPAddress()
        {
            try
            {

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.ToString();
                        }
                    }
                }
            }
            catch (NetworkInformationException)
            {
                // Running on platform where LocalIP is not available
            }
            return null;
        }
        #endregion
    }
}
