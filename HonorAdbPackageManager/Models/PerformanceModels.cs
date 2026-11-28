namespace HonorAdbPackageManager.Models;

/// <summary>游戏性能单秒采样</summary>
public class PerformanceSample
{
    /// <summary>采样时刻</summary>
    public DateTime Time { get; init; }
    /// <summary>相对统计开始的秒数</summary>
    public int Second { get; init; }
    /// <summary>该秒帧率（SurfaceFlinger 图层帧时间戳统计）</summary>
    public int Fps { get; init; }
    /// <summary>电池温度 °C</summary>
    public double BatteryTemp { get; init; }
    /// <summary>CPU 峰值核心温度 °C</summary>
    public double CpuTemp { get; init; }
    /// <summary>游戏进程 CPU 占用 %</summary>
    public double CpuPercent { get; init; }
    /// <summary>该秒内帧间隔超阈值（卡顿）占比 %</summary>
    public double JankPercent { get; init; }
    /// <summary>电量 %</summary>
    public int BatteryLevel { get; init; }
}

/// <summary>设备状态快照（状态面板）</summary>
public class StatusSnapshot
{
    public int BatteryLevel { get; init; }
    public string BatteryStatusText { get; init; } = "—";
    public double BatteryTemp { get; init; }
    public string SignalText { get; init; } = "—";
    public string NetworkText { get; init; } = "—";
    public bool TrafficOk { get; init; }
    public long MobileRx { get; init; }
    public long MobileTx { get; init; }
    public long WifiRx { get; init; }
    public long WifiTx { get; init; }
}
