module Helios.Core.Models.EnergyMeasurement

open System

type FlowType =
    | Production = 0
    | Consumption = 1

type EnergyMeasurement(time: DateTime, flowType: FlowType, kwh: double) =
    member val Time = time with get, set

    member val FlowType = flowType with get, set

    member val Kwh = kwh with get, set

    override this.Equals(other) =
        match other with
        | :? EnergyMeasurement as other ->
            this.Time = other.Time && this.FlowType = other.FlowType && this.Kwh = other.Kwh
        | _ -> false

    override this.GetHashCode() =
        HashCode.Combine(this.Time, this.FlowType, this.Kwh)
