using Microsoft.AspNetCore.Mvc;
using ProcessMonitorApi.Models;
using ProcessMonitorApi.Operations.Interfaces;

namespace Host.ProcessMonitorApi.Controllers;

[ApiController]
public class AIController(IAnalyzeOperation analyzeOperation) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<AnalysisResponse?> AnalyzeAsync([FromBody] AnalysisRequest request)
    {
        return await analyzeOperation.ExecuteAsync(request);
    }

    [HttpGet("history")]
    public async Task<AnalysisResponse[]?> GetHistoryAsync()
    {
        return await analyzeOperation.GetHistoryAsync();
    }

    [HttpGet("summary")]
    public async Task<AnalysesSummaryResponse> GetSummaryAsync()
    {
        return await analyzeOperation.GetSummaryAsync();
    }
}
