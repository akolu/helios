module Helios.Core.Models.SolarPanelOutput

open Helios.Core.Services.FusionSolar.Types

open System

type SolarPanelOutput(time: DateTimeOffset, kwh: double) =
    member val Time = time with get, set

    member val Kwh = kwh with get, set

    override this.ToString() =
        sprintf "SolarPanelOutput(%A, %A)" this.Time this.Kwh

    override this.Equals(other) =
        match other with
        | :? SolarPanelOutput as other -> this.Time = other.Time && this.Kwh = other.Kwh
        | _ -> false

    override this.GetHashCode() = HashCode.Combine(this.Time, this.Kwh)

    static member fromFusionSolarResponse(responseBody: GetHourlyData.ResponseBody) : SolarPanelOutput list =
        responseBody.data
        |> List.map (fun data ->
            new SolarPanelOutput(
                time = DateTimeOffset.FromUnixTimeMilliseconds(data.collectTime),
                kwh =
                    match data.dataItemMap.inverter_power with
                    | Some kwh -> kwh
                    | None -> 0.0
            ))
