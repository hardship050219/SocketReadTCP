using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ZC.UI.Extensions;

namespace AppZC.UI.Layouts;

public partial class DefaultLayout : ApplicationLayoutView
{
	public DefaultLayout()
	{
		InitializeComponent();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		(DataContext as AppVM)?.AppBarSettings.Height = 40;
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		base.OnSizeChanged(e);
		(DataContext as AppVM)?.AppBarSettings.Height = 40;
	}

	public object? MainContent { get; protected set; }

	public override void SetMainContent(object? content)
	{
		var control = content as Control;
		if (control == null) return;
		control.RemoveFromParent();
		SetColumn(control, 1);
		SetRow(control, 1);
		(MainContent as Control)?.RemoveFromParent();
		MainContent = control;
		Children.Add(control);
	}
}