using AppZC.UI.Pages.Internal;
using Avalonia.Interactivity;

namespace AppZC.UI;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class AboutUsPage : UserControl
{
	public AboutUsPage()
	{
		InitializeComponent();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		if (DataContext is AboutUsVM vm)
		{
			var launcher = TopLevel.GetTopLevel(this)?.Launcher;
			vm.Launcher = launcher;
		}
	}
}