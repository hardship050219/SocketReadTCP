using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AppZC.UI;

[AddToIOC(Lifetime = LifetimeType.Singleton, AliasMapTo = [typeof(ApplicationWindow)])]
public partial class AppWindow : ApplicationWindow
{
	public AppWindow()
	{
		InitializeComponent();
	}
}