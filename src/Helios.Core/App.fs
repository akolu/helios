namespace Helios.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open Database
open Helios.Core.Services
open Helios.Core.Models
open Helios.Core.Utils
open Repository
open Logger
open System

type Secrets =
    { FusionSolar:
        {| Username: string
           Password: string
           StationCode: string |} }

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
          FusionSolar: FusionSolar.FusionSolar
          EntsoE: EntsoE.EntsoE }

        static member Init(?dbPath) =
            let dbContext = initDbContext (defaultArg dbPath "Helios.sqlite")
            let configuration = initConfiguration ()
            let httpHandler = HttpHandler()
            let logger = ConsoleLogger()

            { Repositories = Repositories.Init(dbContext)
              Config = configuration
              FusionSolar =
                FusionSolar.init
                    { httpClient = httpHandler
                      logger = logger
                      userName = configuration.GetSection("FusionSolar").["UserName"]
                      systemCode = configuration.GetSection("FusionSolar").["Password"] }
              EntsoE =
                EntsoE.init
                    { httpClient = httpHandler
                      logger = logger
                      securityToken = configuration.GetSection("EntsoE").["SecurityToken"] } }

    let importFusionSolar (date: DateTimeOffset) (app: App) =
        app.FusionSolar
        |> tap (fun _ -> printfn "Successfully initialized FusionSolar client, getting data from date %A" date)
        |> FusionSolar.getHourlyData
            { stationCodes = app.Config.GetSection("FusionSolar").["StationCode"]
              collectTime = date.ToUnixTimeMilliseconds() }
        |> tap (fun _ -> printfn "Successfully got FusionSolar data from date %A" date)
        |> unwrap
        |> EnergyMeasurement.fromFusionSolarResponse
        |> tap (fun r ->
            printfn "Successfully parsed data from FusionSolar response:"
            r |> List.iter (fun x -> printfn "%A" (x.ToString())))
        |> app.Repositories.EnergyMeasurement.Save
        |> tap (fun _ -> printfn "Successfully saved data to database")

    let importEntsoE (fromDate: DateTimeOffset, toDate: DateTimeOffset) (app: App) =
        app.EntsoE
        |> tap (fun _ ->
            printfn "Successfully initialized ENTSO-E client, getting data from date %A to %A" fromDate toDate)
        |> EntsoE.getDayAheadPrices (fromDate, toDate)
        |> tap (fun _ -> printfn "Successfully got ENTSO-E data from %A to %A" fromDate toDate)
        |> unwrap
        |> ElectricitySpotPrice.fromEntsoETransmissionDayAheadPricesResponse
        |> List.filter (fun x -> x.Time >= fromDate && x.Time <= toDate)
        |> tap (fun r ->
            printfn "Successfully parsed data from ENTSO-E response:"
            r |> List.iter (fun x -> printfn "%A" (x.ToString())))
        |> app.Repositories.ElectricitySpotPrice.Save
        |> tap (fun _ -> printfn "Successfully saved data to database")
