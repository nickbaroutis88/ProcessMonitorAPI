using System.Runtime.Serialization;

namespace ProcessMonitorApi.Models;

/// <summary>
/// Request to analyze
/// </summary>
[DataContract]
[Serializable]
public class AnalysisRequest
{
    /// <summary>
    /// Action to analyze
    /// </summary>
    [DataMember]
    public string? Action { get; set; }

    /// <summary>
    /// Guidelines to use
    /// </summary>
    [DataMember]
    public string? Guideline { get; set; }
}
