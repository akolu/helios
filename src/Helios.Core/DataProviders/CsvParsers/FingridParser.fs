module Helios.Core.DataProviders.CsvParsers.FingridParser

open FSharp.Data
open System
open Helios.Core.Utils
open Microsoft.Extensions.Logging

type GrossEnergyConsumption =
    { SiteIdentifier: string
      Time: DateTimeOffset
      KWh: double }

type SiteType =
    { Consumption: string
      Production: string }

type FingridReading =
    { Time: DateTimeOffset
      Consumption: double
      Production: double }

type Config =
    { Logger: ILogger
      SiteIdentifiers: SiteType }

type Fingrid =
    { Config: Config }

    static member Init(config: Config) = { Config = config }

let private parseEnergyConsumptionRow (row: CsvRow) =
    { SiteIdentifier = row.GetColumn("Mittauspisteen tunnus")
      Time = DateTimeOffset.Parse(row.GetColumn("Alkuaika"))
      KWh = (System.Double.Parse(row.GetColumn("Määrä"), Globalization.CultureInfo("fi-FI"))) }

let private getTotalConsumption (siteIdentifiers: SiteType) (time: DateTimeOffset, rows: seq<GrossEnergyConsumption>) =
    let getTotal siteType =
        rows
        |> Seq.filter (fun row -> row.SiteIdentifier = siteType)
        |> Seq.sumBy (fun row -> row.KWh)

    { Time = time
      Consumption = getTotal siteIdentifiers.Consumption
      Production = getTotal siteIdentifiers.Production }

let parseNetEnergyConsumptionFromDatahubCsv (csv: CsvFile) (this: Fingrid) =
    csv.Rows
    |> Seq.map parseEnergyConsumptionRow
    |> Seq.groupBy (fun row -> row.Time)
    |> Seq.map (getTotalConsumption this.Config.SiteIdentifiers)
    |> Seq.toList
