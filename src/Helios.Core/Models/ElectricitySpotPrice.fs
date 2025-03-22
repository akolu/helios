module Helios.Core.Models.ElectricitySpotPrice

open Helios.Core.DataProviders.ApiClients.EntsoEClient.Types
open System
open Microsoft.Extensions.Logging

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

let private validateDataIntegrity (logger: ILogger) (data: TimeSeriesPeriod) =
    let expectedHours =
        int (data.TimeInterval.EndDt - data.TimeInterval.StartDt).TotalHours

    if expectedHours = List.length data.Points then
        data
    else
        let points: TimeSeriesPoint list =
            [ 1..expectedHours ]
            |> List.fold
                (fun acc pos ->
                    match List.tryFind (fun p -> p.Position = pos) data.Points with
                    | Some point -> acc @ [ point ]
                    | None ->
                        let latest = List.last acc

                        logger.LogWarning(
                            "Missing data for {Position}. Using last available value {LastAvailableValue}.",
                            data.TimeInterval.StartDt
                                .AddHours(double (pos - 1))
                                .ToString("dd.MM.yyyy HH:mm:ss"),
                            latest.PriceAmount
                        )

                        acc @ [ { latest with Position = pos } ])
                []

        { data with Points = points }

let private eurPerMWhToEuroCentsPerKWh (eurPerMWh: decimal) = eurPerMWh * 100m / 1000m

let fromEntsoETransmissionDayAheadPricesResponse
    (logger: ILogger)
    (timeSeries: TimeSeriesPeriod list)
    : ElectricitySpotPrice list =
    timeSeries
    |> List.map (validateDataIntegrity logger)
    |> List.map (fun data ->
        data.Points
        |> List.map (fun point ->
            new ElectricitySpotPrice(
                time = data.TimeInterval.StartDt.AddHours(double (point.Position - 1)),
                euroCentsPerKWh = eurPerMWhToEuroCentsPerKWh point.PriceAmount
            )))
    |> List.fold (fun acc x -> acc @ x) []
