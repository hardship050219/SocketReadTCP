using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ZC.UI.Extensions;

namespace AppZC.UI.Layouts;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class StartUpLayout : ApplicationLayoutView
{
	public StartUpLayout()
	{
		InitializeComponent();
	}

	public object? MainContent { get; protected set; }

	public override void SetMainContent(object? content)
	{
		var control = content as Control;
		if (control == null) return;
		control.RemoveFromParent();
		SetRow(control, 2);
		(MainContent as Control)?.RemoveFromParent();
		MainContent = control;
		Children.Add(control);
	}
}