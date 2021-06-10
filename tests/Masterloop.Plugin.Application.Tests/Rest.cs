using System;
using System.Linq;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Xunit;

namespace Masterloop.Plugin.Application.Tests
{
    public class Rest : ApplicationBase
    {
        [Fact]
        public void GetAllDevices()
        {
            Device[] devices = GetMCSAPI().GetDevices(false);
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
        public void GetTemplateDevices()
        {
            Device[] devices = GetMCSAPI().GetTemplateDevices(GetTID());
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
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
            MasterloopServerConnection mcs = GetMCSAPI();
            mcs.Metadata = new ApplicationMetadata()
            {
                Application = "Tests.ServerConnection",
                Reference = "SendDeviceCommandWithMetadata"
            };
            Assert.True(mcs.SendDeviceCommand(GetMID(), cmd));
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
