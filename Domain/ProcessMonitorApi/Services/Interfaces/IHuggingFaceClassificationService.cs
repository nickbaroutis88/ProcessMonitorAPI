using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Models;

namespace ProcessMonitorApi.Services.Interfaces;

public interface IHuggingFaceClassificationService
{
    Task<ClassificationResponse?> ClassifyAsync(AnalysisRequest analyzeRequest);
}
