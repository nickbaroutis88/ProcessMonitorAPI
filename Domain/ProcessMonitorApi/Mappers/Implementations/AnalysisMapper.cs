using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Mappers.Interfaces;
using ProcessMonitorApi.Models;

namespace ProcessMonitorApi.Mappers.Implementations;

public class AnalysisMapper : IAnalysisMapper
{
    public Analysis MapToAnalysis(AnalysisRequest request, ClassificationResponse? response)
    {
        if (response is null || response.Label is null || response.Score is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        return new Analysis
        (
            request.Action!,
            request.Guideline!,
            response.Label,
            Math.Round((decimal)response.Score.Value, 2)
        );
    }

    public AnalysisResponse MapToAnalysisResponse(AnalysisRequest request, ClassificationResponse? classificationResponse)
    {
        if (classificationResponse is null || classificationResponse.Label is null || classificationResponse.Score is null)
        {
            throw new ArgumentNullException(nameof(classificationResponse));
        }

        return new AnalysisResponse
        {
            Action = request.Action,
            Guideline = request.Guideline,
            Result = classificationResponse?.Label,
            Confidence = classificationResponse?.Score.HasValue == true 
                        ? Math.Round((decimal)classificationResponse.Score.Value, 2) 
                        : null,
            TimeStamp = DateTime.UtcNow
        };
    }

    public AnalysisResponse MapToAnalysisResponse(Analysis analysisRecord)
    {
        return new AnalysisResponse
        {
            Action = analysisRecord.Action,
            Guideline = analysisRecord.Guideline,
            Result = analysisRecord.Result,
            Confidence = analysisRecord.Confidence,
            TimeStamp = analysisRecord.CreatedAt.ToUniversalTime(),
        };
    }
}
