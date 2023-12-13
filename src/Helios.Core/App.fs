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
    open System.IO
    open System.Reflection

    type App =
        { Repository: EnergyMeasurementRepository
          Config: IConfiguration
          FusionSolar: FusionSolar.FusionSolar }

        static member Init(?dbPath) =
            let serviceCollection = new ServiceCollection()

            serviceCollection.AddDbContext<HeliosDatabaseContext>(fun options ->
                options.UseSqlite(
                    sprintf "Data Source=%s" (defaultArg dbPath "Helios.sqlite"),
                    fun f -> f.MigrationsAssembly("Helios.Migrations") |> ignore
                )
                |> ignore)
            |> ignore

            // Build the service provider
            let serviceProvider = serviceCollection.BuildServiceProvider()
            let dbContext = serviceProvider.GetService<HeliosDatabaseContext>()

            // run database migrations
            dbContext.Database.Migrate() |> ignore

            let repository = new EnergyMeasurementRepository(dbContext)

            let configuration =
                (new ConfigurationBuilder())
                    .SetBasePath(AppContext.BaseDirectory)
                    // .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
                    .AddUserSecrets<Secrets>() // NOTE: without type parameter, it doesn't work
                    .AddEnvironmentVariables()
                    .Build()

            { Repository = repository
              Config = configuration
              FusionSolar =
                FusionSolar.init
                    { httpClient = new HttpHandler()
                      logger = ConsoleLogger()
                      userName = configuration.GetSection("FusionSolar").["UserName"]
                      systemCode = configuration.GetSection("FusionSolar").["Password"] } }

    let importFusionSolar (date: DateTimeOffset) (app: App) =
        app.FusionSolar
        |> tap (fun _ -> printfn "Successfully initialized FusionSolar client, getting data from date %A" date)
        |> FusionSolar.getHourlyData
            { stationCodes = app.Config.GetSection("FusionSolar").["StationCode"]
              collectTime = date.ToUnixTimeMilliseconds() }
        |> tap (fun _ -> printfn "Successfully got data from date %A" date)
        |> unwrap
        |> EnergyMeasurement.fromFusionSolarResponse
        |> tap (fun r ->
            printfn "Successfully parsed data from FusionSolar response:"
            r |> List.iter (fun x -> printfn "%A" (x.ToString())))
        |> app.Repository.Save
        |> tap (fun _ -> printfn "Successfully saved data to database")
