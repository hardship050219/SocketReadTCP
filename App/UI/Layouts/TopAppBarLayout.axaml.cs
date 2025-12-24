using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ZC.UI.Extensions;

namespace AppZC.UI.Layouts;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class TopAppBarLayout : ApplicationLayoutView
{
	public TopAppBarLayout()
	{
		InitializeComponent();
		AppBarPanel.SizeChanged += (o, e) =>
		{
			var appVM = DataContext as ApplicationViewModel;
			if (appVM == null) return;
			appVM.AppBarSettings.Height = e.NewSize.Height;
			// if (appVM.AppView.DialogManager is Control dialogManager)
			// {
			// 	var margin = dialogManager.Margin;
			// 	dialogManager.Margin = new Thickness(margin.Left, appVM.AppBarSettings.Height, margin.Right, margin.Bottom);
			// }

			if (appVM.AppView.NotificationManager is Control notificationManager)
			{
				var margin = notificationManager.Margin;
				notificationManager.Margin =
					new Thickness(margin.Left, appVM.AppBarSettings.Height, margin.Right, margin.Bottom);
			}

			if (appVM.AppView.ToastManager is Control toastManager)
			{
				var margin = toastManager.Margin;
				toastManager.Margin = new Thickness(margin.Left, appVM.AppBarSettings.Height, margin.Right, margin.Bottom);
			}
		};
	}

	public object? MainContent { get; protected set; }

	public override void SetMainContent(object? content)
	{
		var control = content as Control;
		if (control == null) return;
		if (control.Parent == this)
			return;
		control.RemoveFromParent();
		Grid.SetRow(control, 2);
		(MainContent as Control)?.RemoveFromParent();
		MainContent = control;
		Children.Add(control);
	}
}