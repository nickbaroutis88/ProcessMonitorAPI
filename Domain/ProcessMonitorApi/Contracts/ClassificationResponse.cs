namespace ProcessMonitorApi.Contracts;

/// <summary>
/// Hugging Face classification response
/// </summary>
public class ClassificationResponse
{
    /// <summary>
    /// Labels
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Scores
    /// </summary>
    public float? Score { get; set; }
}
