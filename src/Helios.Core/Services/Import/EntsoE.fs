namespace Helios.Core.Services

open Helios.Core.Repository
open Helios.Core.DataProviders.ApiClients.EntsoEClient
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open System
open Helios.Core.Utils
open Helios.Core.HttpHandler
open Helios.Core.Models

type EntsoEImport =
    { repositories: Repositories
      client: EntsoE
      config: IConfiguration
      logger: ILogger }

    member this.Import(fromDate: DateTimeOffset, toDate: DateTimeOffset) =
        this.client
        |> tap (fun _ ->
            this.logger.LogInformation(sprintf "Importing ENTSO-E data from date %A to %A" fromDate toDate))
        |> getDayAheadPrices (fromDate, toDate)
        |> tap (fun _ ->
            this.logger.LogInformation(sprintf "Successfully got ENTSO-E data from %A to %A" fromDate toDate))
        |> unwrap
        |> ElectricitySpotPrice.fromEntsoETransmissionDayAheadPricesResponse
        |> List.filter (fun x -> x.Time >= fromDate && x.Time <= toDate)
        |> tap (fun r ->
            this.logger.LogInformation "Successfully parsed data from ENTSO-E response"
            r |> List.iter (fun x -> this.logger.LogDebug(sprintf "%A" (x.ToString()))))
        |> this.repositories.ElectricitySpotPrice.Save
        |> tap (fun _ -> this.logger.LogInformation "Successfully saved data to database")

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          client =
            EntsoE.Init
                { HttpClient = HttpHandler()
                  Logger = logger
                  SecurityToken = config.GetSection("EntsoE").["SecurityToken"] }
          config = config
          logger = logger }
