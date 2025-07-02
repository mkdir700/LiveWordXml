using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;

namespace LiveWordXml.Wpf.Services
{
    /// <summary>
    /// Performance monitoring utility for document processing operations
    /// </summary>
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, PerformanceMetrics> _metrics = new();

        /// <summary>
        /// Start monitoring an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Stopwatch for the operation</returns>
        public static Stopwatch StartOperation(string operationName)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (!_metrics.ContainsKey(operationName))
            {
                _metrics[operationName] = new PerformanceMetrics();
            }

            // Record memory before operation
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            _metrics[operationName].StartMemory = GC.GetTotalMemory(false);
            
            return stopwatch;
        }

        /// <summary>
        /// End monitoring an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="stopwatch">Stopwatch that was started</param>
        public static void EndOperation(string operationName, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            
            if (_metrics.ContainsKey(operationName))
            {
                var metrics = _metrics[operationName];
                metrics.ExecutionCount++;
                metrics.TotalTime += stopwatch.ElapsedMilliseconds;
                metrics.LastExecutionTime = stopwatch.ElapsedMilliseconds;
                
                // Record memory after operation
                var endMemory = GC.GetTotalMemory(false);
                metrics.MemoryUsed = Math.Max(0, endMemory - metrics.StartMemory);
                metrics.PeakMemoryUsed = Math.Max(metrics.PeakMemoryUsed, metrics.MemoryUsed);
                
                // Update min/max times
                if (metrics.MinExecutionTime == 0 || stopwatch.ElapsedMilliseconds < metrics.MinExecutionTime)
                {
                    metrics.MinExecutionTime = stopwatch.ElapsedMilliseconds;
                }
                
                if (stopwatch.ElapsedMilliseconds > metrics.MaxExecutionTime)
                {
                    metrics.MaxExecutionTime = stopwatch.ElapsedMilliseconds;
                }
            }
        }

        /// <summary>
        /// Get performance metrics for an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Performance metrics or null if not found</returns>
        public static PerformanceMetrics? GetMetrics(string operationName)
        {
            return _metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Get all performance metrics
        /// </summary>
        /// <returns>Dictionary of all metrics</returns>
        public static Dictionary<string, PerformanceMetrics> GetAllMetrics()
        {
            return new Dictionary<string, PerformanceMetrics>(_metrics);
        }

        /// <summary>
        /// Clear all performance metrics
        /// </summary>
        public static void ClearMetrics()
        {
            _metrics.Clear();
        }

        /// <summary>
        /// Get a formatted performance report
        /// </summary>
        /// <returns>Formatted performance report</returns>
        public static string GetPerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Performance Report ===");
            report.AppendLine();

            foreach (var kvp in _metrics)
            {
                var name = kvp.Key;
                var metrics = kvp.Value;
                
                report.AppendLine($"Operation: {name}");
                report.AppendLine($"  Executions: {metrics.ExecutionCount}");
                report.AppendLine($"  Total Time: {metrics.TotalTime}ms");
                report.AppendLine($"  Average Time: {(metrics.ExecutionCount > 0 ? metrics.TotalTime / metrics.ExecutionCount : 0)}ms");
                report.AppendLine($"  Min Time: {metrics.MinExecutionTime}ms");
                report.AppendLine($"  Max Time: {metrics.MaxExecutionTime}ms");
                report.AppendLine($"  Last Time: {metrics.LastExecutionTime}ms");
                report.AppendLine($"  Memory Used: {FormatBytes(metrics.MemoryUsed)}");
                report.AppendLine($"  Peak Memory: {FormatBytes(metrics.PeakMemoryUsed)}");
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// Format bytes into human-readable format
        /// </summary>
        /// <param name="bytes">Number of bytes</param>
        /// <returns>Formatted string</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }

    /// <summary>
    /// Performance metrics for an operation
    /// </summary>
    public class PerformanceMetrics
    {
        public int ExecutionCount { get; set; }
        public long TotalTime { get; set; }
        public long MinExecutionTime { get; set; }
        public long MaxExecutionTime { get; set; }
        public long LastExecutionTime { get; set; }
        public long StartMemory { get; set; }
        public long MemoryUsed { get; set; }
        public long PeakMemoryUsed { get; set; }
    }
}
