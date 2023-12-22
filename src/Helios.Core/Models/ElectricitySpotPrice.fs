module Helios.Core.Models.ElectricitySpotPrice

open Helios.Core.DataProviders.ApiClients.EntsoEClient.Types
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

    interface ITimeSeries with
        member this.Time = this.Time

let private validateTimeSeries (data: TimeSeriesPeriod) (prices: ElectricitySpotPrice list) =
    let hoursDifference =
        (data.TimeInterval.EndDt - data.TimeInterval.StartDt).TotalHours

    if float (List.length prices) <> hoursDifference then
        failwith (
            sprintf
                "Inconsistent time series in time interval %A: number of points does not match time interval. Expected: %s, actual: %s"
                data.TimeInterval
                (string hoursDifference)
                (string (List.length prices))
        )

let private eurPerMWhToEuroCentsPerKWh (eurPerMWh: decimal) = eurPerMWh * 100m / 1000m

let fromEntsoETransmissionDayAheadPricesResponse (timeSeries: TimeSeriesPeriod list) : ElectricitySpotPrice list =
    timeSeries
    |> List.map (fun data ->
        data.Points
        |> List.map (fun point ->
            new ElectricitySpotPrice(
                time = data.TimeInterval.StartDt.AddHours(double (point.Position - 1)),
                euroCentsPerKWh = eurPerMWhToEuroCentsPerKWh point.PriceAmount
            ))
        |> tap (validateTimeSeries data))
    |> List.fold (fun acc x -> acc @ x) []
