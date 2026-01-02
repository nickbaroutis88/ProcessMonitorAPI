using System.Runtime.Serialization;

namespace ProcessMonitorApi.Models;

/// <summary>
/// Analysis response
/// </summary>
[DataContract]
[Serializable]
public class AnalysisResponse
{
    /// <summary>
    /// Analyzed action
    /// </summary>
    [DataMember]
    public string? Action { get; set; }

    /// <summary>
    /// Guidelines used
    /// </summary>
    [DataMember]
    public string? Guideline { get; set; }

    /// <summary>
    /// Result of the analysis
    /// </summary>
    [DataMember]
    public string? Result { get; set; }

    /// <summary>
    /// Confidence level of the analysis
    /// </summary>
    [DataMember]
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Timestamp of the analysis
    /// </summary>
    [DataMember]
    public DateTime TimeStamp { get; set; }
}
