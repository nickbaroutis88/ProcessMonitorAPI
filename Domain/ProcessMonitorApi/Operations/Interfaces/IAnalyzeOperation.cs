using ProcessMonitorApi.Models;

namespace ProcessMonitorApi.Operations.Interfaces;

public interface IAnalyzeOperation
{
    Task<AnalysisResponse?> ExecuteAsync(AnalysisRequest request);

    Task<AnalysisResponse[]?> GetHistoryAsync();

    Task<AnalysesSummaryResponse> GetSummaryAsync();
}
