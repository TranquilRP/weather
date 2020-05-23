using NFive.SDK.Core.Controllers;
using System.Dynamic;

namespace TranquilRP.Weather.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public int WeatherChangeMins { get; set; } = 10;
		public int ClientUpdateSeconds { get; set; } = 1;
		public float ClientTransitionTime { get; set; } = 45.0f;
	}
}
