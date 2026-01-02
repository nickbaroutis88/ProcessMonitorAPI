using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Mappers.Interfaces;
using ProcessMonitorApi.Models;
using ProcessMonitorApi.Operations.Interfaces;
using ProcessMonitorApi.Repository;
using ProcessMonitorApi.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ProcessMonitorApi.Operations.Implementations;

public class AnalyzeOperation(
    IHuggingFaceClassificationService huggingFaceClassificationService,
    ISQLiteRepository sQLiteRepository,
    IAnalysisMapper analysisMapper,
    ILogger<AnalyzeOperation> logger
    ) : IAnalyzeOperation
{

    public async Task<AnalysisResponse?> ExecuteAsync(AnalysisRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.Guideline))
        {
            throw new ArgumentException("Invalid request");
        }

        // Check if it has been already analyzed
        var existingAnalysis = await sQLiteRepository.GetEntityAsync<Analysis>(p => 
            p.Action == request.Action 
            && p.Guideline == request.Guideline);

        if (existingAnalysis != null) 
        {
            return analysisMapper.MapToAnalysisResponse(existingAnalysis);
        }

        var classificationResponse = await huggingFaceClassificationService.ClassifyAsync(request);

        try 
        {
            // Save to database
            var analysisRecord = analysisMapper.MapToAnalysis(request, classificationResponse);

            await sQLiteRepository.AddAsync(analysisRecord);
        }
        catch (ArgumentNullException ex)
        {
            logger.LogError(ex, "Error during classification");
            // But still return the response
        }

        return analysisMapper.MapToAnalysisResponse(request, classificationResponse);
    }

    public async Task<AnalysisResponse[]?> GetHistoryAsync()
    {
        var analyses = await sQLiteRepository.GetAllAsync<Analysis>();
        if (analyses == null || !analyses.Any())
        {
            return null;
        }

        return [.. analyses
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => analysisMapper.MapToAnalysisResponse(a))];
    }

    public async Task<AnalysesSummaryResponse> GetSummaryAsync()
    {
        var analyses = await sQLiteRepository.GetAllAsync<Analysis>();

        if (analyses == null || !analyses.Any())
        {
            return new AnalysesSummaryResponse
            {
                Count = 0
            };
        }

        var resultsCount = analyses
            .GroupBy(a => a.Result)
            .ToDictionary(g => g.Key, g => g.Count());

        
        return new AnalysesSummaryResponse
        {
            Count = analyses.Count(),
            ResultsCount = resultsCount
        };
    }
}
