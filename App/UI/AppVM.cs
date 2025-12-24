using AppZC.UI.Layouts;
using AppZC.UI.Pages;
using AppZC.UI.Pages.Internal;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ZC.Collections;
using ZC.Mvvm;
using ZC.UI.Extensions;
using ZC.UI.Models;

namespace AppZC.UI;

[ApiController("/App")]
[AddToIOC(Lifetime = LifetimeType.Singleton,
	AliasMapTo = [typeof(IApplicationViewModel), typeof(ApplicationViewModel)])]
[ObservableObject(IncludeAllPartialProperty = true)]
public sealed partial class AppVM : ApplicationViewModel<AppView>
{
	public partial bool HasAdminRole { get; set; }

	public override IEnumerable<INavigationInfo> CreateNavigations() => new NavigationInfo[]
	{
		new("/主页") { ViewModel = typeof(MainVM), AllowClose = false },
		new("/关于") { ViewModel = typeof(AboutUsVM), AllowClose = false },
	};

	public required AppStartUpVM AppStartUpVM { get; init; }

	void @Test()
	{
		Toast.Show("123");
	}


	public partial object? SelectedNavData { get; set; }

	partial void OnSelectedNavDataChanged(object? value)
	{
		if (AppStartUpVM.IsCompleted == false) return;
		NavManager.Navigate(value);
	}


	[Inject]
	public void Initialize(AppStartUpVM appStartUpVM)
	{
		UiTick = 100;
		LayoutView = App.Current.IOC.Get<StartUpLayout>();
		View.TopLevelView?.IsVisible = false;
		appStartUpVM.OnCompleted += () =>
		{
			LayoutView = App.Current.IOC.Get<TopAppBarLayout>();
			View.TopLevelView?.IsVisible = true;
			NavManager.Navigate(SelectedNavData);
			return Task.CompletedTask;
		};
		NavManager.Navigate(appStartUpVM);
	}

	#region Basic

	public override partial IThemeInfo? SelectedTheme { get; set; }


	public void @ToggleGround()
	{
		if (NavManager.ActiveContent is UiView uiView) uiView.ShowBackgroundContent = !uiView.ShowBackgroundContent;
	}

	public void @ChangeColorTheme(IThemeInfo? themeInfo)
	{
		SelectedTheme = themeInfo;
	}

	public override IEnumerable<IThemeInfo> CreateThemes() => new List<ThemeInfo>()
	{
		new("默认", ThemeVariant.Default),
		new("亮色", ThemeVariant.Light),
		new("暗色", ThemeVariant.Dark),
		new("Aquatic", UiTheme.Aquatic),
		new("Desert", UiTheme.Desert),
		new("Dusk", UiTheme.Dusk),
		new("NightSky", UiTheme.NightSky)
	};

	partial void OnSelectedThemeChanged(IThemeInfo? value)
	{
		if (value is null) return;
		var app = Application.Current;
		if (app is null) return;
		app.RequestedThemeVariant = value.Theme;
		Notification.Show(new UiNotification(
			"主题已切换",
			$"主题切换到 {value.Name}",
			UiMessageType.Success));
	}

	#endregion
}