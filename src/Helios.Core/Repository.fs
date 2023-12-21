module Helios.Core.Repository

open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Database
open Helios.Core.Models.ElectricitySpotPrice
open System
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Helios.Core.Models

type ModelRepository<'T when 'T :> ITimeSeries and 'T: not struct>
    (db: HeliosDatabaseContext, dbSet: DbSet<'T>, logger: ILogger) =
    member _.Find(startDate: DateTimeOffset, endDate: DateTimeOffset) =
        dbSet
        |> Seq.filter (fun m -> let time = m.Time in time >= startDate && time <= endDate)
        |> Seq.toList

    member _.Save(items: 'T list) =
        let existingItems =
            dbSet
            |> Seq.filter (fun m -> items |> List.exists (fun y -> y.Time = m.Time))
            |> Seq.toList

        let existingRows, newRows =
            items
            |> List.partition (fun row -> existingItems |> List.exists (fun m -> row.Time = m.Time))

        existingRows
        |> List.iter (fun row -> logger.LogWarning(sprintf "Warning: %A already exists, ignoring" row))

        dbSet.AddRange(newRows)
        db.SaveChanges() |> ignore

type EnergySavingsDatabaseResult =
    { Time: DateTimeOffset
      SolarPanelOutput: float
      Consumption: float
      Production: float
      Price: decimal }

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


type ReportsRepository(db: HeliosDatabaseContext, logger: ILogger) =
    member _.SolarEnergySavingsReport =
        let fixedCosts = 5.62m + 2.79372m + 0.49m

        query {
            for reading in db.HouseholdEnergyReadings do
                join output in db.SolarPanelOutputs on (reading.Time = output.Time)
                join spotPrice in db.ElectricitySpotPrices on (reading.Time = spotPrice.Time)

                select
                    { Time = reading.Time
                      SolarPanelOutput = output.Kwh
                      Consumption = reading.Consumption
                      Production = reading.Production
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
                          Savings = 0.0m
                          SavingsAcc = 0.0m
                          SoldToGrid = 0.0m
                          SoldToGridAcc = 0.0m
                          NetTotal = 0.0m
                          NetTotalAcc = 0.0m }

                let netConsumption = row.Consumption - row.Production
                let surplus = if netConsumption < 0 then Math.Abs(netConsumption) else 0.0

                let savings =
                    decimal (row.SolarPanelOutput - surplus) * (row.Price + fixedCosts) / 100.0m

                let soldToGrid = decimal surplus * row.Price / 100m
                let netTotal = savings + soldToGrid

                let newRow =
                    { Time = row.Time
                      Consumption = row.Consumption
                      Production = row.Production
                      Surplus = surplus
                      SpotPrice = row.Price
                      Savings = savings
                      SavingsAcc = lastRow.SavingsAcc + savings
                      SoldToGrid = soldToGrid
                      SoldToGridAcc = lastRow.SoldToGridAcc + soldToGrid
                      NetTotal = netTotal
                      NetTotalAcc = lastRow.NetTotalAcc + netTotal }

                acc @ [ newRow ])
            []

type Repositories =
    { SolarPanelOutput: ModelRepository<SolarPanelOutput>
      HouseholdEnergyReading: ModelRepository<HouseholdEnergyReading>
      ElectricitySpotPrice: ModelRepository<ElectricitySpotPrice>
      Reports: ReportsRepository }

    static member Init(db: HeliosDatabaseContext, logger: ILogger) =
        { SolarPanelOutput = new ModelRepository<SolarPanelOutput>(db, db.SolarPanelOutputs, logger)
          HouseholdEnergyReading = new ModelRepository<HouseholdEnergyReading>(db, db.HouseholdEnergyReadings, logger)
          ElectricitySpotPrice = new ModelRepository<ElectricitySpotPrice>(db, db.ElectricitySpotPrices, logger)
          Reports = new ReportsRepository(db, logger) }
