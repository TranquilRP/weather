using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using TranquilRP.Weather.Server.Storage;

namespace TranquilRP.Weather.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
