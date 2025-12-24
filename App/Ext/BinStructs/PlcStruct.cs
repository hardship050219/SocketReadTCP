using ZC.BinaryStruct;
using ZC.MemoryToolkit;
using ZC.Mvvm;

namespace AppZC.Models.BinStructs;

[BinaryStruct(Length = 10, ByteFormat = ByteFormat.ABCD)]
[BinaryPointGroup(Name = "BasicInfo", Start = 50, End = 90)]
public partial class TestMc
{
	[BinaryPoint(Offset = 0, RawType = typeof(short), ByteLength = 2)]
	public int Id { get; set; }

	[BinaryPoint(Offset = 2, ByteLength = 4)]
	public float Temperature { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
public class MemoryClientWriteAttribute : Attribute
{
	public static string DefaultNameTemplate { get; set; } = "Write{}ToPlc";
	public string? Name { get; set; }
	public string? NameTemplate { get; set; }
	public long AddressByInt { get; set; }
	public object? Address { get; set; }
	public object[]? NameAndAddresses { get; set; }
	public int Length { get; set; }
	public int LengthUnit { get; set; }
	public int Offset { get; set; }
	public bool UseAsync { get; set; } = true;
	public bool UseSync { get; set; } = true;
	public string[]? Members { get; set; }
	public object[]? AddressAndMembers { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class ChainedSetProperty : Attribute
{
	public string[]? Ignore { get; set; }
	public string[]? Select { get; set; }
	public bool SelectAll { get; set; }
}

[BinaryStruct(Length = 201)]
[ChainedSetProperty(SelectAll = true)]
[BinaryPointGroup(Name = "All", Start = 0, End = 90)]
public partial class PlcStruct : ObservableObject
{
	public IMemoryClient? MemoryClient { get; set; }

	[BinaryPoint(Offset = 0, ByteLength = 2, RawTo = "value * 10", ToRaw = "value / 10")]
	[MemoryClientWrite(Name = "OP010WriteIdToPlc", Address = "DB2.200")]
	[MemoryClientWrite(Name = "OPWriteIdToPlc", Address = "{OP}.200")]
	[MemoryClientWrite(NameAndAddresses =
	[
		"SetOP010Id", "DB1.200",
		"SetOP020Id", "DB2.200",
	])]
	public int Id { get; set; }

	[BinaryPoint(Offset = 15)] public BitBool IsOpen { get; set; } = new(1);

	[BinaryPoint(Offset = 0, RawTo = "value * 10", ToRaw = "value / 10")]
	public int Id2 { get; set; }


	// [MemoryClientWrite(Address = "DB2.200", Members = [nameof(Id)])]
	// public partial Result WriteId(IMemoryClient? plc = null);
	//
	// [MemoryClientWrite(AddressAndMembers = ["DB1.200", nameof(Id), "DB1.204", nameof(Id2)])]
	// public partial Result WriteIds(IMemoryClient? plc = null);

	// #region MemoryClientWrite 源码生成
	//
	// public Result OP010WriteIdToPlc(IMemoryClient? plc = null)
	// {
	// 	plc ??= MemoryClient;
	// 	if (plc is null) return Result.Err("XXX");
	// 	return plc.Write("DB1.200", Id);
	// }
	//
	// public Result SetOP010Id(IMemoryClient? plc = null)
	// {
	// 	plc ??= MemoryClient;
	// 	if (plc is null) return Result.Err("XXX");
	// 	return plc.Write("DB1.200", Id);
	// }
	//
	// public Result SetOP020Id(IMemoryClient? plc = null)
	// {
	// 	plc ??= MemoryClient;
	// 	if (plc is null) return Result.Err("XXX");
	// 	return plc.Write("DB2.200", Id);
	// }
	//
	// public partial Result WriteId(IMemoryClient? plc)
	// {
	// 	plc ??= MemoryClient;
	// 	if (plc is null) return Result.Err("XXX");
	// 	return plc.Write("DB1.200", Id);
	// }
	//
	// public partial Result WriteIds(IMemoryClient? plc)
	// {
	// 	plc ??= MemoryClient;
	// 	if (plc is null) return Result.Err("XXX");
	// 	Result ret;
	// 	ret = plc.Write("DB1.200", Id);
	// 	if (ret.IsError())
	// 		return ret;
	// 	ret = plc.Write("DB1.204", Id2);
	// 	return ret;
	// }
	//
	// #endregion


	// static void Test()
	// {
	// 	var data = new PlcStruct();
	// 	data.MemoryClient = null!; // new xxPlcClient();
	// 	data.SetId(100).OP010WriteIdToPlc().Unwarp();
	// }
}

[BinaryStruct(Length = 10, LengthUnit = 1)]
public partial class MyData
{
	[BinaryPoint(Offset = 0)] public short SerialNo { get; set; }
	[BinaryPoint(Offset = 2)] public double Temperature { get; set; }
}