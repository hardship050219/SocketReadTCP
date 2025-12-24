using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ZC.UI.RES.Locale;

public partial class en_us : ResourceDictionary
{
	public en_us()
	{
		AvaloniaXamlLoader.Load(this);
		this["STRING_PAGINATION_PAGE"] = string.Empty;
	}
}