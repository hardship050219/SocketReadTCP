using System.Text.Json.Nodes;using AppZC.Models.BinStructs;
using Garnet;
using Newtonsoft.Json;
using ZC.BinaryStruct;
using ZC.EnhanceApp;
using ZC.DB;
using ZC.HttpSV;
using ZC.IO;
using ZC.KvStorage;
using ZC.KvStorage.DB;
using ZC.Net;
using JsonSerializer = System.Text.Json.JsonSerializer;

var plcStruct = new PlcStruct();
var iBinaryStructInfo = new PlcStruct().GetStructInfo();
var gridPanelChildDefinitions = new GridPanelChildDefinitions("100,*,100");
JsonConvert.DefaultSettings = () =>
{
	var settings = new JsonSerializerSettings();
	settings.Converters.Add(new AnyNewtonsoftJsonConverter());
	return settings;
};
AppCore.UnhandledException += (sender, eventArgs) =>
	Log.Fatal(eventArgs.ExceptionObject as Exception, "发生未处理异常! {obj} -> {err} CLR:{}",
		sender, (eventArgs.ExceptionObject as Exception)?.Message, eventArgs.IsTerminating);
var config = new AppConfig
{
	GarnetServer = new JsonObject { { "Port", 1234 }, { "Address", "127.0.0.1" }, { "MemorySize", "2g" } },
	Database = new DatabaseConnectionConfig
		{ DbType = DatabaseType.Sqlite, ConnectionString = "Data Source=AppData/AppData.db" },
	TaskServiceHostOptions = new TaskServiceHostOptions { MaxRemainStartCount = 1 }
};
config = Debugger.IsAttached ? config : AppCore.LoadConfig<AppConfig>();
var app = new App(config).UseLogger().UseUi(AvaApp.StartAsync)
	.AddToIOC(typeof(App).Assembly.GetTypes(), RegistrationMode.Override)
	.UseDatabase(config.Database).UseDbKeyValueStorage()
	.UseWebServer();

var storage = app.GetObject<IKeyValueStorage>() as DbKeyValueStorage;
storage.DbClient.CodeFirst.InitTables<DbKeyValueItem>();
var result = storage.SetValue("Def", new User() { });
app.TaskServiceManager.ServiceLifecycleEvents.OnEvent += [Inject](serv, status) =>
	Console.WriteLine($"[T-SERVICE] '{serv.Name}' is {status} | {serv.LifecycleError} {serv.TaskError}");
await app.Initialize().UnwarpAsync(errThrow: true, success: _ => { }, onError: r => { });
await app.Start().UnwarpAsync(errThrow: true, success: _ => { }, onError: r => { });
while (true) await Task.Delay(1000);

public sealed class App(AppConfig config) : AppCore<App, AppConfig>(config)
{
	protected override void OnObjectContainerInitialize(IObjectContainer ioc)
	{
		ioc.AddSingleton<ITaskServiceHostOptions>(creator: _ => Config.TaskServiceHostOptions);
		base.OnObjectContainerInitialize(ioc);
	}

	protected override async ValueTask<Result> OnInitialize(object? context = null, CancellationToken ctk = default)
	{
		await StartUi();
		if (context is Func<Task> task) await task();
		IOC.GetOrNull<IAppStartUpVM>()?.SetProgress(40, 500);
		return default;
	}

	protected override async ValueTask<Result> OnStart(object? context = null, CancellationToken ctk = default)
	{
		var splashVM = IOC.GetOrNull<IAppStartUpVM>();
		if (Config.GarnetServer != null)
		{
			var tempFileName = Path.GetTempFileName() + ".conf".Replace("\\", "/");
			var configText = JsonSerializer.Serialize(Config.GarnetServer);
			await File.WriteAllTextAsync(tempFileName, configText, ctk);
			var server = new GarnetServer([$"--config-import-path={tempFileName}"]);
			server.Start();
		}

		await StartTaskServices();
		var tcpServerSocket = new TcpStreamServerSocket("0.0.0.0", 80);
		var httpServer = IOC.Get<HttpServer>();
		tcpServerSocket.Acceptor = httpServer;
		// await tcpServerSocket.StartAsync().UnwarpAsync();
		splashVM?.SetProgress(100, 500);
		return default;
	}
}

public static partial class Program
{
	static Program()
	{
		// AppDomain.CurrentDomain.AssemblyResolve += DllInit.OnAssemblyResolve;
	}

	public static AppBuilder BuildAvaloniaApp() => AvaApp.Build();
}