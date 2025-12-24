using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using ZC.UI.RES.Locale;

namespace ZC.UI;

public class UiTheme : Styles
{
	public static readonly Dictionary<CultureInfo, ResourceDictionary> LocaleResources = new()
	{
		{ new CultureInfo("zh-cn"), new zh_cn() },
		{ new CultureInfo("en-us"), new en_us() },
		{ new CultureInfo("ja-jp"), new ja_jp() },
		{ new CultureInfo("uk-ua"), new uk_ua() },
		{ new CultureInfo("ru-ru"), new ru_ru() },
		{ new CultureInfo("zh-tw"), new zh_tw() },
		{ new CultureInfo("de-de"), new de_de() },
		{ new CultureInfo("es-es"), new es_es() },
		{ new CultureInfo("pl-pl"), new pl_pl() },
	};

	public UiTheme(IServiceProvider? provider = null)
	{
		AvaloniaXamlLoader.Load(provider, this);
		AvaloniaApplication.RemoveDictionaries(t => t.ToString()?.Contains("Themes.") ?? false);
	}

	public static ResourceDictionary DefaultLocaleResource { get; set; } = new zh_cn();

	public static ThemeVariant Aquatic => new(nameof(Aquatic), ThemeVariant.Dark);
	public static ThemeVariant Desert => new(nameof(Desert), ThemeVariant.Light);
	public static ThemeVariant Dusk => new(nameof(Dusk), ThemeVariant.Dark);
	public static ThemeVariant NightSky => new(nameof(NightSky), ThemeVariant.Dark);

	public CultureInfo? Locale
	{
		get;
		set
		{
			try
			{
				if (TryGetLocaleResource(value, out var resource) && resource is not null)
				{
					field = value;
					foreach (var kv in resource) Resources[kv.Key] = kv.Value;
				}
				else
				{
					field = new CultureInfo("zh-CN");
					foreach (var kv in DefaultLocaleResource) Resources[kv.Key] = kv.Value;
				}
			}
			catch
			{
				field = CultureInfo.InvariantCulture;
			}
		}
	}

	private static bool TryGetLocaleResource(CultureInfo? locale, out ResourceDictionary? resourceDictionary)
	{
		if (Equals(locale, CultureInfo.InvariantCulture))
		{
			resourceDictionary = DefaultLocaleResource;
			return true;
		}

		if (locale is null)
		{
			resourceDictionary = DefaultLocaleResource;
			return false;
		}

		if (LocaleResources.TryGetValue(locale, out var resource))
		{
			resourceDictionary = resource;
			return true;
		}

		resourceDictionary = DefaultLocaleResource;
		return false;
	}

	public static void OverrideLocaleResources(Application application, CultureInfo? culture)
	{
		if (culture is null) return;
		if (!LocaleResources.TryGetValue(culture, out var resources)) return;
		foreach (var kv in resources)
		{
			application.Resources[kv.Key] = kv.Value;
		}
	}

	public static void OverrideLocaleResources(StyledElement element, CultureInfo? culture)
	{
		if (culture is null) return;
		if (!LocaleResources.TryGetValue(culture, out var resources)) return;
		foreach (var kv in resources)
		{
			element.Resources[kv.Key] = kv.Value;
		}
	}
}

public static class ApplicationExtension
{
	private static Application _app = null!;

	private static readonly Dictionary<Color, ThemeVariant> ColorThemeMap = new()
	{
		[Color.Parse("#8EE3F0")] = UiTheme.Aquatic,
		[Color.Parse("#903909")] = UiTheme.Desert,
		[Color.Parse("#A1BFDE")] = UiTheme.Dusk,
		[Color.Parse("#D6B4FD")] = UiTheme.NightSky
	};

	public static void RegisterFollowSystemTheme(this Application app)
	{
		_app = app;
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
		if (app.PlatformSettings is null) return;
		app.PlatformSettings.ColorValuesChanged -= OnColorValuesChanged;
		app.PlatformSettings.ColorValuesChanged += OnColorValuesChanged;
		OnColorValuesChanged(null, app.PlatformSettings?.GetColorValues());
	}

	public static void UnregisterFollowSystemTheme(this Application app)
	{
		_app = app;
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
		if (app.PlatformSettings is null) return;
		app.PlatformSettings.ColorValuesChanged -= OnColorValuesChanged;
	}

	private static void OnColorValuesChanged(object? _, PlatformColorValues? args)
	{
		ThemeVariant result;
		if (args?.ContrastPreference is ColorContrastPreference.High)
		{
			result = ColorThemeMap.TryGetValue(args.AccentColor1, out var theme) ? theme : ThemeVariant.Default;
		}
		else
		{
			result = args?.ThemeVariant switch
			{
				PlatformThemeVariant.Light => ThemeVariant.Light,
				PlatformThemeVariant.Dark => ThemeVariant.Dark,
				_ => ThemeVariant.Default
			};
		}

		_app.RequestedThemeVariant = result;
	}
}