using System;
using System.Threading;
using Masterloop.Core.Types.Commands;
using Xunit;

namespace Masterloop.Plugin.Application.Tests
{
    public class Live : ApplicationBase
    {
        [Fact]
        public void ConnectDisconnect()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        [Fact]
        public void SendCommand()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        /*[Fact]
        public void SendCommandPersistent()
        {
            MasterloopLiveConnection live = GetMCSPersistentConnection();
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }*/

        [Fact]
        public void SendCommandWithMetadata()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            live.Metadata = new ApplicationMetadata()
            {
                Application = "Masterloop.Plugin.Application.Tests",
                Reference = "Live.SendCommandWithMetadata"
            };
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        [Fact]
        public void SendCommandWithResponse()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            live.UseAutomaticCallbacks = false;
            live.Metadata = new ApplicationMetadata()
            {
                Application = "Masterloop.Plugin.Application.Tests",
                Reference = "Live.SendCommandWithMetadata"
            };
            live.RegisterCommandResponseHandler(null, MLTEST.Constants.Commands.PollSingle, OnPollCommandResponseReceived);
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            Thread.Sleep(5000);
            live.Fetch();
            Assert.False(string.IsNullOrEmpty(live.LastFetchedMessageRoutingKey));
            Assert.False(string.IsNullOrEmpty(live.LastFetchedMessageBody));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        private void OnPollCommandResponseReceived(string MID, CommandResponse cmdResponse)
        {
        }

        [Fact]
        public void SendCommandWithTransactionCommit()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            live.UseAtomicTransactions = false;
            live.UseAutomaticCallbacks = false;
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.PublishBegin());
            Assert.True(live.SendCommand(GetMID(), cmd));
            Thread.Sleep(5000);
            cmd.Id = MLTEST.Constants.Commands.Simple;
            cmd.Timestamp = DateTime.UtcNow;
            cmd.Arguments = null;
            Assert.True(live.SendCommand(GetMID(), cmd));
            Assert.True(live.PublishCommit());
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        [Fact]
        public void SendCommandWithTransactionRollback()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            live.UseAtomicTransactions = false;
            live.UseAutomaticCallbacks = false;
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = MLTEST.Constants.Commands.PollSingle,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = new CommandArgument[] {
                    new CommandArgument() { Id = MLTEST.Constants.Commands.PollSingleArguments.ObsId, Value = "4" }
                }
            };
            Assert.True(live.PublishBegin());
            Assert.True(live.SendCommand(GetMID(), cmd));
            Assert.True(live.PublishRollback());
            live.Disconnect();
            Assert.False(live.IsConnected());
        }
    }
}
