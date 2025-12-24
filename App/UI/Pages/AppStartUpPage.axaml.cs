// ReSharper disable CheckNamespace

namespace AppZC.UI;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class AppStartUpPage : UserControl
{
	public AppStartUpPage()
	{
		InitializeComponent();
	}
}