module ElectricitySpotPriceTests

open Xunit
open System
open Helios.Core.Models.ElectricitySpotPrice
open Helios.Core.Services.EntsoE.Types

[<Fact>]
let ``fromEntsoETransmissionDayAheadPricesResponse should map response to ElectricitySpotPrice list`` () =
    let timeSeries =
        [ { TimeSeriesPeriod.timeInterval =
              { TimeSeriesPeriodInterval.startDt = DateTimeOffset.Parse("2023-03-25T23:00Z")
                TimeSeriesPeriodInterval.endDt = DateTimeOffset.Parse("2023-03-26T01:00Z") }
            TimeSeriesPeriod.points =
              [ { TimeSeriesPoint.position = 1
                  TimeSeriesPoint.priceAmount = 39.66m }
                { TimeSeriesPoint.position = 2
                  TimeSeriesPoint.priceAmount = 39.23m } ] }
          { TimeSeriesPeriod.timeInterval =
              { TimeSeriesPeriodInterval.startDt = DateTimeOffset.Parse("2023-03-27T00:00Z")
                TimeSeriesPeriodInterval.endDt = DateTimeOffset.Parse("2023-03-27T01:00Z") }
            TimeSeriesPeriod.points =
              [ { TimeSeriesPoint.position = 1
                  TimeSeriesPoint.priceAmount = 42.13m } ] } ]

    let expected =
        [ new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-25T23:00:00Z"), 3.966m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-26T00:00:00Z"), 3.923m)
          new ElectricitySpotPrice(DateTimeOffset.Parse("2023-03-27T00:00:00Z"), 4.213m) ]

    let actual = fromEntsoETransmissionDayAheadPricesResponse timeSeries
    Assert.Equal<ElectricitySpotPrice list>(expected, actual)
