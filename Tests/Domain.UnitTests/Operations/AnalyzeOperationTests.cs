using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Mappers.Interfaces;
using ProcessMonitorApi.Models;
using ProcessMonitorApi.Operations.Implementations;
using ProcessMonitorApi.Repository;
using ProcessMonitorApi.Services.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace Domain.UnitTests.Operations;

public class AnalyzeOperationTests
{
    private readonly Mock<IHuggingFaceClassificationService> _mockClassificationService;
    private readonly Mock<ISQLiteRepository> _mockRepository;
    private readonly Mock<IAnalysisMapper> _mockMapper;
    private readonly Mock<ILogger<AnalyzeOperation>> _mockLogger;
    private readonly AnalyzeOperation _operation;

    public AnalyzeOperationTests()
    {
        _mockClassificationService = new Mock<IHuggingFaceClassificationService>();
        _mockRepository = new Mock<ISQLiteRepository>();
        _mockMapper = new Mock<IAnalysisMapper>();
        _mockLogger = new Mock<ILogger<AnalyzeOperation>>();

        _operation = new AnalyzeOperation(
            _mockClassificationService.Object,
            _mockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentException()
    {
        // Arrange
        AnalysisRequest? request = null;

        // Act
        Func<Task> act = async () => await _operation.ExecuteAsync(request!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid request");
    }

    [Theory]
    [InlineData(null, "Test guideline")]
    [InlineData("", "Test guideline")]
    [InlineData("   ", "Test guideline")]
    [InlineData("Test action", null)]
    [InlineData("Test action", "")]
    [InlineData("Test action", "   ")]
    public async Task ExecuteAsync_WithInvalidActionOrGuideline_ThrowsArgumentException(string? action, string? guideline)
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = action,
            Guideline = guideline
        };

        // Act
        Func<Task> act = async () => await _operation.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid request");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisExists_ReturnsExistingAnalysis()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        var existingAnalysis = new Analysis(
            request.Action,
            request.Guideline,
            "COMPLIES",
            0.95m
        );

        var expectedResponse = new AnalysisResponse
        {
            Action = request.Action,
            Guideline = request.Guideline,
            Result = "COMPLIES",
            Confidence = 0.95m,
            TimeStamp = DateTime.UtcNow
        };

        _mockRepository
            .Setup(x => x.GetEntityAsync<Analysis>(It.IsAny<Expression<Func<Analysis, bool>>>()))
            .ReturnsAsync(existingAnalysis);

        _mockMapper
            .Setup(x => x.MapToAnalysisResponse(existingAnalysis))
            .Returns(expectedResponse);

        // Act
        var result = await _operation.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
        
        _mockClassificationService.Verify(
            x => x.ClassifyAsync(It.IsAny<AnalysisRequest>()), 
            Times.Never);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Analysis>()), 
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalysisDoesNotExist_CallsClassificationService()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = "COMPLIES",
            Score = 0.95f
        };

        var analysisRecord = new Analysis(
            request.Action,
            request.Guideline,
            "COMPLIES",
            0.95m
        );

        var expectedResponse = new AnalysisResponse
        {
            Action = request.Action,
            Guideline = request.Guideline,
            Result = "COMPLIES",
            Confidence = 0.95m,
            TimeStamp = DateTime.UtcNow
        };

        _mockRepository
            .Setup(x => x.GetEntityAsync<Analysis>(It.IsAny<Expression<Func<Analysis, bool>>>()))
            .ReturnsAsync((Analysis?)null);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(request))
            .ReturnsAsync(classificationResponse);

        _mockMapper
            .Setup(x => x.MapToAnalysis(request, classificationResponse))
            .Returns(analysisRecord);

        _mockMapper
            .Setup(x => x.MapToAnalysisResponse(request, classificationResponse))
            .Returns(expectedResponse);

        // Act
        var result = await _operation.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);

        _mockClassificationService.Verify(
            x => x.ClassifyAsync(request), 
            Times.Once);

        _mockRepository.Verify(
            x => x.AddAsync(analysisRecord), 
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveFails_LogsErrorAndReturnsResponse()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = "COMPLIES",
            Score = 0.95f
        };

        var expectedResponse = new AnalysisResponse
        {
            Action = request.Action,
            Guideline = request.Guideline,
            Result = "COMPLIES",
            Confidence = 0.95m,
            TimeStamp = DateTime.UtcNow
        };

        _mockRepository
            .Setup(x => x.GetEntityAsync<Analysis>(It.IsAny<Expression<Func<Analysis, bool>>>()))
            .ReturnsAsync((Analysis?)null);

        _mockClassificationService
            .Setup(x => x.ClassifyAsync(request))
            .ReturnsAsync(classificationResponse);

        _mockMapper
            .Setup(x => x.MapToAnalysis(request, classificationResponse))
            .Throws<ArgumentNullException>();

        _mockMapper
            .Setup(x => x.MapToAnalysisResponse(request, classificationResponse))
            .Returns(expectedResponse);

        // Act
        var result = await _operation.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<ArgumentNullException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockClassificationService.Verify(
            x => x.ClassifyAsync(request),
            Times.Once);

        _mockMapper.Verify(
            x => x.MapToAnalysisResponse(request, classificationResponse),
            Times.Once);
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_WhenNoAnalysesExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetAllAsync<Analysis>())
            .ReturnsAsync((IEnumerable<Analysis>?)null);

        // Act
        var result = await _operation.GetHistoryAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_WhenEmptyList_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetAllAsync<Analysis>())
            .ReturnsAsync([]);

        // Act
        var result = await _operation.GetHistoryAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_WithAnalyses_ReturnsOrderedAnalysisResponses()
    {
        // Arrange
        var analysis1 = new Analysis("Action1", "Guideline1", "COMPLIES", 0.95m);
        var analysis2 = new Analysis("Action2", "Guideline2", "DEVIATES", 0.85m);
        var analysis3 = new Analysis("Action3", "Guideline3", "COMPLIES", 0.90m);

        var analyses = new List<Analysis> { analysis1, analysis2, analysis3 };

        var response1 = new AnalysisResponse { Action = "Action1", Guideline = "Guideline1", Result = "COMPLIES", Confidence = 0.95m };
        var response2 = new AnalysisResponse { Action = "Action2", Guideline = "Guideline2", Result = "DEVIATES", Confidence = 0.85m };
        var response3 = new AnalysisResponse { Action = "Action3", Guideline = "Guideline3", Result = "COMPLIES", Confidence = 0.90m };

        _mockRepository
            .Setup(x => x.GetAllAsync<Analysis>())
            .ReturnsAsync(analyses);

        _mockMapper.Setup(x => x.MapToAnalysisResponse(analysis1)).Returns(response1);
        _mockMapper.Setup(x => x.MapToAnalysisResponse(analysis2)).Returns(response2);
        _mockMapper.Setup(x => x.MapToAnalysisResponse(analysis3)).Returns(response3);

        // Act
        var result = await _operation.GetHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Verify all analyses are mapped
        _mockMapper.Verify(x => x.MapToAnalysisResponse(It.IsAny<Analysis>()), Times.Exactly(3));
    }

    #endregion

    #region GetSummaryAsync Tests

    [Fact]
    public async Task GetSummaryAsync_WhenNoAnalysesExist_ReturnsZeroCount()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetColumnValueCountsAsync<Analysis>(
                It.IsAny<Expression<Func<Analysis, string>>>()))
            .ReturnsAsync((Dictionary<string, int>?)null);

        // Act
        var result = await _operation.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
        result.ResultsCount.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_WhenEmptyDictionary_ReturnsZeroCount()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetColumnValueCountsAsync<Analysis>(
                It.IsAny<Expression<Func<Analysis, string>>>()))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        var result = await _operation.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
        result.ResultsCount.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_WithAnalyses_ReturnsCorrectSummary()
    {
        // Arrange
        var resultCounts = new Dictionary<string, int>
        {
            { "COMPLIES", 3 },
            { "DEVIATES", 1 },
            { "UNCLEAR", 1 }
        };

        _mockRepository
            .Setup(x => x.GetColumnValueCountsAsync<Analysis>(
                It.IsAny<Expression<Func<Analysis, string>>>()))
            .ReturnsAsync(resultCounts);

        // Act
        var result = await _operation.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.ResultsCount.Should().NotBeNull();
        result.ResultsCount.Should().HaveCount(3);
        result.ResultsCount!["COMPLIES"].Should().Be(3);
        result.ResultsCount["DEVIATES"].Should().Be(1);
        result.ResultsCount["UNCLEAR"].Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_WithSingleResult_ReturnsCorrectSummary()
    {
        // Arrange
        var resultCounts = new Dictionary<string, int>
        {
            { "COMPLIES", 3 }
        };

        _mockRepository
            .Setup(x => x.GetColumnValueCountsAsync<Analysis>(
                It.IsAny<Expression<Func<Analysis, string>>>()))
            .ReturnsAsync(resultCounts);

        // Act
        var result = await _operation.GetSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.ResultsCount.Should().NotBeNull();
        result.ResultsCount.Should().HaveCount(1);
        result.ResultsCount!["COMPLIES"].Should().Be(3);
    }

    #endregion
}
