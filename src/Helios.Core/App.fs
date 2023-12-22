namespace Helios.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Database
open Repository
open Logger
open System


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
    open Helios.Core.Services

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
        { Services: Services }

        static member Init(logLevel: LogLevel, ?dbPath) =
            let logger =
                createLogger (
                    new HeliosLoggerProvider(
                        { LoggerOptions.Default with
                            Level = logLevel }
                    )
                )

            let dbContext = initDbContext (defaultArg dbPath "Helios.sqlite")
            let repos = Repositories.Init(dbContext, logger)
            let config = initConfiguration ()

            { Services = Services.Init(repos, logger, config) }
