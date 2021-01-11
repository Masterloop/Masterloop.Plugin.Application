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
    }
}
