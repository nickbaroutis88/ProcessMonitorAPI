using Microsoft.Extensions.Configuration;
using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Models;
using ProcessMonitorApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace ProcessMonitorApi.Services.Implementations;

public class HuggingFaceClassificationService : IHuggingFaceClassificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    private static readonly JsonSerializerOptions CamelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public HuggingFaceClassificationService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _modelId = config["HuggingFaceSettings:Model"] ?? "facebook/bart-large-mnli";
    }

    public async Task<ClassificationResponse?> ClassifyAsync(AnalysisRequest analyzeRequest)
    {
        var labels = new string[] { "COMPLIES", "DEVIATES", "UNCLEAR" };

        // Structure the payload
        var payload = new
        {
            inputs = string.Join(Environment.NewLine,
                "Classify the action below based on the following guideline.", 
                "If the action contradicts the guideline, classify it as DEVIATES.",
                "If the relationship between the two is unclear or ambiguous, classify it as UNCLEAR.",
                "Otherwise, classify it as COMPLIES.",
                "", 
                $"Guideline: {analyzeRequest.Guideline}",
                "",
                $"Action: {analyzeRequest.Action}"
            ),
            parameters = new
            {
                candidate_labels = labels,
                hypothesis_template = "This action has been classified as {} with respect to the guideline.",
                multi_label = false
            }
        };   

        var jsonPayload = JsonSerializer.Serialize(payload, CamelCaseOptions);

        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"hf-inference/models/{_modelId}", content);

        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        var classificationResponses = JsonSerializer.Deserialize<ClassificationResponse[]?>(resultJson, CaseInsensitiveOptions);

        return classificationResponses?.FirstOrDefault();
    }
}
