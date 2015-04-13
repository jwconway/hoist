﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Hive.Plugin.Actors.Plugin;
using Hive.Plugin.Messages.Control;
using Hive.Plugin.Plugin;
using Moq;
using Xunit;

namespace Hive.Plugin.Tests
{
    public class PluginControllerActor_Start_TestSpec : PluginControllerActor_Base_TestSpec
    {
	    public PluginControllerActor_Start_TestSpec() : base(new XunitAssertions())
	    {
	    }

		[Fact]
		private void Can_send_start_message()
		{
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(Mock.Of<IPluginAppStart>())));
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
		}

		[Fact]
		private void Can_send_start_message_and_recieve_acknowledgemnt()
		{
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(Mock.Of<IPluginAppStart>())));
			StartPlugin(pluginControllerActorRef);
		}

		[Fact]
		private void Can_send_start_message_and_recieve_acknowledgement_message_then_get_status()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			var pluginStatus = pluginControllerActorRef.Ask(new PluginAction<StatusRequest>()).Result;
			Assert.NotNull(pluginStatus);
		}

		[Fact]
		private void Send_start_request_to_plugin_get_ack_get_running_status()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
		}

		[Fact]
		private void Send_start_request_to_faulty_plugin_get_ack_get_faulted_status()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(pas => pas.StartPluginApp()).Throws(new Exception("IM FAKE!!!"));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			this.ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Faulted, pluginControllerActorRef);
		}

		[Fact]
		private void Send_start_request_to_slow_to_start_plugin_get_ack_get_starting_status_eventually_get_running()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(pas => pas.StartPluginApp()).Callback(() => Thread.Sleep(300));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			this.ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			pluginControllerActorRef.Tell(new PluginAction<StatusRequest>());
			WaitForStatus(PluginStatus.Starting, pluginControllerActorRef);
			WaitForStatus(PluginStatus.Running, pluginControllerActorRef);
		}

		[Fact]
		private void Send_start_request_to_paused_plugin_get_invalid_request()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<PauseRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Paused, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
		}

		[Fact]
		private void Send_start_request_to_starting_plugin_get_invalid_request()
		{
			var mre = new ManualResetEventSlim();
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(e => e.StartPluginApp()).Callback(() => mre.Wait(10000));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Starting, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
			mre.Set();
		}
		
		[Fact]
		private void Send_start_request_to_running_plugin_get_invalid_request()
		{
			var moqPluginStarter = new Mock<IPluginAppStart>();
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			WaitForStatus(PluginStatus.Running, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
		}
		
		[Fact]
		private void Send_start_request_to_pausing_plugin_get_invalid_request()
		{
			var mre = new ManualResetEventSlim();
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(e => e.PausePluginApp()).Callback(() => mre.Wait(10000));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<PauseRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Pausing, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
			mre.Set();
		}
		
		[Fact]
		private void Send_start_request_to_unpausing_plugin_get_invalid_request()
		{
			var mre = new ManualResetEventSlim();
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(e => e.UnPausePluginApp()).Callback(() => mre.Wait(10000));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<PauseRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Paused, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<UnPauseRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Unpausing, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
		}
		
		[Fact]
		private void Send_start_request_to_stopping_plugin_get_invalid_request()
		{
			var mre = new ManualResetEventSlim();
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(e => e.StopPluginApp()).Callback(() => mre.Wait(10000));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			StartPlugin(pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StopRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Stopping, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
		}
		
		[Fact]
		private void Send_start_request_to_faulted_plugin_get_invalid_request()
		{
			var mre = new ManualResetEventSlim();
			var moqPluginStarter = new Mock<IPluginAppStart>();
			moqPluginStarter.Setup(e => e.StartPluginApp()).Throws(new Exception("FAKE!!!"));
			var pluginControllerActorRef = ActorOf(Props.Create(() => new PluginControllerActor(moqPluginStarter.Object)));
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<AcknowledgedResponse>>();
			WaitForStatus(PluginStatus.Faulted, pluginControllerActorRef);
			pluginControllerActorRef.Tell(new PluginAction<StartRequest>());
			ExpectMsg<PluginResponse<NotSupportedResponse>>();
		}
    }
}
