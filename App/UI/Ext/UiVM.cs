using System.Diagnostics.CodeAnalysis;

namespace AppZC.UI;

public partial class UiVM : UiViewModel
{
	public AppVM AppVM => (GetAppVM() as AppVM)!;
}

public partial class UiVM<T> : UiViewModel<T> where T : UiView
{
	public required AppVM AppVM { get; init; }
}