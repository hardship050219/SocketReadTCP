using System.Text.Json.Nodes;
using ZC.CFG;
using ZC.DB;
using ZC.Tasks;

namespace AppZC.Configs;

public class AppConfig : ConfigBase
{
	public string PluginLibPath { get; set; } = "Plugins";
	public DatabaseConnectionConfig Database { get; set; } = null!;
	public TaskServiceHostOptions TaskServiceHostOptions { get; set; } = null!;

	public JsonObject? GarnetServer { get; set; }
}