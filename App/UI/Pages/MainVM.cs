using System.Linq;
using System.Net.Sockets;
using System.Text;
using Avalonia.Media;
using Avalonia.Threading;
using ZC.Mvvm;

namespace AppZC.UI.Pages;

[AddToIOC(Lifetime = LifetimeType.Singleton)]
[ObservableObject(IncludeAllPartialProperty = true)]
public partial class MainVM : UiVM<MainPage>
{
	private TcpClient? _tcpClient;
	private NetworkStream? _networkStream;
	private CancellationTokenSource? _receiveCancellationTokenSource;
	private Task? _receiveTask;
	private int _receivedCount;

	public partial string ServerIp { get; set; } = "127.0.0.1";
	public partial int ServerPort { get; set; } = 502;
	public partial string ReadAddress { get; set; } = "100";
	public partial string DataType { get; set; } = "int";
	public partial bool IsConnected { get; set; }
	public List<string> DataTypes { get; } = new() { "bool", "int", "short", "double", "float", "string" };
	public partial string SendDataText { get; set; } = "";
	public partial string SendDataDisplay { get; set; } = "";
	public partial string ReceivedData { get; set; } = "";
	public partial bool SendAsHex { get; set; }
	public partial bool DisplayAsHex { get; set; }
	
	private string _lastReadAddress = "";
	private string _lastDataType = "";

	public partial string ConnectionStatusText { get; set; } = "未连接";
	public partial IBrush ConnectionStatusColor { get; set; } = Brushes.Gray;
	public partial string ConnectionButtonText { get; set; } = "连接";
	public string ReceivedCountText => $"已接收: {_receivedCount} 条";

	partial void OnIsConnectedChanged(bool value)
	{
		ConnectionStatusText = value ? "已连接" : "未连接";
		ConnectionStatusColor = value ? Brushes.Green : Brushes.Gray;
		ConnectionButtonText = value ? "断开连接" : "连接";
	}

	async Task @ToggleConnection()
	{
		if (IsConnected)
		{
			await DisconnectAsync();
		}
		else
		{
			await ConnectAsync();
		}
	}

	private async Task ConnectAsync()
	{
		try
		{
			//判断是否为空地址
			if (string.IsNullOrWhiteSpace(ServerIp))
			{
				ShowToast("请输入服务器地址");
				return;
			}
			
			//判断端口号范围
			if (ServerPort < 1 || ServerPort > 65535)
			{
				ShowToast("端口号必须在1-65535之间");
				return;
			}

			//创建TCP连接实例
			_tcpClient = new TcpClient();
			//连接TCP服务器
			await _tcpClient.ConnectAsync(ServerIp, ServerPort);
			//获取网络流，方便后续数据读写
			_networkStream = _tcpClient.GetStream();
			IsConnected = true;

			//初始化取消令牌源，防止线程堵塞
			_receiveCancellationTokenSource = new CancellationTokenSource();
			//启动线程接收数据
			_receiveTask = Task.Run(() => ReceiveDataAsync(_receiveCancellationTokenSource.Token));

			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 已连接到 {ServerIp}:{ServerPort}\r\n");
			ShowToast("连接成功");
		}
		catch (Exception ex)
		{
			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 连接失败: {ex.Message}\r\n");
			ShowToast($"连接失败: {ex.Message}");
			await DisconnectAsync();
		}
	}

	private async Task DisconnectAsync()
	{
		try
		{
			// 先关闭网络流，这会中断ReadAsync操作
			try
			{
				_networkStream?.Close();
			}
			catch { }

			// 取消接收任务
			_receiveCancellationTokenSource?.Cancel();

			// 等待接收任务结束（最多等待2秒）
			if (_receiveTask != null)
			{
				try
				{
					await Task.WhenAny(_receiveTask, Task.Delay(2000));
				}
				catch { }
			}

			// 关闭TCP客户端
			try
			{
				_tcpClient?.Close();
			}
			catch { }

			// 释放资源
			//释放网络流
			_networkStream?.Dispose();
			//释放TCP客户端
			_tcpClient?.Dispose();
			//释放取消令牌源
			_receiveCancellationTokenSource?.Dispose();

			//清空赋值
			_networkStream = null;
			_tcpClient = null;
			_receiveTask = null;
			_receiveCancellationTokenSource = null;
			IsConnected = false;

			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 已断开连接\r\n");
			ShowToast("已断开连接");
		}
		catch (Exception ex)
		{
			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 断开连接时出错: {ex.Message}\r\n");
		}
	}

	private async Task ReceiveDataAsync(CancellationToken cancellationToken)
	{
		var buffer = new byte[4096];
		while (!cancellationToken.IsCancellationRequested && _networkStream != null)
		{
			try
			{
				//利用网络流读取byte值
				var bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
				if (bytesRead == 0)
				{
					// 连接已关闭
					await DisconnectAsync();
					break;
				}

				var data = new byte[bytesRead];
				Array.Copy(buffer, data, bytesRead);
				_receivedCount++;

				// 在UI线程上更新接收数据（如果UI还存在）
				try
				{
					await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
					{
						// 尝试解析Modbus TCP响应
						var parsedResult = ParseModbusResponse(data, _lastDataType);
						if (!string.IsNullOrEmpty(parsedResult))
						{
							AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] {parsedResult}\r\n");
						}
						else
						{
							// 如果无法解析，显示原始数据
							AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 接收 ({bytesRead} 字节): {BitConverter.ToString(data).Replace("-", " ")}\r\n");
						}
					});
				}
				catch
				{
					// UI线程可能已经关闭，忽略异常
					break;
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				// 流已被释放，退出循环
				break;
			}
			catch (Exception ex)
			{
				// 尝试更新UI，但如果UI已关闭则忽略
				try
				{
					await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
					{
						AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 接收数据出错: {ex.Message}\r\n");
					});
				}
				catch { }
				
				await DisconnectAsync();
				break;
			}
		}
	}
	
	void @ClearLog()
	{
		ReceivedData = "";
		SendDataDisplay = "";
		_receivedCount = 0;
	}

	void @ClearReceived()
	{
		ReceivedData = "";
		_receivedCount = 0;
	}

	void @ClearSendDisplay()
	{
		SendDataDisplay = "";
	}

	private void AppendReceivedData(string text)
	{
		ReceivedData += text;
		// 限制接收数据长度，避免内存溢出
		if (ReceivedData.Length > 100000)
		{
			var lines = ReceivedData.Split('\n');
			if (lines.Length > 1000)
			{
				ReceivedData = string.Join("\n", lines.Skip(lines.Length - 1000));
			}
		}
		
		// 自动滚动到底部
		if (View is MainPage mainPage)
		{
			var scrollViewer = mainPage.GetReceivedDataScrollViewer();
			ScrollToBottom(scrollViewer);
		}
	}

	private void AppendSendData(string text)
	{
		SendDataDisplay += text;
		// 限制发送数据长度，避免内存溢出
		if (SendDataDisplay.Length > 100000)
		{
			var lines = SendDataDisplay.Split('\n');
			if (lines.Length > 1000)
			{
				SendDataDisplay = string.Join("\n", lines.Skip(lines.Length - 1000));
			}
		}
		
		// 自动滚动到底部
		if (View is MainPage mainPage)
		{
			var scrollViewer = mainPage.GetSendDataScrollViewer();
			ScrollToBottom(scrollViewer);
		}
	}

	private void ScrollToBottom(ScrollViewer? scrollViewer)
	{
		if (scrollViewer != null)
		{
			// 在UI线程上执行滚动
			Dispatcher.UIThread.Post(() =>
			{
				try
				{
					scrollViewer.ScrollToEnd();
				}
				catch
				{
					// 如果ScrollToEnd不可用，尝试使用Offset
					try
					{
						scrollViewer.Offset = new Avalonia.Vector(0, scrollViewer.Extent.Height);
					}
					catch
					{
						// 忽略滚动错误
					}
				}
			}, DispatcherPriority.Background);
		}
	}

	async Task @ReadData()
	{
		if (!IsConnected || _networkStream == null)
		{
			ShowToast("请先连接服务器");
			return;
		}

		if (string.IsNullOrWhiteSpace(ReadAddress))
		{
			ShowToast("请输入读取地址");
			return;
		}

		if (!ushort.TryParse(ReadAddress, out var address))
		{
			ShowToast("读取地址必须是有效的数字");
			return;
		}

		try
		{
			// 保存当前读取参数，用于解析响应
			_lastReadAddress = ReadAddress;
			_lastDataType = DataType;
			
			// 构造Modbus TCP协议数据包
			var modbusPacket = BuildModbusReadPacket(address, DataType);
			
			await _networkStream.WriteAsync(modbusPacket, 0, modbusPacket.Length);
			await _networkStream.FlushAsync();

			// 更新发送数据显示
			var sendDisplay = $"[{DateTime.Now:HH:mm:ss}] 发送读取命令 (地址:{address}, 类型:{DataType}):\r\n";
			sendDisplay += BitConverter.ToString(modbusPacket).Replace("-", " ") + "\r\n";
			sendDisplay += "\r\n";
			AppendSendData(sendDisplay);

			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 发送读取命令 (地址:{address}, 类型:{DataType})\r\n");
			ShowToast("读取命令已发送");
		}
		catch (Exception ex)
		{
			AppendReceivedData($"[{DateTime.Now:HH:mm:ss}] 发送读取命令出错: {ex.Message}\r\n");
			ShowToast($"发送失败: {ex.Message}");
		}
	}

	private static ushort _transactionId = 0;

	// 将ushort转换为大端序字节数组
	private static byte[] UInt16ToBigEndian(ushort value)
	{
		return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
	}

	private byte[] BuildModbusReadPacket(ushort address, string dataType)
	{
		// Modbus TCP协议头 (大端序)
		// Transaction Identifier (2 bytes) - 递增
		_transactionId++;
		if (_transactionId == 0) _transactionId = 1;

		// Protocol Identifier (2 bytes) - 固定为0x0000
		// Length (2 bytes) - 剩余字节数
		// Unit Identifier (1 byte) - 通常为0x01
		// Function Code (1 byte)
		// Starting Address (2 bytes)
		// Quantity of Registers (2 bytes)

		byte functionCode;
		ushort quantity;

		// 根据数据类型选择功能码和寄存器数量
		switch (dataType.ToLower())
		{
			case "bool":
				functionCode = 0x01; // Read Coils
				quantity = 1;
				break;
			case "short":
				functionCode = 0x03; // Read Holding Registers
				quantity = 1; // 1个寄存器 = 2字节 = short
				break;
			case "int":
				functionCode = 0x03; // Read Holding Registers
				quantity = 2; // 2个寄存器 = 4字节 = int
				break;
			case "float":
				functionCode = 0x03; // Read Holding Registers
				quantity = 2; // 2个寄存器 = 4字节 = float
				break;
			case "double":
				functionCode = 0x03; // Read Holding Registers
				quantity = 4; // 4个寄存器 = 8字节 = double
				break;
			case "string":
				functionCode = 0x03; // Read Holding Registers
				quantity = 16; // 假设字符串最多32字节 = 16个寄存器
				break;
			default:
				throw new ArgumentException($"不支持的数据类型: {dataType}");
		}

		// 构造数据包
		var packet = new byte[12]; // Modbus TCP头(6) + MBAP(1) + Function Code(1) + Address(2) + Quantity(2)
		var offset = 0;
		
		// Transaction Identifier (大端序)
		var transIdBytes = UInt16ToBigEndian(_transactionId);
		packet[offset++] = transIdBytes[0];
		packet[offset++] = transIdBytes[1];
		
		// Protocol Identifier (0x0000) - 大端序
		packet[offset++] = 0x00;
		packet[offset++] = 0x00;
		
		// Length (剩余字节数: 1 Unit ID + 1 Function Code + 2 Address + 2 Quantity = 6) - 大端序
		var lengthBytes = UInt16ToBigEndian(6);
		packet[offset++] = lengthBytes[0];
		packet[offset++] = lengthBytes[1];
		
		// Unit Identifier
		packet[offset++] = 0x01;
		
		// Function Code
		packet[offset++] = functionCode;
		
		// Starting Address (大端序)
		var addressBytes = UInt16ToBigEndian(address);
		packet[offset++] = addressBytes[0];
		packet[offset++] = addressBytes[1];
		
		// Quantity of Registers (大端序)
		var quantityBytes = UInt16ToBigEndian(quantity);
		packet[offset++] = quantityBytes[0];
		packet[offset++] = quantityBytes[1];

		return packet;
	}

	// 从大端序字节数组读取ushort
	private static ushort BigEndianToUInt16(byte[] bytes, int offset)
	{
		return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
	}

	// 从大端序字节数组读取uint
	private static uint BigEndianToUInt32(byte[] bytes, int offset)
	{
		return (uint)((bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3]);
	}

	private string ParseModbusResponse(byte[] data, string dataType)
	{
		try
		{
			// Modbus TCP响应格式：
			// Transaction ID (2 bytes)
			// Protocol ID (2 bytes) - 0x0000
			// Length (2 bytes)
			// Unit ID (1 byte)
			// Function Code (1 byte)
			// Byte Count (1 byte) - 数据字节数
			// Data (N bytes)

			if (data.Length < 9)
			{
				return ""; // 数据太短，不是有效的Modbus响应
			}

			var transactionId = BigEndianToUInt16(data, 0);
			var protocolId = BigEndianToUInt16(data, 2);
			var length = BigEndianToUInt16(data, 4);
			var unitId = data[6];
			var functionCode = data[7];
			var byteCount = data[8];

			// 检查是否是Modbus TCP响应
			if (protocolId != 0x0000 || data.Length < 9 + byteCount)
			{
				return "";
			}

			// 检查功能码错误
			if ((functionCode & 0x80) != 0)
			{
				var errorCode = data[8];
				return $"Modbus错误: 功能码 0x{functionCode:X2}, 错误代码 0x{errorCode:X2}";
			}

			// 解析数据部分
			if (byteCount == 0)
			{
				return "无数据";
			}

			var dataOffset = 9;
			if (data.Length < dataOffset + byteCount)
			{
				return "";
			}

			switch (dataType.ToLower())
			{
				case "bool":
					if (byteCount >= 1)
					{
						var boolValue = data[dataOffset] != 0;
						return $"[bool] 值: {boolValue}";
					}
					break;

				case "short":
					if (byteCount >= 2)
					{
						var reg0 = BigEndianToUInt16(data, dataOffset);
						var shortValue = (short)reg0;
						return $"[short] 值: {shortValue}";
					}
					break;

				case "int":
					if (byteCount >= 4)
					{
						// Modbus寄存器是大端序
						// 寄存器0: data[9-10], 寄存器1: data[11-12]
						var reg0 = BigEndianToUInt16(data, dataOffset);
						var reg1 = BigEndianToUInt16(data, dataOffset + 2);
						
						/*// 尝试多种解析方式
						// 方式1: 大端序（高字节在前）
						var intValueBigEndian = (int)BigEndianToUInt32(data, dataOffset);*/
						
						// 方式2: 小端序（低字节在前）- 交换寄存器顺序
						var intValueLittleEndian = (int)((reg1 << 16) | reg0);
						
						/*// 方式3: 只取第一个寄存器（如果第二个为0）
						if (reg1 == 0)
						{
							return $"[int] 值: {reg0}";
						}*/
						
						// 如果第二个寄存器不为0，优先显示小端序（更常见）
						return $"[int] 值: {intValueLittleEndian}";
					}
					break;

				case "float":
					if (byteCount >= 4)
					{
						// 尝试大端序
						var bytesBigEndian = new byte[4];
						Array.Copy(data, dataOffset, bytesBigEndian, 0, 4);
						if (BitConverter.IsLittleEndian)
						{
							Array.Reverse(bytesBigEndian);
						}
						var floatValueBigEndian = BitConverter.ToSingle(bytesBigEndian, 0);
						
						// 尝试小端序（交换寄存器）
						var bytesLittleEndian = new byte[4];
						bytesLittleEndian[0] = data[dataOffset + 2];
						bytesLittleEndian[1] = data[dataOffset + 3];
						bytesLittleEndian[2] = data[dataOffset];
						bytesLittleEndian[3] = data[dataOffset + 1];
						if (BitConverter.IsLittleEndian)
						{
							Array.Reverse(bytesLittleEndian);
						}
						var floatValueLittleEndian = BitConverter.ToSingle(bytesLittleEndian, 0);
						
						// 优先显示小端序（更常见）
						return $"[float] 值: {floatValueLittleEndian}";
					}
					break;

				case "double":
					if (byteCount >= 8)
					{
						// 小端序（交换寄存器顺序：3,2,1,0）
						// 交换寄存器：寄存器0<->寄存器3, 寄存器1<->寄存器2
						var bytesLittleEndian = new byte[8];
						// 寄存器3 -> 位置0
						bytesLittleEndian[0] = data[dataOffset + 6];
						bytesLittleEndian[1] = data[dataOffset + 7];
						// 寄存器2 -> 位置1
						bytesLittleEndian[2] = data[dataOffset + 4];
						bytesLittleEndian[3] = data[dataOffset + 5];
						// 寄存器1 -> 位置2
						bytesLittleEndian[4] = data[dataOffset + 2];
						bytesLittleEndian[5] = data[dataOffset + 3];
						// 寄存器0 -> 位置3
						bytesLittleEndian[6] = data[dataOffset];
						bytesLittleEndian[7] = data[dataOffset + 1];
						if (BitConverter.IsLittleEndian)
						{
							Array.Reverse(bytesLittleEndian);
						}
						var doubleValueLittleEndian = BitConverter.ToDouble(bytesLittleEndian, 0);
						
						return $"[double] 值: {doubleValueLittleEndian}";
					}
					break;

				case "string":
					if (byteCount > 0)
					{
						// 直接使用原始字节进行UTF-8解码
						var bytes = new byte[byteCount];
						Array.Copy(data, dataOffset, bytes, 0, byteCount);
						
						// 去除末尾的空字节
						int len = bytes.Length;
						while (len > 0 && bytes[len - 1] == 0)
						{
							len--;
						}
						
						// 使用UTF-8解码
						try
						{
							var stringValue = Encoding.UTF8.GetString(bytes, 0, len);
							return $"[string] 值: {stringValue}";
						}
						catch
						{
							// 如果解码失败，显示原始十六进制
							var hexData = BitConverter.ToString(bytes, 0, Math.Min(len, 32)).Replace("-", " ");
							return $"[string] 原始数据: {hexData}";
						}
					}
					break;
			}

			// 如果无法解析，显示原始十六进制
			var hexString = BitConverter.ToString(data, dataOffset, byteCount).Replace("-", " ");
			return $"原始数据: {hexString}";
		}
		catch (Exception ex)
		{
			return $"解析错误: {ex.Message}";
		}
	}

	protected override void OnViewDetach()
	{
		// 同步清理所有资源
		CleanupResources();
		base.OnViewDetach();
	}

	private void CleanupResources()
	{
		try
		{
			// 先关闭网络流，这会中断ReadAsync操作
			try
			{
				_networkStream?.Close();
			}
			catch { }

			// 取消接收任务
			_receiveCancellationTokenSource?.Cancel();

			// 等待接收任务结束（最多等待2秒）
			if (_receiveTask != null)
			{
				try
				{
					_receiveTask.Wait(2000);
				}
				catch { }
			}

			// 关闭TCP客户端
			try
			{
				_tcpClient?.Close();
			}
			catch { }

			// 释放资源
			_networkStream?.Dispose();
			_tcpClient?.Dispose();
			_receiveCancellationTokenSource?.Dispose();

			_networkStream = null;
			_tcpClient = null;
			_receiveTask = null;
			_receiveCancellationTokenSource = null;
			IsConnected = false;
		}
		catch
		{
			// 忽略清理时的异常
		}
	}
}