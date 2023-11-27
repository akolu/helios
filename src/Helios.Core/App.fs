namespace Helios.Core

open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore
open Database
open System

module Main =

    type App =
        { ServiceProvider: IServiceProvider }

        static member Init config =
            // Create a new service collection
            let serviceCollection = new ServiceCollection()

            // Add the DbContext to the service collection
            serviceCollection.AddDbContext<HeliosDatabaseContext>(fun options ->
                options.UseSqlite("Data Source=helios.db") |> ignore)
            |> ignore

            // Build the service provider
            let serviceProvider = serviceCollection.BuildServiceProvider()

            { ServiceProvider = serviceProvider }

    let import (csv: string) (app: App) = printfn "Importing %s... (stub)" csv

    let generateReport (startDate, endDate) (app: App) =
        printfn "Generating report from %s to %s... (stub)" startDate endDate
