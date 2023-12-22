namespace Helios.Core.Services

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Helios.Core.Repository
open Helios.Core.Utils
open System

type EnergySavingsReport =
    { Time: DateTimeOffset
      Consumption: float
      Production: float
      Surplus: float
      SpotPrice: decimal
      Savings: decimal
      SavingsAcc: decimal
      SoldToGrid: decimal
      SoldToGridAcc: decimal
      NetTotal: decimal
      NetTotalAcc: decimal }

    static member NoData =
        { Time = DateTimeOffset.MinValue
          Consumption = 0.0
          Production = 0.0
          Surplus = 0.0
          SpotPrice = 0.0m
          Savings = 0.0m
          SavingsAcc = 0.0m
          SoldToGrid = 0.0m
          SoldToGridAcc = 0.0m
          NetTotal = 0.0m
          NetTotalAcc = 0.0m }

type Reporting =
    { repositories: Repositories
      config: IConfiguration
      logger: ILogger }

    member this.EnergySavings() =
        let fixedCosts = 5.62m + 2.79372m + 0.49m

        this.repositories.Reports.GetTimeSeriesData
        |> List.sortBy (fun row -> row.Time)
        |> List.fold
            (fun (acc: EnergySavingsReport list) (row: TimeSeriesData) ->
                let lastRow = Option.defaultValue EnergySavingsReport.NoData (acc |> List.tryLast)
                let netConsumption = row.Consumption - row.Production
                let surplus = if netConsumption < 0 then Math.Abs(netConsumption) else 0.0
                let savings = decimal (row.KwhOutput - surplus) * (row.Price + fixedCosts) / 100.0m
                let soldToGrid = decimal surplus * row.Price / 100m
                let netTotal = savings + soldToGrid

                acc
                @ [ { Time = row.Time
                      Consumption = row.Consumption
                      Production = row.Production
                      Surplus = surplus
                      SpotPrice = row.Price
                      Savings = savings
                      SavingsAcc = lastRow.SavingsAcc + savings
                      SoldToGrid = soldToGrid
                      SoldToGridAcc = lastRow.SoldToGridAcc + soldToGrid
                      NetTotal = netTotal
                      NetTotalAcc = lastRow.NetTotalAcc + netTotal } ])
            []
        |> tap (List.iter (fun x -> this.logger.LogDebug(sprintf "%A" x)))

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          config = config
          logger = logger }
