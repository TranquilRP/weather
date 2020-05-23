using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using CitizenFX.Core.Native;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using TranquilRP.Weather.Client.Overlays;
using TranquilRP.Weather.Shared;
using CitizenFX.Core;
using System.Collections.Generic;

namespace TranquilRP.Weather.Client
{
	[PublicAPI]
	public class WeatherService : Service
	{
		private Configuration config;
		private WeatherOverlay overlay;
		private Dictionary<string, string> LastSystem;
		//private int WeatherVersion = 0;
		private string LastWeather = String.Empty;
		private string LastZone = String.Empty;

		public WeatherService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			// Request server configuration
			this.config = await this.Comms.Event(WeatherEvents.Configuration).ToServer().Request<Configuration>();

			// Create overlay
			this.overlay = new WeatherOverlay(this.OverlayManager);

			// Pull the weather on connect
			this.LastSystem = await this.Comms.Event(WeatherEvents.Pull).ToServer().Request<Dictionary<string, string>>();

			// Handle update from the server
			this.Comms.Event(WeatherEvents.Update).FromServer().On<Dictionary<string, string>>((e, t) =>
			{
				UpdateWeather(t);
			});

			// Attach a tick handler
			this.Ticks.On(OnTick);
		}

		private async Task OnTick() // Periodically update the client based off of ClientUpdateSeconds
		{
			UpdateZone();
			await Delay(TimeSpan.FromSeconds(config.ClientUpdateSeconds));
		}

		private void UpdateWeather(Dictionary<string, string> NewWeather) // Update weather and adjust accordingly
		{
			LastSystem = NewWeather;
			UpdateZone();
		}

		private void UpdateZone() // Update weather based on zone
		{
			string NewZone = GetPedZone();

			try
			{
				if (LastZone != NewZone || LastWeather != LastSystem[NewZone])
				{
					if (LastWeather != LastSystem[NewZone])
					{
						this.Logger.Debug($"Player Zone: { LastZone } => { NewZone }");
						this.Logger.Debug($"Player Weather: { LastWeather } => { LastSystem[NewZone] }");

						TransitionWeather(LastSystem[NewZone]);
					}
				}

				LastZone = NewZone;
				LastWeather = LastSystem[NewZone];
			}
			catch (KeyNotFoundException e)
			{
				this.Logger.Debug($"Zone { NewZone } is not found in the last system recieved from the server");
			}
		}

		public string GetPedZone()
		{
			Vector3 coords = API.GetEntityCoords(API.GetPlayerPed(API.PlayerId()), true);
			return API.GetNameOfZone(coords.X, coords.Y, coords.Z);
		}

		public void TransitionWeather(string weather)
		{
			API.ClearOverrideWeather();
			API.ClearWeatherTypePersist();
			API.SetWeatherTypeOverTime(weather, config.ClientTransitionTime);
			API.SetWeatherTypePersist(weather);
		}
	}
}


