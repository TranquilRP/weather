using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;
using TranquilRP.Weather.Shared;

namespace TranquilRP.Weather.Server
{
	[PublicAPI]
	public class WeatherController : ConfigurableController<Configuration>
	{
		private readonly ICommunicationManager comms;
		private static Dictionary<int, List<int>> WeatherSeasons;
		private static Dictionary<int, string> WeatherTypes;
		private static Dictionary<int, List<string>> WeatherZones;
		private static Dictionary<int, Dictionary<int, List<string>>> WeatherSystems;
		private static Dictionary<string, string> ActiveWeatherPattern;
		private bool stop = false;

		public WeatherController(ILogger logger, Configuration configuration, ICommunicationManager comms) : base(logger, configuration)
		{
			this.comms = comms;
			// Send configuration when requested
			comms.Event(WeatherEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));

			comms.Event(WeatherEvents.Pull).FromClients().OnRequest(e => e.Reply(Pull()));

			WeatherSeasons = new Dictionary<int, List<int>>();
			WeatherTypes = new Dictionary<int, string>();
			WeatherZones = new Dictionary<int, List<string>>();
			WeatherSystems = new Dictionary<int, Dictionary<int, List<string>>>();
			ActiveWeatherPattern = new Dictionary<string, string>();

			// Set seasons
			WeatherSeasons.Add(0, new List<int> { 3, 4, 5 }); // Spring
			WeatherSeasons.Add(1, new List<int> { 6, 7, 8 }); // Summer
			WeatherSeasons.Add(2, new List<int> { 9, 10, 11 }); // Fall
			WeatherSeasons.Add(3, new List<int> { 12, 1, 2 }); // Winter

			// Set weather types
			WeatherTypes.Add(0, "CLEAR");
			WeatherTypes.Add(1, "EXTRASUNNY");
			WeatherTypes.Add(2, "CLOUDS");
			WeatherTypes.Add(3, "OVERCAST");
			WeatherTypes.Add(4, "RAIN");
			WeatherTypes.Add(5, "CLEARING");
			WeatherTypes.Add(6, "THUNDER");
			WeatherTypes.Add(7, "SMOG");
			WeatherTypes.Add(8, "FOGGY");
			WeatherTypes.Add(9, "XMAS");
			WeatherTypes.Add(10, "SNOWLIGHT");

			// set weather zones
			WeatherZones.Add(0, new List<string> { "TERMINA", "ELYSIAN", "AIRP", "BANNING", "DESOL", "RANCHO", "STRAW", "CYPRE", "SANAND" }); // South LS
			WeatherZones.Add(1, new List<string> { "MURRI", "LMESA", "SKID", "LEGSQU", "TEXTI", "PBOX", "KOREAT" }); // Central LS
			WeatherZones.Add(2, new List<string> { "MIRR", "VINE", "EAST_V", "DTVINE", "ALTA", "HAWICK", "DOWNT", "BURTON", "ROCKF", "MOVIE", "DELPE", "MORN", "RICHM", "GOLF", "WVINE", "HORS", "LACT", "LDAM" }); // North LS
			WeatherZones.Add(3, new List<string> { "BEACH", "VESP", "VCANA", "DELBE", "PBLUFF" }); // LS Beaches
			WeatherZones.Add(4, new List<string> { "EBURO", "PALHIGH", "NOOSE", "TATAMO" }); // Eastern Valley
			WeatherZones.Add(5, new List<string> { "BANHAMC", "BANHAMCA", "CHU", "TONGVAH" }); // Coastal Beaches
			WeatherZones.Add(6, new List<string> { "CHIL", "GREATC", "RGLEN", "TONGVAV" }); // North LS Hills
			WeatherZones.Add(7, new List<string> { "PALMPOW", "WINDF", "RTRACK", "JAIL", "HARMO", "DESRT", "SANDY", "ZQ_UAR", "HUMLAB", "SANCHIA", "GRAPES", "ALAMO", "SLAB", "CALAFAB" }); // Grand Senora Desert
			WeatherZones.Add(8, new List<string> { "MTGORDO", "ELGORL", "BRADP", "BRADT", "MTCHIL", "GALFISH" }); // Northern Mountains
			WeatherZones.Add(9, new List<string> { "LAGO", "ARMYB", "NCHU", "CANNY", "MTJOSE", "CCREAK" }); // Zancudo
			WeatherZones.Add(10, new List<string> { "CMSW", "PALCOV", "OCEANA", "PALFOR", "PALETO", "PROCOB" }); // Palteo

			// set weather systems
			WeatherSystems.Add(0, new Dictionary<int, List<string>> { // South LS
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[3], WeatherTypes[3] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[9], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(1, new Dictionary<int, List<string>> { // Central LS
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(2, new Dictionary<int, List<string>> { // North LS
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(3, new Dictionary<int, List<string>> { // LS Beaches
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(4, new Dictionary<int, List<string>> { // Eastern Valley
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(5, new Dictionary<int, List<string>> { // Coastal Beaches
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(6, new Dictionary<int, List<string>> { // North LS Hills
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(7, new Dictionary<int, List<string>> { // Grand Senora Desert
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[1], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(8, new Dictionary<int, List<string>> { // Northern Mountains
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(9, new Dictionary<int, List<string>> { // Zancudo
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherSystems.Add(10, new Dictionary<int, List<string>> { // Palteo
                { 0, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2], WeatherTypes[3] } }, // Spring
                { 1, new List<string> { WeatherTypes[0], WeatherTypes[1], WeatherTypes[2] } }, // Summer
                { 2, new List<string> { WeatherTypes[2], WeatherTypes[3], WeatherTypes[4], WeatherTypes[5], WeatherTypes[6], WeatherTypes[7] } }, // Fall
                { 3, new List<string> { WeatherTypes[7], WeatherTypes[8], WeatherTypes[10], WeatherTypes[10] } } // Winter
            });

			WeatherUpdate(); // start the updater.

		}
		public Dictionary<string, string> Pull()
		{
			return ActiveWeatherPattern;
		}

		public void Update()
		{
			this.Logger.Debug("Sending Weather update to clients.");
			this.comms.Event(WeatherEvents.Update).ToClients().Emit(ActiveWeatherPattern);
		}

		public void WeatherUpdate()
		{
			Task.Factory.StartNew(async () =>
			{
				while (!this.stop)
				{
					this.Logger.Debug("Generating Server Weather");
					GeneratePattern();
					Update();
					await Task.Delay(TimeSpan.FromMinutes(Configuration.WeatherChangeMins));
				}
			});
		}

		public static string GenerateWeather(int location, int season)
		{
			var random = new Random();
			var index = random.Next(0, WeatherSystems[location][season].Count);

			return WeatherSystems[location][season][index];
		}

		public static void GeneratePattern()
		{
			int currentSeason = 0;
			ActiveWeatherPattern.Clear();

			// Get the month
			int currentMonth = DateTime.Now.Month;

			// Set the season
			foreach (KeyValuePair<int, List<int>> wSeasons in WeatherSeasons)
			{
				if (wSeasons.Value.Contains(currentMonth))
				{
					currentSeason = wSeasons.Key;
					break;
				}
			}

			foreach (KeyValuePair<int, List<string>> location in WeatherZones) // loop through zones
			{
				string weather = GenerateWeather(location.Key, currentSeason);

				foreach (string loc in location.Value) // loop through each location within the zone
				{
					ActiveWeatherPattern.Add(loc, weather); // set the active weather in a zone.
				}
			}
		}
	}
}
