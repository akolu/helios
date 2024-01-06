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

type EnergySavingsData =
    { Time: DateTimeOffset
      KwhOutput: float
      Consumption: float
      Production: float
      Price: decimal }

type EnergyConsumptionData =
    { Time: DateTimeOffset
      Consumption: float
      Production: float
      Price: decimal }

type ReportsRepository(db: HeliosDatabaseContext) =
    member _.GetEnergySavingsData =
        query {
            for reading in db.HouseholdEnergyReadings do
                join output in db.SolarPanelOutputs on (reading.Time = output.Time)
                join spotPrice in db.ElectricitySpotPrices on (reading.Time = spotPrice.Time)

                select
                    { Time = reading.Time
                      KwhOutput = output.Kwh // TODO: modify so that non-existent KwhOutputs are treated as 0
                      Consumption = reading.Consumption
                      Production = reading.Production
                      Price = spotPrice.EuroCentsPerKWh }
        }
        |> Seq.toList

    member _.GetEnergyConsumptionData =
        query {
            for reading in db.HouseholdEnergyReadings do
                join spotPrice in db.ElectricitySpotPrices on (reading.Time = spotPrice.Time)

                select
                    { Time = reading.Time
                      Consumption = reading.Consumption
                      Production = reading.Production
                      Price = spotPrice.EuroCentsPerKWh }
        }
        |> Seq.toList

type Repositories =
    { SolarPanelOutput: ModelRepository<SolarPanelOutput>
      HouseholdEnergyReading: ModelRepository<HouseholdEnergyReading>
      ElectricitySpotPrice: ModelRepository<ElectricitySpotPrice>
      Reports: ReportsRepository }

    static member Init(db: HeliosDatabaseContext, logger: ILogger) =
        { SolarPanelOutput = new ModelRepository<SolarPanelOutput>(db, db.SolarPanelOutputs, logger)
          HouseholdEnergyReading = new ModelRepository<HouseholdEnergyReading>(db, db.HouseholdEnergyReadings, logger)
          ElectricitySpotPrice = new ModelRepository<ElectricitySpotPrice>(db, db.ElectricitySpotPrices, logger)
          Reports = new ReportsRepository(db) }
