using System;
using System.Linq;
using System.Threading.Tasks;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Masterloop.Core.Types.Firmware;
using Masterloop.Core.Types.Observations;
using Xunit;

namespace Masterloop.Plugin.Application.Tests
{
    /// <summary>
    /// TODO: Will need to work on new testing work-flow where a new template and devices are created on the fly and do testing against them
    /// </summary>
    public class Rest : ApplicationBase
    {
        [Fact]
        public void GetAllTemplates()
        {
            DeviceTemplate[] templates = GetMCSAPI().GetTemplates();
            Assert.NotNull(templates);
            Assert.Contains(GetTID(), templates.Select(d => GetTID()));
        }

        [Fact]
        public void GetTemplate()
        {
            DeviceTemplate template = GetMCSAPI().GetTemplate(GetTID());
            Assert.NotNull(template);
            Assert.NotEmpty(template.Observations);
            Assert.NotEmpty(template.Settings);
            Assert.NotEmpty(template.Commands);
            Assert.NotEmpty(template.Pulses);
            Assert.Equal(GetTID(), template.Id);
        }

        [Fact]
        public void GetTemplateDevices()
        {
            Device[] devices = GetMCSAPI().GetTemplateDevices(GetTID());
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
        }

        [Fact]
        public void GetDevicelets()
        {
            Devicelet[] devicelets = GetMCSAPI().GetDevicelets(true);
            Assert.NotNull(devicelets);
            Assert.Contains(GetMID(), devicelets.Select(d => d.MID));
        }

        [Fact]
        public void GetAllDevices()
        {
            Device[] devices = GetMCSAPI().GetDevices(false);
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
        }

        [Fact]
        public async Task GetAllDevicesWithDetails()
        {
            DetailedDevice[] devices = await GetMCSAPI(true).GetDevicesAsync(true, true);
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
        }

        [Fact]
        public async Task GetDeviceDetails()
        {
            Device device = await GetMCSAPI().GetDeviceDetailsAsync(GetMID());
            Assert.NotNull(device);
            Assert.Equal(GetMID(), device.MID);
        }

        [Fact]
        public async Task GetSecureDeviceDetails()
        {
            SecureDetailedDevice device = await GetMCSAPI().GetSecureDeviceDetailsAsync(GetMID());
            Assert.NotNull(device);
            Assert.NotNull(device.PreSharedKey);
            Assert.Equal(GetMID(), device.MID);
        }

        [Fact]
        public async Task GetDeviceTemplate()
        {
            DeviceTemplate template = await GetMCSAPI().GetDeviceTemplateAsync(GetMID());
            Assert.NotNull(template);
            Assert.NotEmpty(template.Observations);
            Assert.NotEmpty(template.Settings);
            Assert.NotEmpty(template.Commands);
            Assert.NotEmpty(template.Pulses);
            Assert.Equal(GetTID(), template.Id);
        }

        /*[Fact]
        public void CreateDevice()
        {
            NewDevice newDevice = new NewDevice()
            {
                TemplateId = GetTID(),
                Name = $"CreateDevice on {DateTime.UtcNow:o}",
                Description = $"CreateDevice on {DateTime.UtcNow:o}",
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };
            DetailedDevice detailedDevice = GetMCSAPI().CreateDevice(newDevice);
            Assert.True(detailedDevice != null);
        }*/

        [Fact]
        public async Task GetDeviceLatestLoginTimestamp()
        {
            DateTime? timestamp = await GetMCSAPI().GetLatestLoginTimestampAsync(GetMID());
            Assert.NotNull(timestamp);
        }

        [Fact]
        public async Task GetCurrentDeviceTemplateFirmwareDetails()
        {
            FirmwareReleaseDescriptor firmware = await GetMCSAPI().GetCurrentDeviceTemplateFirmwareDetailsAsync(GetTID());
            Assert.NotNull(firmware);
        }

        [Fact]
        public void GetDeviceTemplateFirmwareVariants()
        {
            var variants = GetMCSAPI().GetDeviceTemplateFirmwareVariants(GetTID());
            Assert.NotNull(variants);
        }

        [Fact]
        public async Task GetDeviceTemplateFirmwareVariantsAsync()
        {
            var variants = await GetMCSAPI().GetDeviceTemplateFirmwareVariantsAsync(GetTID());
            Assert.NotNull(variants);
        }

        [Fact]
        public void GetCurrentDeviceTemplateVariantFirmwareDetails()
        {
            var firmwareVariantId = 0;
            var result = GetMCSAPI().GetCurrentDeviceTemplateVariantFirmwareDetails(GetTID(), firmwareVariantId);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCurrentDeviceTemplateVariantFirmwareDetailsAsync()
        {
            var firmwareVariantId = 0;
            var result = await GetMCSAPI().GetCurrentDeviceTemplateVariantFirmwareDetailsAsync(GetTID(), firmwareVariantId);
            Assert.NotNull(result);
        }

        [Fact]
        public void SendDeviceCommand()
        {
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(GetMCSAPI().SendDeviceCommand(GetMID(), cmd));
        }

        [Fact]
        public async Task SendMultipleDeviceCommands()
        {
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            var commandPackage = new CommandsPackage
            {
                Commands = new[] { cmd },
                MID = GetMID()
            };
            Assert.True(await GetMCSAPI().SendMultipleDeviceCommandAsync(new[] { commandPackage }));
        }

        [Fact]
        public void SendDeviceCommandWithMetadata()
        {
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            var mcs = GetMCSAPI();
            mcs.Metadata = new ApplicationMetadata()
            {
                Application = "Tests.ServerConnection",
                Reference = "SendDeviceCommandWithMetadata"
            };
            Assert.True(mcs.SendDeviceCommand(GetMID(), cmd));
        }

        [Fact]
        public async Task SendMultipleDeviceCommandsWithMetadata()
        {
            var mcs = GetMCSAPI();
            mcs.Metadata = new ApplicationMetadata()
            {
                Application = "Tests.ServerConnection",
                Reference = "SendDeviceCommandWithMetadata"
            };
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            var commandPackage = new CommandsPackage
            {
                Commands = new[] { cmd },
                MID = GetMID()
            };
            Assert.True(await mcs.SendMultipleDeviceCommandAsync(new[] { commandPackage }));
        }

        [Fact]
        public async Task GetDeviceCurrentObservation()
        {
            var observation = await GetMCSAPI().GetCurrentObservationAsync(GetMID(), MLTEST.Constants.Observations.BoolTest, DataType.Boolean);
            Assert.IsType<BooleanObservation>(observation);
        }

        [Fact]
        public async Task GetDeviceCurrentObservations()
        {
            var observations = await GetMCSAPI().GetCurrentObservationsAsync(GetMID());
            Assert.NotNull(observations);
        }

        [Fact]
        public async Task GetDeviceObservations()
        {
            var observations = await GetMCSAPI(false).GetObservationsAsync(GetMID(), MLTEST.Constants.Observations.BoolTest,
                DataType.Boolean, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
            Assert.NotNull(observations);
        }

        [Fact]
        public async Task GetDeviceCommandHistory()
        {
            SendDeviceCommand();
            var commandHistory = await GetMCSAPI().GetDeviceCommandHistoryAsync(GetMID(), DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow);
            Assert.NotEmpty(commandHistory);
            Assert.Equal(CommandStatus.Sent, commandHistory.First().Status);
        }

        [Fact]
        public async Task GetDeviceSettings()
        {
            var settings = await GetMCSAPI().GetSettingsAsync(GetMID());
            Assert.NotNull(settings);
        }

        [Fact]
        public void GetNonExistingPersistentWhitelist()
        {
            var devices = GetMCSAPI().GetPersistentSubscriptionWhitelist(GetPersistentSubscriptionKey());
            Assert.Null(devices);
        }

        [Fact]
        public async void GetNonExistingPersistentWhitelistAsync()
        {
            var devices = await GetMCSAPI().GetPersistentSubscriptionWhitelistAsync(GetPersistentSubscriptionKey());
            Assert.Null(devices);
        }

        [Fact]
        public void GetExistingPersistentWhitelist()
        {
            var devices = GetMCSAPI().GetPersistentSubscriptionWhitelist(GetPersistentSubscriptionKey());
            Assert.True(devices.Length == 1);
        }

        [Fact]
        public void CreatePersistentWhitelist()
        {
            var result = GetMCSAPI().CreatePersistentSubscriptionWhitelist(GetPersistentSubscriptionKey());
            Assert.True(result);
        }

        [Fact]
        public async void CreatePersistentWhitelistAsync()
        {
            var result = await GetMCSAPI().CreatePersistentSubscriptionWhitelistAsync(GetPersistentSubscriptionKey());
            Assert.True(result);
        }
    }
}
