namespace Helios.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Database
open Helios.Core.Services
open Helios.Core.Models
open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Utils
open Repository
open Logger
open System
open FSharp.Data


type Secrets =
    { FusionSolar:
        {| Username: string
           Password: string
           StationCode: string |}
      EntsoE: {| SecurityToken: string |}
      Fingrid:
          {| SiteIdentifier_Consumption: string
             SiteIdentifier_Production: string |} }

module Main =
    let private initDbContext (dbPath: string) =
        let serviceCollection = new ServiceCollection()

        serviceCollection.AddDbContext<HeliosDatabaseContext>(fun options ->
            options.UseSqlite(
                sprintf "Data Source=%s" dbPath,
                fun f -> f.MigrationsAssembly("Helios.Migrations") |> ignore
            )
            |> ignore)
        |> ignore

        // Build the service provider
        let serviceProvider = serviceCollection.BuildServiceProvider()
        let dbContext = serviceProvider.GetService<HeliosDatabaseContext>()

        // run database migrations
        dbContext.Database.Migrate() |> ignore

        dbContext

    let private initConfiguration () =
        (new ConfigurationBuilder())
            .SetBasePath(AppContext.BaseDirectory)
            // .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .AddUserSecrets<Secrets>() // NOTE: without type parameter, it doesn't work
            .AddEnvironmentVariables()
            .Build()

    type App =
        { Repositories: Repositories
          Config: IConfiguration
          Logger: ILogger
          FusionSolar: FusionSolar.FusionSolar
          EntsoE: EntsoE.EntsoE
          Fingrid: Fingrid.Fingrid }

        static member Init(?dbPath) =
            let dbContext = initDbContext (defaultArg dbPath "Helios.sqlite")
            let configuration = initConfiguration ()
            let httpHandler = HttpHandler()
            let logger = createLogger (new HeliosLoggerProvider())

            { Repositories = Repositories.Init(dbContext, logger)
              Config = configuration
              Logger = logger
              FusionSolar =
                FusionSolar.init
                    { HttpClient = httpHandler
                      Logger = logger
                      UserName = configuration.GetSection("FusionSolar").["UserName"]
                      SystemCode = configuration.GetSection("FusionSolar").["Password"] }
              EntsoE =
                EntsoE.init
                    { HttpClient = httpHandler
                      Logger = logger
                      SecurityToken = configuration.GetSection("EntsoE").["SecurityToken"] }
              Fingrid =
                Fingrid.init
                    { Logger = logger
                      SiteIdentifiers =
                        { Consumption = configuration.GetSection("Fingrid").["SiteIdentifier_Consumption"]
                          Production = configuration.GetSection("Fingrid").["SiteIdentifier_Production"] } } }

    let importFusionSolar (date: DateTimeOffset) (app: App) =
        app.FusionSolar
        |> tap (fun _ ->
            app.Logger.LogInformation(
                sprintf "Successfully initialized FusionSolar client, getting data from date %A" date
            ))
        |> FusionSolar.getHourlyData
            { stationCodes = app.Config.GetSection("FusionSolar").["StationCode"]
              collectTime = date.ToUnixTimeMilliseconds() }
        |> tap (fun _ -> app.Logger.LogInformation(sprintf "Successfully got FusionSolar data from date %A" date))
        |> unwrap
        |> SolarPanelOutput.fromFusionSolarResponse
        |> tap (fun r ->
            app.Logger.LogInformation "Successfully parsed data from FusionSolar response"
            r |> List.iter (fun x -> app.Logger.LogDebug(sprintf "%A" (x.ToString()))))
        |> app.Repositories.SolarPanelOutput.Save
        |> tap (fun _ -> app.Logger.LogInformation "Successfully saved data to database")

    let importEntsoE (fromDate: DateTimeOffset, toDate: DateTimeOffset) (app: App) =
        app.EntsoE
        |> tap (fun _ ->
            app.Logger.LogInformation(
                sprintf "Successfully initialized ENTSO-E client, getting data from date %A to %A" fromDate toDate
            ))
        |> EntsoE.getDayAheadPrices (fromDate, toDate)
        |> tap (fun _ ->
            app.Logger.LogInformation(sprintf "Successfully got ENTSO-E data from %A to %A" fromDate toDate))
        |> unwrap
        |> ElectricitySpotPrice.fromEntsoETransmissionDayAheadPricesResponse
        |> List.filter (fun x -> x.Time >= fromDate && x.Time <= toDate)
        |> tap (fun r ->
            app.Logger.LogInformation "Successfully parsed data from ENTSO-E response"
            r |> List.iter (fun x -> app.Logger.LogDebug(sprintf "%A" (x.ToString()))))
        |> app.Repositories.ElectricitySpotPrice.Save
        |> tap (fun _ -> app.Logger.LogInformation "Successfully saved data to database")

    let importFingrid (filePath: string) (app: App) =
        if not (IO.File.Exists filePath) then
            Error(sprintf "File does not exist: %s" filePath)
        else
            app.Fingrid
            |> Fingrid.parseNetEnergyConsumptionFromDatahubCsv (CsvFile.Load(filePath, ";"))
            |> tap (fun _ ->
                app.Logger.LogInformation(sprintf "Successfully parsed data from Fingrid CSV file %s" filePath))
            |> List.map (fun reading ->
                new HouseholdEnergyReading(
                    time = reading.Time,
                    consumption = reading.Consumption,
                    production = reading.Production
                ))
            |> tap (fun r ->
                app.Logger.LogInformation "Successfully parsed data from Fingrid response"
                r |> List.iter (fun x -> app.Logger.LogDebug(sprintf "%A" (x.ToString()))))
            |> app.Repositories.HouseholdEnergyReading.Save
            |> Ok

    let generateReport (app: App) =
        app.Repositories.Reports.SolarEnergySavingsReport
        |> tap (List.iter (fun x -> app.Logger.LogDebug(sprintf "%A" x)))
