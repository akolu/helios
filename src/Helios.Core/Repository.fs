module Helios.Core.Repository

open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Database
open Helios.Core.Models.ElectricitySpotPrice

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

type Repositories =
    { EnergyMeasurement: EnergyMeasurementRepository
      ElectricitySpotPrice: ElectricitySpotPriceRepository }

    static member Init(db: HeliosDatabaseContext) =
        { EnergyMeasurement = new EnergyMeasurementRepository(db)
          ElectricitySpotPrice = new ElectricitySpotPriceRepository(db) }
