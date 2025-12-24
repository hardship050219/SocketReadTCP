using System.Windows.Input;
using Avalonia.Platform.Storage;
using ZC.Mvvm;
using Uri = System.Uri;


namespace AppZC.UI.Pages.Internal;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
public partial class AboutUsVM : ViewModel<AboutUsPage>
{
	private static readonly IReadOnlyDictionary<string, string> KeyToUrlMapping = new Dictionary<string, string>()
	{
	};

	public AboutUsVM()
	{
		NavigateCommand = new AsyncRelayCommand<string>(OnNavigateAsync);
	}

	public ICommand NavigateCommand { get; set; }

	internal ILauncher? Launcher { get; set; }

	private async Task OnNavigateAsync(string? arg)
	{
		if (Launcher is not null && arg is not null && KeyToUrlMapping.TryGetValue(arg.ToLower(), out var uri))
		{
			await Launcher.LaunchUriAsync(new Uri(uri));
		}
	}
}