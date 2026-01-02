using FluentAssertions;
using ProcessMonitorApi.Contracts;
using ProcessMonitorApi.Mappers.Implementations;
using ProcessMonitorApi.Models;
using Xunit;

namespace Domain.UnitTests.Mappers;

public class AnalysisMapperTests
{
    private readonly AnalysisMapper _sut;

    public AnalysisMapperTests()
    {
        _sut = new AnalysisMapper();
    }

    #region MapToAnalysis Tests

    [Fact]
    public void MapToAnalysis_WithValidInputs_ReturnsAnalysis()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "User deleted a customer record",
            Guideline = "Data protection compliance"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = "COMPLIES",
            Score = 0.956789f
        };

        // Act
        var result = _sut.MapToAnalysis(request, classificationResponse);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(request.Action);
        result.Guideline.Should().Be(request.Guideline);
        result.Result.Should().Be(classificationResponse.Label);
        result.Confidence.Should().Be(0.96m); // Rounded to 2 decimal places
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MapToAnalysis_WithNullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        // Act
        Action act = () => _sut.MapToAnalysis(request, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void MapToAnalysis_WithNullLabel_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = null,
            Score = 0.95f
        };

        // Act
        Action act = () => _sut.MapToAnalysis(request, classificationResponse);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void MapToAnalysis_WithNullScore_ThrowsArgumentNullException()
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
            Score = null
        };

        // Act
        Action act = () => _sut.MapToAnalysis(request, classificationResponse);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Theory]
    [InlineData(0.123456f, 0.12)]
    [InlineData(0.999999f, 1.00)]
    [InlineData(0.005f, 0.00)]
    [InlineData(0.015f, 0.02)]
    public void MapToAnalysis_RoundsConfidenceToTwoDecimalPlaces(float score, decimal expectedConfidence)
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = "UNCLEAR",
            Score = score
        };

        // Act
        var result = _sut.MapToAnalysis(request, classificationResponse);

        // Assert
        result.Confidence.Should().Be(expectedConfidence);
    }

    #endregion

    #region MapToAnalysisResponse (from Request and ClassificationResponse) Tests

    [Fact]
    public void MapToAnalysisResponse_WithValidInputs_ReturnsAnalysisResponse()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "User accessed sensitive data",
            Guideline = "Security policy compliance"
        };

        var classificationResponse = new ClassificationResponse
        {
            Label = "DEVIATES",
            Score = 0.876543f
        };

        // Act
        var result = _sut.MapToAnalysisResponse(request, classificationResponse);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(request.Action);
        result.Guideline.Should().Be(request.Guideline);
        result.Result.Should().Be(classificationResponse.Label);
        result.Confidence.Should().Be(0.88m); // Rounded to 2 decimal places
        result.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MapToAnalysisResponse_WithNullClassificationResponse_ReturnsResponseWithNullResultAndConfidence()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            Action = "Test action",
            Guideline = "Test guideline"
        };

        // Act
        Action act = () => _sut.MapToAnalysisResponse(request, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("classificationResponse");
    }

    [Fact]
    public void MapToAnalysisResponse_WithNullScore_ReturnsResponseWithNullConfidence()
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
            Score = null
        };

        // Act
        Action act = () => _sut.MapToAnalysisResponse(request, classificationResponse);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("classificationResponse");
    }

    #endregion

    #region MapToAnalysisResponse (from Analysis) Tests

    [Fact]
    public void MapToAnalysisResponse_FromAnalysis_ReturnsAnalysisResponse()
    {
        // Arrange
        var analysis = new Analysis(
            "User deleted a customer record",
            "Data protection compliance",
            "COMPLIES",
            0.95m
        );

        // Act
        var result = _sut.MapToAnalysisResponse(analysis);

        // Assert
        result.Should().NotBeNull();
        result.Action.Should().Be(analysis.Action);
        result.Guideline.Should().Be(analysis.Guideline);
        result.Result.Should().Be(analysis.Result);
        result.Confidence.Should().Be(analysis.Confidence);
        result.TimeStamp.Should().Be(analysis.CreatedAt.ToUniversalTime());
    }

    [Fact]
    public void MapToAnalysisResponse_FromAnalysis_ConvertsCreatedAtToUtc()
    {
        // Arrange
        var analysis = new Analysis(
            "Test action",
            "Test guideline",
            "COMPLIES",
            0.95m
        );

        // Act
        var result = _sut.MapToAnalysisResponse(analysis);

        // Assert
        result.TimeStamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion
}
