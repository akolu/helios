module ElectricitySpotPriceTests

open Xunit
open System
open Helios.Core.Models.ElectricitySpotPrice
open Helios.Core.DataProviders.ApiClients.EntsoEClient.Types
open Microsoft.Extensions.Logging
open Moq

[<Fact>]
let ``fromEntsoETransmissionDayAheadPricesResponse should map response to ElectricitySpotPrice list`` () =
    // Arrange
    let timeSeries =
        [ { TimeSeriesPeriod.TimeInterval =
              { TimeSeriesPeriodInterval.StartDt = DateTimeOffset.Parse("2023-03-25T23:00Z")
                TimeSeriesPeriodInterval.EndDt = DateTimeOffset.Parse("2023-03-26T01:00Z") }
            TimeSeriesPeriod.Points =
              [ { TimeSeriesPoint.Position = 1
                  TimeSeriesPoint.PriceAmount = 39.66m }
                { TimeSeriesPoint.Position = 2
                  TimeSeriesPoint.PriceAmount = 39.23m } ] }
          { TimeSeriesPeriod.TimeInterval =
              { TimeSeriesPeriodInterval.StartDt = DateTimeOffset.Parse("2023-03-27T00:00Z")
                TimeSeriesPeriodInterval.EndDt = DateTimeOffset.Parse("2023-03-27T01:00Z") }
            TimeSeriesPeriod.Points =
              [ { TimeSeriesPoint.Position = 1
                  TimeSeriesPoint.PriceAmount = 42.13m } ] } ]

    let loggerMock = new Mock<ILogger>(MockBehavior.Strict)

    let expected =
        [ new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-25T23:00:00Z"), 3.966m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-26T00:00:00Z"), 3.923m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-27T00:00:00Z"), 4.213m) ]

    // Act
    let actual =
        fromEntsoETransmissionDayAheadPricesResponse loggerMock.Object timeSeries

    // Assert
    Assert.Equal<ElectricitySpotPrice list>(expected, actual)

[<Fact>]
let ``Missing positions should be handled gracefully`` () =
    // Arrange
    let timeSeries =
        [ { TimeSeriesPeriod.TimeInterval =
              { TimeSeriesPeriodInterval.StartDt = DateTimeOffset.Parse("2024-09-25T22:00Z")
                TimeSeriesPeriodInterval.EndDt = DateTimeOffset.Parse("2024-09-26T02:00Z") }
            TimeSeriesPeriod.Points =
              [ { TimeSeriesPoint.Position = 1
                  TimeSeriesPoint.PriceAmount = -1.21m }
                { TimeSeriesPoint.Position = 2
                  TimeSeriesPoint.PriceAmount = -1.77m }
                // Position 3 is missing
                { TimeSeriesPoint.Position = 4
                  TimeSeriesPoint.PriceAmount = -1.04m } ] } ]

    // Create mock for testing
    let loggerMock = new Mock<ILogger>(MockBehavior.Strict)

    loggerMock
        .Setup(fun l ->
            l.Log(
                It.Is<LogLevel>(fun level -> level = LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ))
        .Verifiable()

    // Act
    let actual =
        fromEntsoETransmissionDayAheadPricesResponse loggerMock.Object timeSeries

    // Assert
    loggerMock.Verify()
    // Verify data was processed correctly
    Assert.Equal(4, actual.Length)

    let expectedPrices =
        [ new ElectricitySpotPrice(DateTimeOffset.Parse("2024-09-25T22:00:00Z"), -0.121m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2024-09-25T23:00:00Z"), -0.177m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2024-09-26T00:00:00Z"), -0.177m) // Position 3 filled with previous value
          new ElectricitySpotPrice(DateTimeOffset.Parse("2024-09-26T01:00:00Z"), -0.104m) ]

    for i in 0..3 do
        Assert.Equal(expectedPrices[i], actual[i])
