module Helios.Core.Repository

open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Database
open Helios.Core.Models.ElectricitySpotPrice
open System
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Helios.Core.Models
open Microsoft.Data.Sqlite
open Dapper

type ModelRepository<'T when 'T :> ITimeSeries and 'T: not struct>
    (db: HeliosDatabaseContext, dbSet: DbSet<'T>, logger: ILogger) =
    member _.Find(startDate: DateTimeOffset, endDate: DateTimeOffset) =
        dbSet
        |> Seq.filter (fun m -> let time = m.Time in time >= startDate && time <= endDate)
        |> Seq.toList

    member _.FindLatest() = dbSet |> Seq.maxBy (fun m -> m.Time)

    member _.FindFirst() = dbSet |> Seq.minBy (fun m -> m.Time)

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

[<CLIMutable>]
type EnergyConsumptionData =
    { Time: string
      Consumption: float
      Production: float
      Price: decimal }

type ReportsRepository(db: HeliosDatabaseContext) =
    member _.GetEnergySavingsData(dateFrom: DateTime, dateTo: DateTime) =
        query {
            for reading in db.HouseholdEnergyReadings do
                join output in db.SolarPanelOutputs on (reading.Time.DateTime = output.Time.DateTime)
                join spotPrice in db.ElectricitySpotPrices on (reading.Time.DateTime = spotPrice.Time.DateTime)

                where (reading.Time.DateTime >= dateFrom && reading.Time.DateTime <= dateTo)

                select
                    { Time = reading.Time
                      KwhOutput = output.Kwh // TODO: modify so that non-existent KwhOutputs are treated as 0
                      Consumption = reading.Consumption
                      Production = reading.Production
                      Price = spotPrice.EuroCentsPerKWh }
        }
        |> Seq.toList

    member _.GetEnergyConsumptionData(dateFrom: DateTimeOffset, dateTo: DateTimeOffset) =
        using (new SqliteConnection("Data Source=Helios.sqlite")) (fun connection ->
            let sql =
                """
                SELECT reading.Time, reading.Consumption, reading.Production, spotPrice.EuroCentsPerKWh AS Price
                FROM HouseholdEnergyReadings reading
                JOIN ElectricitySpotPrices spotPrice ON reading.Time = spotPrice.Time
                WHERE reading.Time BETWEEN @From AND @To
            """

            let parameters = new DynamicParameters()
            parameters.Add("@From", dateFrom)
            parameters.Add("@To", dateTo)

            let results = connection.Query<EnergyConsumptionData>(sql, parameters)
            results |> Seq.toList)

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
