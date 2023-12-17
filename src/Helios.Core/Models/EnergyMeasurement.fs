module Helios.Core.Models.EnergyMeasurement

open Helios.Core.Services.FusionSolar.Types

open System

type FlowType =
    | Production = 0
    | Consumption = 1

type EnergyMeasurement(time: DateTimeOffset, flowType: FlowType, kwh: double) =
    member val Time = time with get, set

    member val FlowType = flowType with get, set

    member val Kwh = kwh with get, set

    override this.ToString() =
        sprintf "EnergyMeasurement(%A, %A, %A)" this.Time this.FlowType this.Kwh

    override this.Equals(other) =
        match other with
        | :? EnergyMeasurement as other ->
            this.Time = other.Time && this.FlowType = other.FlowType && this.Kwh = other.Kwh
        | _ -> false

    override this.GetHashCode() =
        HashCode.Combine(this.Time, this.FlowType, this.Kwh)

    static member fromFusionSolarResponse(responseBody: GetHourlyData.ResponseBody) : EnergyMeasurement list =
        responseBody.data
        |> List.map (fun data ->
            new EnergyMeasurement(
                time = DateTimeOffset.FromUnixTimeMilliseconds(data.collectTime),
                flowType = FlowType.Production,
                kwh =
                    match data.dataItemMap.inverter_power with
                    | Some kwh -> kwh
                    | None -> 0.0
            ))

    static member fromFingridNetEnergyConsumption
        (measurements: list<DateTimeOffset * double>)
        : EnergyMeasurement list =
        measurements
        |> List.map (fun (time, kwh) -> new EnergyMeasurement(time = time, flowType = FlowType.Consumption, kwh = kwh))
