namespace ProcessMonitorApi.Contracts;

public class Analysis
{
    // EF Core uses this to recreate objects from the DB
    private Analysis() { }

    // Your app uses this to ensure valid state
    public Analysis(string action, string guideline, string result, decimal confidence)
    {
        Action = action;
        Guideline = guideline;
        Result = result;
        Confidence = confidence;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Action { get; set; } = null!;

    public string Guideline { get; set; } = null!;

    public string Result { get; set; } = null!;

    public decimal Confidence { get; set; }

    public DateTime CreatedAt { get; private set; }
}
