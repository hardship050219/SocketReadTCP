using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI.Avalonia;

namespace AppZC.UI;

public class AvaApp : AvaloniaApplication
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		WindowsWebView.SetToDefaultWebView();
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = App.Current.IOC.Get<AppWindow>();
			desktop.MainWindow.Content = App.Current.IOC.Get<ApplicationViewModel>().AppView;
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = App.Current.IOC.Get<ApplicationViewModel>().AppView;
		}

		base.OnFrameworkInitializationCompleted();
		AvaloniaInitializedCompletionSource.SetResult();
	}

	public static Task StartAsync()
	{
		UiThread = new Thread(AvaloniaMain, 1024 * 1024 * 10) { Name = "AppUI", IsBackground = false };
		UiThread.TrySetApartmentState(ApartmentState.STA);
		UiThread.Start();
		return AvaloniaInitializedCompletionSource.Task;
	}

	private static void AvaloniaMain()
	{
		try
		{
			Build().StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
		}
		catch (Exception ex)
		{
			if (Debugger.IsAttached)
				Debugger.Break();
			Console.Error.WriteLine(ex);
		}
		finally
		{
			App.Current.Destroy().GetAwaiter().GetResult();
			Environment.Exit(0);
		}
	}

	public static AppBuilder Build() =>
		AppBuilder.Configure<AvaApp>().UseSkia().UsePlatformDetect().WithInterFont().LogToTrace().UseReactiveUI();
}