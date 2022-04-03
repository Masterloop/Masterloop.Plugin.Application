using System;

namespace Masterloop.Plugin.Application
{
    internal class MasterloopApiUrlHelper
    {
        private const string AddressTemplates = "/api/templates";
        private const string AddressTemplate = "/api/templates/{0}";
        private const string AddressTemplateDevices = "/api/templates/{0}/devices";
        private const string AddressTenantTemplates = "/api/tenants/{0}/templates";
        private const string AddressDevices = "/api/devices";
        private const string AddressDevice = "/api/devices/{0}";
        private const string AddressDevicesWithMetadata = "/api/devices?includeMetadata={0}";
        private const string AddressDevicesWithMetadataAndDetails = "/api/devices?includeMetadata={0}&includeDetails={1}";
        private const string AddressDeviceDetails = "/api/devices/{0}/details";
        private const string AddressDeviceSecureDetails = "/api/devices/{0}/securedetails";
        private const string AddressDeviceTemplate = "/api/devices/{0}/template";
        private const string AddressDeviceObservationsCurrent = "/api/devices/{0}/observations/current2";
        private const string AddressDeviceObservationCurrent = "/api/devices/{0}/observations/{1}/current";
        private const string AddressDeviceObservations = "/api/devices/{0}/observations/{1}/observations?fromTimestamp={2}&toTimestamp={3}";
        private const string AddressDeviceCreateCommand = "/api/devices/{0}/commands/{1}";
        private const string AddressDeviceCommandQueue = "/api/devices/{0}/commands/queue";
        private const string AddressDeviceCommandHistory = "/api/devices/{0}/commands?fromTimestamp={1}&toTimestamp={2}";
        private const string AddressDeviceSettings = "/api/devices/{0}/settings";
        private const string AddressDeviceSettingsExpanded = "/api/devices/{0}/settings/expanded";
        private const string AddressDeviceLatestActivityTimestamp = "/api/devices/{0}/activity/latest/timestamp";
        private const string AddressDeviceTemplateFirmwareCurrent = "/api/templates/{0}/firmware/current";
        private const string AddressDeviceTemplateFirmwareVariants = "/api/templates/{0}/firmware/variants";
        private const string AddressDeviceTemplateFirmwareVariantsCurrent = "/api/templates/{0}/firmware/{1}/current";
        private const string AddressToolsLiveConnect = "/api/tools/liveconnect";
        private const string AddressToolsLiveConnectTemporaryIdentified = "/api/tools/liveconnect/{0}";
        private const string AddressToolsLiveConnectPersistent = "/api/tools/liveconnect/persistent";
        private const string AddressToolsLiveConnectPersistentIdentified = "/api/tools/liveconnect/persistent/{0}";
        private const string AddressToolsLiveConnectPersistentAddDevice = "/api/tools/liveconnect/persistent/{0}/devices";
        private const string AddressToolsLiveConnectPersistentCreateWhitelist = "/api/tools/liveconnect/persistent/{0}/whitelist";
        private const string AddressToolsLiveConnectPersistentRemoveDevice = "/api/tools/liveconnect/persistent/{0}/devices/{1}";
        private const string AddressDevicePulsePeriod = "/api/devices/{0}/pulse/{1}?fromTimestamp={2}&toTimestamp={3}";
        private const string AddressDevicePulsePeriodCurrent = "/api/devices/{0}/pulse/{1}/current";
        private const string AddressSnapshotCurrent = "/api/tools/snapshot/current";
        private const string AddressToolsMultiSettings = "/api/tools/multisettings";
        private const string AddressToolsMultiCommands = "/api/tools/multicommands";
        private const string AddressPing = "/api/tools/ping";

        private readonly string _baseAddress;

        public MasterloopApiUrlHelper(string host, bool useHttps)
        {
            _baseAddress = useHttps ? $"https://{host}" : $"http://{host}";
        }

        public string TemplatesUrl()
        {
            return AddressTemplates;
        }

        public string TemplateUrl(string templateId)
        {
            return string.Format(AddressTemplate, templateId);
        }

        public string TemplateDevicesUrl(string templateId)
        {
            return string.Format(AddressTemplateDevices, templateId);
        }

        public string TenantTemplatesUrl(int tenantId)
        {
            return string.Format(AddressTenantTemplates, tenantId);
        }

        public string DevicesUrl()
        {
            return AddressDevices;
        }

        public string DeviceUrl(string deviceId)
        {
            return string.Format(AddressDevice, deviceId);
        }

        public string DevicesWithMetadataUrl(bool includeMetadata)
        {
            return string.Format(AddressDevicesWithMetadata, includeMetadata);
        }

        public string DevicesWithMetadataAndDetailsUrl(bool includeMetadata, bool includeDetails)
        {
            return string.Format(AddressDevicesWithMetadataAndDetails, includeMetadata, includeDetails);
        }

        public string DeviceDetailsUrl(string deviceId)
        {
            return string.Format(AddressDeviceDetails, deviceId);
        }

        public string DeviceSecureDetailsUrl(string deviceId)
        {
            return string.Format(AddressDeviceSecureDetails, deviceId);
        }

        public string DeviceTemplateUrl(string deviceId)
        {
            return string.Format(AddressDeviceTemplate, deviceId);
        }

        public string DeviceObservationsCurrentUrl(string deviceId)
        {
            return string.Format(AddressDeviceObservationsCurrent, deviceId);
        }

        public string DeviceObservationCurrentUrl(string deviceId, int observationId)
        {
            return string.Format(AddressDeviceObservationCurrent, deviceId, observationId);
        }

        public string DeviceObservationsUrl(string deviceId, int observationId, DateTime from, DateTime to)
        {
            return string.Format(AddressDeviceObservations, deviceId, observationId, from.ToString("o"), to.ToString("o"));
        }

        public string DeviceCreateCommandUrl(string deviceId, int commandId)
        {
            return string.Format(AddressDeviceCreateCommand, deviceId, commandId);
        }

        public string DeviceCommandQueueUrl(string deviceId)
        {
            return string.Format(AddressDeviceCommandQueue, deviceId);
        }

        public string DeviceCommandHistoryUrl(string deviceId, DateTime from, DateTime to)
        {
            return string.Format(AddressDeviceCommandHistory, deviceId, from.ToString("o"), to.ToString("o"));
        }

        public string DeviceSettingsUrl(string deviceId)
        {
            return string.Format(AddressDeviceSettings, deviceId);
        }

        public string DeviceSettingsExpandedUrl(string deviceId)
        {
            return string.Format(AddressDeviceSettingsExpanded, deviceId);
        }

        public string DeviceLatestActivityTimestampUrl(string deviceId)
        {
            return string.Format(AddressDeviceLatestActivityTimestamp, deviceId);
        }

        public string DeviceTemplateFirmwareCurrentUrl(string templateId)
        {
            return string.Format(AddressDeviceTemplateFirmwareCurrent, templateId);
        }

        public string DeviceTemplateFirmwareVariantsUrl(string templateId)
        {
            return string.Format(AddressDeviceTemplateFirmwareVariants, templateId);
        }

        public string DeviceTemplateFirmwareVariantsCurrentUrl(string templateId, int variantId)
        {
            return string.Format(AddressDeviceTemplateFirmwareVariantsCurrent, templateId, variantId);
        }

        public string ToolsLiveConnectUrl()
        {
            return AddressToolsLiveConnect;
        }

        public string ToolsLiveConnectTemporaryIdentifiedUrl(string subscriptionKey)
        {
            return string.Format(AddressToolsLiveConnectTemporaryIdentified, subscriptionKey);
        }

        public string ToolsLiveConnectPersistentUrl()
        {
            return AddressToolsLiveConnectPersistent;
        }

        public string ToolsLiveConnectPersistentIdentifiedUrl(string subscriptionKey)
        {
            return string.Format(AddressToolsLiveConnectPersistentIdentified, subscriptionKey);
        }

        public string ToolsLiveConnectPersistentAddDeviceUrl(string subscriptionKey)
        {
            return string.Format(AddressToolsLiveConnectPersistentAddDevice, subscriptionKey);
        }

        public string ToolsLiveConnectPersistentCreateWhitelistUrl(string subscriptionKey)
        {
            return string.Format(AddressToolsLiveConnectPersistentCreateWhitelist, subscriptionKey);
        }

        public string ToolsLiveConnectPersistentRemoveDeviceUrl(string subscriptionKey, string deviceId)
        {
            return string.Format(AddressToolsLiveConnectPersistentRemoveDevice, subscriptionKey, deviceId);
        }

        public string DevicePulsePeriodUrl(string deviceId, int pulseId, DateTime from, DateTime to)
        {
            return string.Format(AddressDevicePulsePeriod, deviceId, pulseId, from.ToString("o"), to.ToString("o"));
        }

        public string DevicePulsePeriodCurrentUrl(string deviceId, int pulseId)
        {
            return string.Format(AddressDevicePulsePeriodCurrent, deviceId, pulseId);
        }

        public string SnapshotCurrentUrl()
        {
            return AddressSnapshotCurrent;
        }

        public string ToolsMultiSettingsUrl()
        {
            return AddressToolsMultiSettings;
        }

        public string ToolsMultiCommandsUrl()
        {
            return AddressToolsMultiCommands;
        }

        public string PingUrl()
        {
            return AddressPing;
        }
    }
}