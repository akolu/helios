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

type EnergyConsumptionReport =
    { TimePeriod: string
      NetConsumption: float
      AverageSpotPrice: decimal
      WeightedPriceVat0: decimal
      CostEstimate: decimal
      TransmissionCosts: decimal
      NetTotal: decimal }

type GroupBy =
    | Day
    | Month
    | Year

type Reporting =
    { repositories: Repositories
      config: IConfiguration
      logger: ILogger }

    member this.EnergySavings(dateFrom, dateTo) =
        let fixedCosts = 5.62m + 2.79372m + 0.49m

        this.repositories.Reports.GetEnergySavingsData(dateFrom, dateTo)
        |> List.sortBy (fun row -> row.Time)
        |> List.fold
            (fun (acc: EnergySavingsReport list) (row: EnergySavingsData) ->
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


    // TODO:
    // - bugfix: figure out inconsistencies with billed invoice data (check that AmountPaid and TransmissionCosts match the corresponding invoices)
    //    - take into account fixed prices (e.g. Transmission fees) changing over time -> Elenia price change in 5/2023
    //    - manually double-check small (rounding?) errors in Excel after grouping feature is done
    // - feat: add support for limiting the report by date range
    // - feat: add support for grouping by day/month/year
    // - refactor: move reports to separate files e.g. under Report folder
    member this.EnergyConsumption(dateFrom: DateTimeOffset, dateTo: DateTimeOffset, groupBy: GroupBy) =
        this.repositories.Reports.GetEnergyConsumptionData(dateFrom, dateTo)
        |> List.sortBy (fun row -> row.Time)
        |> List.groupBy (fun row ->
            let localTime = DateTimeOffset.Parse(row.Time).ToLocalTime()

            match groupBy with
            | Day -> localTime.Date.ToShortDateString()
            | Month -> localTime.Month.ToString() + "/" + localTime.Year.ToString()
            | Year -> localTime.Year.ToString())
        // need to group by local date, not UTC date
        |> List.map (fun (timePeriod, rows) ->

            let totalConsumption =
                rows |> List.sumBy (fun row -> Math.Max((row.Consumption - row.Production), 0))

            let totalWeightedPrice =
                rows |> List.sumBy (fun row -> decimal row.Consumption * row.Price)

            let avgSpotPrice =
                (rows |> List.sumBy (fun row -> row.Price)) / decimal (List.length rows)

            let averagePriceVat0 =
                if totalConsumption > 0.0 then
                    totalWeightedPrice / decimal totalConsumption
                else
                    0.0m

            let costEstimate =
                (averagePriceVat0 * 1.24m + 0.49m) * decimal totalConsumption / 100.0m // add VAT 24% and fixed spot margin 0.49c/kWh

            let transmissionCosts =
                (decimal totalConsumption * (5.62m + 2.79372m) / 100.0m) + 21.24m // transmission cost 5.62c/KWh + tax 2.79372c/KWh + fixed fee 21.24€/month

            { TimePeriod = timePeriod
              NetConsumption = totalConsumption
              AverageSpotPrice = avgSpotPrice
              WeightedPriceVat0 = averagePriceVat0
              CostEstimate = costEstimate
              TransmissionCosts = transmissionCosts
              NetTotal = costEstimate + transmissionCosts })

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          config = config
          logger = logger }
