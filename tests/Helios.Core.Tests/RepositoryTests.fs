module Helios.Tests.RepositoryTests

open Xunit
open Helios.Core.Database
open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Repository
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open System
open Helios.Core.Logger
open Helios.Core.Models.ElectricitySpotPrice
open Helios.Core.Models.HouseholdEnergyReading

type RepositoryTests() =
    let mutable dbContext: Option<HeliosDatabaseContext> = None
    let mutable repositories: Option<Repositories> = None

    let mockLogger = createLogger (new HeliosLoggerProvider(LoggerOptions.None))

    let serviceProvider =
        ServiceCollection()
            .AddDbContext<HeliosDatabaseContext>(fun options ->
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()) |> ignore)
            .BuildServiceProvider()

    do
        dbContext <- Some(serviceProvider.GetService<HeliosDatabaseContext>())
        repositories <- Some(Repositories.Init(dbContext.Value, mockLogger))

    interface IDisposable with
        member _.Dispose() =
            match dbContext with
            | Some dbContext -> dbContext.Dispose()
            | None -> ()

            dbContext <- None
            repositories <- None

    [<Fact>]
    member _.``New SolarPanelOutputs can be added to the database``() =
        // Arrange
        let repository = repositories.Value.SolarPanelOutput

        let measurements =
            [ SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-01"), kwh = 100.0)
              SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-02"), kwh = 200.0) ]

        // Act
        repository.Save(measurements)

        // Assert
        let allMeasurements =
            dbContext.Value.SolarPanelOutputs.ToListAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal(measurements, allMeasurements)

    [<Fact>]
    member _.``Duplicate SolarPanelOutputs will be ignored``() =
        // Arrange
        let repository = repositories.Value.SolarPanelOutput

        let measurements =
            [ SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-01"), kwh = 100.0)
              SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-02"), kwh = 200.0) ]

        // Act
        repository.Save(measurements)
        repository.Save(measurements) // should be ignored

        // Assert
        let allMeasurements =
            dbContext.Value.SolarPanelOutputs.ToListAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal([| measurements.[0]; measurements.[1] |], allMeasurements)

    [<Fact>]
    member _.``SolarPanelOutputs can be read from the database, filtered by date``() =
        // Arrange
        let repository = repositories.Value.SolarPanelOutput

        let measurements =
            [ SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-01"), kwh = 10.0)
              SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-02"), kwh = 200.0) ]

        dbContext.Value.SolarPanelOutputs.AddRange(measurements) |> ignore
        dbContext.Value.SaveChanges() |> ignore

        // Act
        let result1 =
            repository.Find(DateTimeOffset.Parse("2020-01-01"), DateTimeOffset.Parse("2020-01-01"))

        let result2 =
            repository.Find(DateTimeOffset.Parse("2020-01-01"), DateTimeOffset.Parse("2020-02-01"))

        let noResults =
            repository.Find(DateTimeOffset.Parse("2020-02-01"), DateTimeOffset.Parse("2022-03-01"))

        // Assert
        Assert.Equal([| measurements.[0] |], result1)
        Assert.Equal([| measurements.[0]; measurements.[1] |], result2)
        Assert.Equal([||], noResults)

    [<Fact>]
    member _.``GetTimeSeriesData returns data that exists in all three tables``() =
        dbContext.Value.SolarPanelOutputs.AddRange(
            [ SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-01"), kwh = 10.0)
              SolarPanelOutput(time = DateTimeOffset.Parse("2020-01-02"), kwh = 20.0) ]
        )
        |> ignore

        dbContext.Value.ElectricitySpotPrices.AddRange(
            [ ElectricitySpotPrice(time = DateTimeOffset.Parse("2020-01-02"), euroCentsPerKWh = 0.2m)
              ElectricitySpotPrice(time = DateTimeOffset.Parse("2020-01-03"), euroCentsPerKWh = 0.3m)
              ElectricitySpotPrice(time = DateTimeOffset.Parse("2020-01-05"), euroCentsPerKWh = 0.5m) ]
        )
        |> ignore

        dbContext.Value.HouseholdEnergyReadings.AddRange(
            [ HouseholdEnergyReading(time = DateTimeOffset.Parse("2020-01-01"), consumption = 100.0, production = 11.0)
              HouseholdEnergyReading(time = DateTimeOffset.Parse("2020-01-02"), consumption = 200.0, production = 22.0)
              HouseholdEnergyReading(time = DateTimeOffset.Parse("2020-01-03"), consumption = 300.0, production = 33.0)
              HouseholdEnergyReading(time = DateTimeOffset.Parse("2020-01-04"), consumption = 400.0, production = 44.0) ]
        )
        |> ignore

        dbContext.Value.SaveChanges() |> ignore

        let result =
            repositories.Value.Reports.GetEnergySavingsData(DateTime.Parse("2020-01-02"), DateTime.Parse("2020-01-05"))

        Assert.Equal<EnergySavingsData list>(
            [ { Time = DateTimeOffset.Parse("2020-01-02")
                KwhOutput = 20.0
                Consumption = 200.0
                Production = 22.0
                Price = 0.2m } ],
            result
        )
