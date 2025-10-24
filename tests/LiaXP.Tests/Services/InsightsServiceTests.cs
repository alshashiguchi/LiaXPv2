using Xunit;
using FluentAssertions;
using Moq;
using LiaXP.Domain.Interfaces;
using LiaXP.Infrastructure.Services;
using LiaXP.Domain.Entities;

namespace LiaXP.Tests.Services;

public class InsightsServiceTests
{
    private readonly Mock<ISalesDataSource> _mockSalesDataSource;
    private readonly InsightsService _sut;

    public InsightsServiceTests()
    {
        _mockSalesDataSource = new Mock<ISalesDataSource>();
        _sut = new InsightsService(_mockSalesDataSource.Object);
    }

    [Fact]
    public async Task CalculateInsights_WithSales_ReturnsCorrectTotalSales()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new Sale { TotalValue = 100, AvgTicket = 50 },
            new Sale { TotalValue = 200, AvgTicket = 100 }
        };
        
        _mockSalesDataSource
            .Setup(x => x.GetSalesByCompanyAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(sales);
        
        _mockSalesDataSource
            .Setup(x => x.GetGoalsByCompanyAsync(companyId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Goal>());

        // Act
        var result = await _sut.CalculateInsightsAsync(companyId);

        // Assert
        result.TotalSales.Should().Be(300);
        result.AvgTicket.Should().Be(75);
    }

    [Fact]
    public async Task CalculateInsights_WithGoals_CalculatesGapCorrectly()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new Sale { TotalValue = 500, AvgTicket = 50 }
        };
        
        var goals = new List<Goal>
        {
            new Goal { TargetValue = 1000 }
        };
        
        _mockSalesDataSource
            .Setup(x => x.GetSalesByCompanyAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(sales);
        
        _mockSalesDataSource
            .Setup(x => x.GetGoalsByCompanyAsync(companyId, It.IsAny<DateTime>()))
            .ReturnsAsync(goals);

        // Act
        var result = await _sut.CalculateInsightsAsync(companyId);

        // Assert
        result.GoalGap.Should().Be(500);
        result.GoalProgress.Should().Be(50);
    }

    [Fact]
    public async Task CalculateInsights_WithNoSales_ReturnsZeroValues()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        
        _mockSalesDataSource
            .Setup(x => x.GetSalesByCompanyAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Sale>());
        
        _mockSalesDataSource
            .Setup(x => x.GetGoalsByCompanyAsync(companyId, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Goal>());

        // Act
        var result = await _sut.CalculateInsightsAsync(companyId);

        // Assert
        result.TotalSales.Should().Be(0);
        result.AvgTicket.Should().Be(0);
    }
}
