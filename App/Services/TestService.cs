using ZC.Collections;
using ZC.KvStorage;
using ZC.Mvvm;

namespace AppZC.Services;

[ObservableObject, AddToIOC(Lifetime = LifetimeType.Singleton)]
public class TestService() : MainTaskService(nameof(TestService)), IAutoStartTaskService
{
	public required IKeyValueStorage Storage { get; init; }
	public required ILogger Logger { get; set; }
	public ObservableList<string> ValueList { get; set; } = [];
	public ObservableListDictionary<string, DateTime> ValueDict { get; } = new(t => t.ToString("yyyy-MM-dd HH:mm:ss"));

	protected override async Task Main(CancellationToken ctk)
	{
		while (ctk.IsCancellationRequested == false)
		{
			ValueDict.Add(DateTime.Now);
			ValueList.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			await Task.Delay(2000, ctk);
		}
	}
}