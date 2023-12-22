namespace Helios.Core.Services

open System
open Microsoft.Extensions.Logging
open Helios.Core.Repository
open Helios.Core.DataProviders.ApiClients.FusionSolarClient
open Microsoft.Extensions.Configuration
open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Utils
open Helios.Core.HttpHandler

type FusionSolarImport =
    { repositories: Repositories
      client: FusionSolar
      config: IConfiguration
      logger: ILogger }

    member this.Import(date: DateTimeOffset) =
        this.client
        |> tap (fun _ -> this.logger.LogInformation(sprintf "Importing FusionSolar data from date %A" date))
        |> getHourlyData
            { stationCodes = this.config.GetSection("FusionSolar").["StationCode"]
              collectTime = date.ToUnixTimeMilliseconds() }
        |> tap (fun _ -> this.logger.LogInformation(sprintf "Successfully got FusionSolar data from date %A" date))
        |> unwrap
        |> SolarPanelOutput.fromFusionSolarResponse
        |> tap (fun r ->
            this.logger.LogInformation "Successfully parsed data from FusionSolar response"
            r |> List.iter (fun x -> this.logger.LogDebug(sprintf "%A" (x.ToString()))))
        |> this.repositories.SolarPanelOutput.Save
        |> tap (fun _ -> this.logger.LogInformation "Successfully saved data to database")

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          client =
            FusionSolar.Init
                { HttpClient = HttpHandler()
                  Logger = logger
                  UserName = config.GetSection("FusionSolar").["UserName"]
                  SystemCode = config.GetSection("FusionSolar").["Password"] }
          config = config
          logger = logger }
