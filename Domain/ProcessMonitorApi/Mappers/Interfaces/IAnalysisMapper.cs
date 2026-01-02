using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Models;

namespace ProcessMonitorApi.Mappers.Interfaces;

public interface IAnalysisMapper
{
    Analysis MapToAnalysis(AnalysisRequest request, ClassificationResponse? classificationResponse);
    AnalysisResponse MapToAnalysisResponse(AnalysisRequest request, ClassificationResponse? classificationResponse);
    AnalysisResponse MapToAnalysisResponse(Analysis analysisRecord);
}
