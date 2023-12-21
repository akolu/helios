module Helios.Core.Models.HouseholdEnergyReading

open System

type HouseholdEnergyReading(time: DateTimeOffset, production: double, consumption: double) =
    member val Time = time with get, set

    member val Production = production with get, set

    member val Consumption = consumption with get, set

    override this.ToString() =
        sprintf "HouseholdEnergyReading(%A, %A, %A)" this.Time this.Production this.Consumption

    interface ITimeSeries with
        member this.Time = this.Time

    override this.Equals(other) =
        match other with
        | :? HouseholdEnergyReading as other ->
            this.Time = other.Time
            && this.Production = other.Production
            && this.Consumption = other.Consumption
        | _ -> false

    override this.GetHashCode() =
        HashCode.Combine(this.Time, this.Production, this.Consumption)
