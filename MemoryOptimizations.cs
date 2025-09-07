using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using HLTVDiscordBridge.Modules;

namespace HLTVDiscordBridge;

/// <summary>
/// Memory optimization utilities for HLTVDiscordBridge
/// </summary>
public static class MemoryOptimizations
{
    private static Timer _gcTimer;
    
    /// <summary>
    /// Initialize memory optimizations
    /// </summary>
    public static void Initialize()
    {
        // Configure GC for server scenarios
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        
        // Force periodic garbage collection every 30 minutes
        _gcTimer = new Timer(TimeSpan.FromMinutes(30).TotalMilliseconds);
        _gcTimer.Elapsed += (_, _) => ForceGarbageCollection();
        _gcTimer.Enabled = true;
        
        Logger.Log(new MyLogMessage(LogSeverity.Info, "MemoryOptimizations", "Memory optimizations initialized"));
    }
    
    /// <summary>
    /// Force garbage collection with full compaction
    /// </summary>
    public static void ForceGarbageCollection()
    {
        var beforeMemory = GC.GetTotalMemory(false);
        
        // Force full garbage collection
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        
        // Compact large object heap
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
        
        var afterMemory = GC.GetTotalMemory(false);
        var freedMemory = beforeMemory - afterMemory;
        
        Logger.Log(new MyLogMessage(LogSeverity.Verbose, "MemoryOptimizations", 
            $"GC completed. Freed {freedMemory / 1024 / 1024} MB memory"));
    }
    
    /// <summary>
    /// Get current memory usage information
    /// </summary>
    public static string GetMemoryInfo()
    {
        var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024;
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        
        return $"Memory: {totalMemory} MB | GC: Gen0={gen0}, Gen1={gen1}, Gen2={gen2}";
    }
    
    public static void Dispose()
    {
        _gcTimer?.Dispose();
    }
}
