module Helios.Core.Models.ElectricitySpotPrice

open Helios.Core.Services.EntsoE.Types
open System
open Helios.Core.Utils

type ElectricitySpotPrice(time: DateTimeOffset, euroCentsPerKWh: decimal) =
    member val Time = time with get, set

    member val EuroCentsPerKWh = euroCentsPerKWh with get, set

    override this.ToString() =
        sprintf "ElectricitySpotPrice(%A, %A)" this.Time this.EuroCentsPerKWh

    override this.Equals(other) =
        match other with
        | :? ElectricitySpotPrice as other -> this.Time = other.Time && this.EuroCentsPerKWh = other.EuroCentsPerKWh
        | _ -> false

    override this.GetHashCode() =
        HashCode.Combine(this.Time, this.EuroCentsPerKWh)

let private validateTimeSeries (data: TimeSeriesPeriod) (prices: ElectricitySpotPrice list) =
    let hoursDifference =
        (data.timeInterval.endDt - data.timeInterval.startDt).TotalHours

    if float (List.length prices) <> hoursDifference then
        failwith (
            sprintf
                "Inconsistent time series in time interval %A: number of points does not match time interval. Expected: %s, actual: %s"
                data.timeInterval
                (string hoursDifference)
                (string (List.length prices))
        )

let private eurPerMWhToEuroCentsPerKWh (eurPerMWh: decimal) = eurPerMWh * 100m / 1000m

let fromEntsoETransmissionDayAheadPricesResponse (timeSeries: TimeSeriesPeriod list) : ElectricitySpotPrice list =
    timeSeries
    |> List.map (fun data ->
        data.points
        |> List.map (fun point ->
            new ElectricitySpotPrice(
                time = data.timeInterval.startDt.AddHours(double (point.position - 1)),
                euroCentsPerKWh = eurPerMWhToEuroCentsPerKWh point.priceAmount
            ))
        |> tap (validateTimeSeries data))
    |> List.fold (fun acc x -> acc @ x) []
