namespace AppZC.UI;

[AddToIOC(Lifetime = LifetimeType.Singleton, AliasMapTo = [typeof(ApplicationView)])]
public sealed partial class AppView : ApplicationView
{
	public AppView() => InitializeComponent();
}