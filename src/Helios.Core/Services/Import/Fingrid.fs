namespace Helios.Core.Services

open Helios.Core.Repository
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Helios.Core.DataProviders.CsvParsers.FingridParser
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Utils
open FSharp.Data
open System

type FingridImport =
    { repositories: Repositories
      client: Fingrid
      config: IConfiguration
      logger: ILogger }

    member this.Import(filePath: string) =
        if not (IO.File.Exists filePath) then
            Error(sprintf "File does not exist: %s" filePath)
        else
            this.client
            |> parseNetEnergyConsumptionFromDatahubCsv (CsvFile.Load(filePath, ";"))
            |> tap (fun _ ->
                this.logger.LogInformation(sprintf "Successfully parsed data from Fingrid CSV file %s" filePath))
            |> List.map (fun reading ->
                new HouseholdEnergyReading(
                    time = reading.Time,
                    consumption = reading.Consumption,
                    production = reading.Production
                ))
            |> tap (fun r ->
                this.logger.LogInformation "Successfully parsed data from Fingrid response"
                r |> List.iter (fun x -> this.logger.LogDebug(sprintf "%A" (x.ToString()))))
            |> this.repositories.HouseholdEnergyReading.Save
            |> Ok

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          client =
            Fingrid.Init
                { Logger = logger
                  SiteIdentifiers =
                    { Consumption = config.GetSection("Fingrid").["SiteIdentifier_Consumption"]
                      Production = config.GetSection("Fingrid").["SiteIdentifier_Production"] } }
          config = config
          logger = logger }
