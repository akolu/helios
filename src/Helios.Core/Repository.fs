module Helios.Core.Repository

open Microsoft.FSharp.Linq
open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Database
open Helios.Core.Models.ElectricitySpotPrice
open System
open Microsoft.EntityFrameworkCore
open System.Data

type EnergyMeasurementRepository(db: HeliosDatabaseContext) =
    member _.Find(startDate, endDate, flowType) =
        db.EnergyMeasurements
        |> Seq.filter (fun m -> m.Time >= startDate && m.Time <= endDate && m.FlowType = flowType)
        |> Seq.toList

    member _.Save(yields: EnergyMeasurement list) =
        let existingMeasurements =
            db.EnergyMeasurements
            |> Seq.filter (fun m -> yields |> List.exists (fun y -> y.Time = m.Time && y.FlowType = m.FlowType))
            |> Seq.toList

        let existingRows, newRows =
            yields
            |> List.partition (fun row ->
                existingMeasurements
                |> List.exists (fun m -> row.Time = m.Time && row.FlowType = m.FlowType))

        existingRows
        |> List.iter (fun row -> printfn "Warning: EnergyMeasurement %s already exists, ignoring" (row.ToString()))

        db.EnergyMeasurements.AddRange(newRows)
        db.SaveChanges() |> ignore

type ElectricitySpotPriceRepository(db: HeliosDatabaseContext) =
    member _.Find(startDate, endDate) =
        db.ElectricitySpotPrices
        |> Seq.filter (fun m -> m.Time >= startDate && m.Time <= endDate)
        |> Seq.toList

    member _.Save(prices: ElectricitySpotPrice list) =
        let existingPrices =
            db.ElectricitySpotPrices
            |> Seq.filter (fun m -> prices |> List.exists (fun y -> y.Time = m.Time))
            |> Seq.toList

        let existingRows, newRows =
            prices
            |> List.partition (fun row -> existingPrices |> List.exists (fun m -> row.Time = m.Time))

        existingRows
        |> List.iter (fun row -> printfn "Warning: ElectricitySpotPrice %s already exists, ignoring" (row.ToString()))

        db.ElectricitySpotPrices.AddRange(newRows)
        db.SaveChanges() |> ignore


type EnergySavingsDatabaseResult =
    { Time: DateTimeOffset
      Consumption: float
      Production: float
      Price: decimal }

type EnergySavingsReport =
    { Time: DateTimeOffset
      Consumption: float
      Production: float
      Surplus: float
      SpotPrice: decimal
      GrossCost: decimal
      GrossCostAcc: decimal
      Savings: decimal
      SavingsAcc: decimal
      SoldToGrid: decimal
      SoldToGridAcc: decimal
      NetTotal: decimal
      NetTotalAcc: decimal }


type ReportsRepository(db: HeliosDatabaseContext) =
    member _.SolarEnergySavingsReport =
        let staticCostsPerKwh = 5.62m + 2.79372m + 0.49m

        query {
            for consumption in db.EnergyMeasurements do
                join production in db.EnergyMeasurements on (consumption.Time = production.Time)
                join spotPrice in db.ElectricitySpotPrices on (consumption.Time = spotPrice.Time)

                where (
                    consumption.FlowType = FlowType.Consumption
                    && production.FlowType = FlowType.Production
                )

                select
                    { Time = consumption.Time
                      Consumption = consumption.Kwh
                      Production = production.Kwh
                      Price = spotPrice.EuroCentsPerKWh }
        }
        |> Seq.toList
        |> List.sortBy (fun row -> row.Time)
        |> List.fold
            (fun (acc: EnergySavingsReport list) (row: EnergySavingsDatabaseResult) ->
                let lastRow =
                    match acc |> List.tryLast with
                    | Some row -> row
                    | None ->
                        { Time = DateTimeOffset.MinValue
                          Consumption = 0.0
                          Production = 0.0
                          Surplus = 0.0
                          SpotPrice = 0.0m
                          GrossCost = 0.0m
                          GrossCostAcc = 0.0m
                          Savings = 0.0m
                          SavingsAcc = 0.0m
                          SoldToGrid = 0.0m
                          SoldToGridAcc = 0.0m
                          NetTotal = 0.0m
                          NetTotalAcc = 0.0m }

                let grossCost = decimal row.Consumption * (row.Price + staticCostsPerKwh) / 100.0m
                let surplus = if row.Consumption < 0 then Math.Abs(row.Consumption) else 0

                let savings =
                    decimal (row.Production - surplus) * (row.Price + staticCostsPerKwh) / 100.0m

                let soldToGrid = decimal surplus * row.Price / 100m
                let netTotal = savings + soldToGrid

                let newRow =
                    { Time = row.Time
                      Consumption = row.Consumption
                      Production = row.Production
                      Surplus = surplus
                      SpotPrice = row.Price
                      GrossCost = grossCost
                      GrossCostAcc = lastRow.GrossCostAcc + grossCost
                      Savings = savings
                      SavingsAcc = lastRow.SavingsAcc + savings
                      SoldToGrid = soldToGrid
                      SoldToGridAcc = lastRow.SoldToGridAcc + soldToGrid
                      NetTotal = netTotal
                      NetTotalAcc = lastRow.NetTotalAcc + netTotal }

                acc @ [ newRow ])
            []






// query {
//     for consumption in db.EnergyMeasurements do
//         join production in db.EnergyMeasurements on (consumption.Time = production.Time)
//         join spotPrice in db.ElectricitySpotPrices on (consumption.Time = spotPrice.Time)

//         where (
//             consumption.FlowType = FlowType.Consumption
//             && production.FlowType = FlowType.Production
//         )

//         groupBy consumption.Time into g

//         select
//             { Time = g.Key
//               Consumption = g |> Seq.head |> (fun (c, _, _) -> c.Kwh)
//               Production = g |> Seq.head |> (fun (_, p, _) -> p.Kwh)
//               Price = g |> Seq.head |> (fun (_, _, sp) -> sp.EuroCentsPerKWh) }
// }
// |> Seq.toList


// let generateReport2 =
//     let sql =
//         @"
//         SELECT c.Time AS Time, c.Kwh AS Consumption, p.Kwh AS Production, esp.EuroCentsPerKWh AS Price
//         FROM EnergyMeasurements c
//         INNER JOIN EnergyMeasurements p ON c.Time = p.Time AND p.FlowType = @productionFlowType
//         INNER JOIN ElectricitySpotPrices esp ON c.Time = esp.Time
//         WHERE c.FlowType = @consumptionFlowType
//         GROUP BY c.Time
//         ORDER BY c.Time ASC"

//     let consumptionFlowTypeParam =
//         new SqlParameter("@consumptionFlowType", SqlDbType.Int)

//     consumptionFlowTypeParam.Value <- int FlowType.Consumption

//     let productionFlowTypeParam = new SqlParameter("@productionFlowType", SqlDbType.Int)
//     productionFlowTypeParam.Value <- int FlowType.Production

//     db.Database.SqlQuery<EnergyReport>(sql, consumptionFlowTypeParam, productionFlowTypeParam)
//     |> Seq.toList


type Repositories =
    { EnergyMeasurement: EnergyMeasurementRepository
      ElectricitySpotPrice: ElectricitySpotPriceRepository
      Reports: ReportsRepository }

    static member Init(db: HeliosDatabaseContext) =
        { EnergyMeasurement = new EnergyMeasurementRepository(db)
          ElectricitySpotPrice = new ElectricitySpotPriceRepository(db)
          Reports = new ReportsRepository(db) }
