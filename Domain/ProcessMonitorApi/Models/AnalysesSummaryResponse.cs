using System.Runtime.Serialization;

namespace ProcessMonitorApi.Models;

/// <summary>
/// Analyses summary response
/// </summary>
[DataContract]
[Serializable]
public class AnalysesSummaryResponse
{
    /// <summary>
    /// Analyses Count
    /// </summary>
    [DataMember]
    public int Count { get; set; }

    /// <summary>
    /// Analyses Count by Result
    /// </summary>
    public Dictionary<string, int>? ResultsCount { get; set; }
}
