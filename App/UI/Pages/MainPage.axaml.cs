using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UiView = ZC.UI.UiView;

namespace AppZC.UI.Pages;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class MainPage : UiView
{
	public MainPage() => InitializeComponent();
	
	private ScrollViewer? _sendDataScrollViewer;
	private ScrollViewer? _receivedDataScrollViewer;
	
	public ScrollViewer? GetSendDataScrollViewer() => _sendDataScrollViewer;
	public ScrollViewer? GetReceivedDataScrollViewer() => _receivedDataScrollViewer;
	
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		_sendDataScrollViewer = this.FindControl<ScrollViewer>("SendDataScrollViewer");
		_receivedDataScrollViewer = this.FindControl<ScrollViewer>("ReceivedDataScrollViewer");
	}
}