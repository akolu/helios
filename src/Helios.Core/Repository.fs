module Helios.Core.Repository

open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Database
open Helios.Core.Models.ElectricitySpotPrice
open System
open Microsoft.Extensions.Logging

type SolarPanelOutputRepository(db: HeliosDatabaseContext, logger: ILogger) =
    member _.Find(startDate, endDate) =
        db.SolarPanelOutputs
        |> Seq.filter (fun m -> m.Time >= startDate && m.Time <= endDate)
        |> Seq.toList

    member _.Save(yields: SolarPanelOutput list) =
        let existingMeasurements =
            db.SolarPanelOutputs
            |> Seq.filter (fun m -> yields |> List.exists (fun y -> y.Time = m.Time))
            |> Seq.toList

        let existingRows, newRows =
            yields
            |> List.partition (fun row -> existingMeasurements |> List.exists (fun m -> row.Time = m.Time))

        existingRows
        |> List.iter (fun row ->
            logger.LogWarning(sprintf "Warning: SolarPanelOutput %s already exists, ignoring" (row.ToString())))

        db.SolarPanelOutputs.AddRange(newRows)
        db.SaveChanges() |> ignore

type ElectricitySpotPriceRepository(db: HeliosDatabaseContext, logger: ILogger) =
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
        |> List.iter (fun row ->
            logger.LogWarning(sprintf "Warning: ElectricitySpotPrice %s already exists, ignoring" (row.ToString())))

        db.ElectricitySpotPrices.AddRange(newRows)
        db.SaveChanges() |> ignore

type HouseholdEnergyReadingRepository(db: HeliosDatabaseContext, logger: ILogger) =
    member _.Find(startDate, endDate) =
        db.HouseholdEnergyReadings
        |> Seq.filter (fun m -> m.Time >= startDate && m.Time <= endDate)
        |> Seq.toList

    member _.Save(readings: HouseholdEnergyReading list) =
        let existingReadings =
            db.HouseholdEnergyReadings
            |> Seq.filter (fun m -> readings |> List.exists (fun y -> y.Time = m.Time))
            |> Seq.toList

        let existingRows, newRows =
            readings
            |> List.partition (fun row -> existingReadings |> List.exists (fun m -> row.Time = m.Time))

        existingRows
        |> List.iter (fun row ->
            logger.LogWarning(sprintf "HouseholdEnergyReading %s already exists, ignoring" (row.ToString())))

        db.HouseholdEnergyReadings.AddRange(newRows)
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
    { SolarPanelOutput: SolarPanelOutputRepository
      HouseholdEnergyReading: HouseholdEnergyReadingRepository
      ElectricitySpotPrice: ElectricitySpotPriceRepository
      Reports: ReportsRepository }

    static member Init(db: HeliosDatabaseContext, logger: ILogger) =
        { SolarPanelOutput = new SolarPanelOutputRepository(db, logger)
          HouseholdEnergyReading = new HouseholdEnergyReadingRepository(db, logger)
          ElectricitySpotPrice = new ElectricitySpotPriceRepository(db, logger)
          Reports = new ReportsRepository(db, logger) }
