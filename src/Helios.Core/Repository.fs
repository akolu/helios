module Helios.Core.Repository

open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Database

type EnergyMeasurementRepository(db: HeliosDatabaseContext) =
    member _.Find(startDate, endDate, flowType) =
        db.EnergyMeasurements
        |> Seq.filter (fun m -> m.Time >= startDate && m.Time <= endDate && m.FlowType = flowType)
        |> Seq.toList

    member _.Save(yields: EnergyMeasurement list) =
        db.EnergyMeasurements.AddRange(yields)
        db.SaveChanges() |> ignore
