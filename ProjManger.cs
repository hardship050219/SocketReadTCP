#:package Spectre.Console@0.51.1

using Spectre.Console;

var selected = AnsiConsole.Prompt(
	new SelectionPrompt<string>()
		.Title("[bold cyan]Project Manager[/]")
		.PageSize(10)
		.MoreChoicesText("[grey](使用上下键选择，回车确认)[/]")
		.AddChoices(ActionOptions.Options));

AnsiConsole.MarkupLine($"[green]你选择了：[/] {selected}");

switch (selected)
{
	case ActionOptions.CleanBinDir: ProjectRmDir("bin"); break;
	case ActionOptions.CleanObjDir: ProjectRmDir("obj"); break;
}

static void ProjectRmDir(string dirName)
{
	var origin = Directory.GetCurrentDirectory();
	var projects = Directory.GetFiles(origin, "*.csproj", SearchOption.AllDirectories)
		.Select(Path.GetDirectoryName).Select(t => t?.Replace("\\", "/")).Distinct().ToList();

	AnsiConsole.MarkupLine("[yellow]📦 项目列表：[/]");
	for (var i = 0; i < projects.Count; i++)
	{
		AnsiConsole.MarkupLine($"{i + 1}. [blue]{projects[i]}[/]");
	}

	var input = AnsiConsole.Ask<string>("\n输入项目编号（如 1,3 或 * 表示全部）：");
	var indexes = input.Trim() == "*"
		? Enumerable.Range(0, projects.Count).ToList()
		: input.Split(',').Select(s => int.TryParse(s, out var n) ? n - 1 : -1)
			.Where(i => i >= 0 && i < projects.Count).ToList();

	if (!indexes.Any())
	{
		AnsiConsole.MarkupLine("[red]❌ 输入无效[/]");
		return;
	}

	AnsiConsole.MarkupLine("\n你选择了：");
	indexes.ForEach(i => AnsiConsole.MarkupLine($"{i + 1}. [blue]{projects[i]}[/]"));

	if (!AnsiConsole.Confirm($"\n确认清空 [bold]{dirName}[/] 文件夹？"))
	{
		AnsiConsole.MarkupLine("[red]❌ 操作取消[/]");
		return;
	}

	indexes.ForEach(i =>
	{
		var target = Path.Combine(projects[i]!, dirName);
		if (!Directory.Exists(target))
			AnsiConsole.MarkupLine($"[yellow]⚠️ 未找到：[/]{target}");
		else
		{
			Directory.Delete(target, true);
			AnsiConsole.MarkupLine($"[green]✅ 清空：[/]{target}");
		}
	});

	AnsiConsole.MarkupLine("[bold green]✅ 所有操作完成！[/]");
}

class ActionOptions
{
	public static List<string> Options = [CleanBinDir, CleanObjDir];
	public const string CleanBinDir = "1.清理项目 bin 文件夹";
	public const string CleanObjDir = "2.清理项目 obj 文件夹";
}