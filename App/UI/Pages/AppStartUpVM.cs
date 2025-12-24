using Avalonia.Threading;
using ZC.Mvvm;

// ReSharper disable CheckNamespace


namespace AppZC.UI;

[AddToIOC(Lifetime = LifetimeType.Singleton, AliasMapTo = [typeof(IAppStartUpVM)], BeginAddAop = nameof(CanAddToIOC))]
public partial class AppStartUpVM : ViewModel<AppStartUpPage>, IAppStartUpVM
{
	public static bool CanAddToIOC(IApp app) => app.Properties.ContainsKey("UiInitializer");

	public Rect WindowSize { get; set; }

	public (double MaxProcess, int UseTimeOfMs, DateTime SetTime) ProcessInfo { get; set; }

	private IDisposable? UpdateTimerHandle { get; set; }
	[ObservableProperty] public partial double Progress { get; set; } = 0;
	public event Func<Task>? OnCompleted;
	public bool IsCompleted { get; private set; } = false;

	public void SetProgress(double maxProgress, int totalTime)
	{
		if (UpdateTimerHandle == null)
			Start();
		ProcessInfo = (maxProgress, totalTime, DateTime.Now);
	}

	private void Start()
	{
		UpdateTimerHandle?.Dispose();
		UpdateTimerHandle = DispatcherTimer.Run(OnUpdate, TimeSpan.FromMilliseconds(30), DispatcherPriority.Default);
	}

	private bool OnUpdate()
	{
		var elapsedTimeMs = (DateTime.Now - ProcessInfo.SetTime).TotalMilliseconds;
		var targetProgress = ProcessInfo.MaxProcess * elapsedTimeMs / ProcessInfo.UseTimeOfMs;
		if (Progress < targetProgress && Progress < ProcessInfo.MaxProcess)
		{
			Progress += Math.Max(targetProgress - Progress, 0);
			return true;
		}

		if (Progress < 100)
			return true;

		UpdateTimerHandle = null;
		IsCompleted = true;
		if (OnCompleted != null)
			Dispatcher.UIThread.InvokeAsync(OnCompleted);
		return false;
	}
}