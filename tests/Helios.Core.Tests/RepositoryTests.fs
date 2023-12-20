module Helios.Tests.RepositoryTests

open Xunit
open Helios.Core.Database
open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Repository
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open System
open Helios.Core.Logger

type RepositoryTests() =
    let mutable dbContext: Option<HeliosDatabaseContext> = None
    let mutable repository: Option<SolarPanelOutputRepository> = None

    let mockLogger = createLogger (new HeliosLoggerProvider(LoggerOptions.None))

    let serviceProvider =
        ServiceCollection()
            .AddDbContext<HeliosDatabaseContext>(fun options ->
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()) |> ignore)
            .BuildServiceProvider()

    do
        dbContext <- Some(serviceProvider.GetService<HeliosDatabaseContext>())
        repository <- Some(new SolarPanelOutputRepository(dbContext.Value, mockLogger))

    interface IDisposable with
        member _.Dispose() =
            match dbContext with
            | Some dbContext -> dbContext.Dispose()
            | None -> ()

            dbContext <- None
            repository <- None

    [<Fact>]
    member _.``New SolarPanelOutputs can be added to the database``() =
        // Arrange
        let repository: SolarPanelOutputRepository =
            new SolarPanelOutputRepository(dbContext.Value, mockLogger)

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
        let repository: SolarPanelOutputRepository =
            new SolarPanelOutputRepository(dbContext.Value, mockLogger)

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
        let repository: SolarPanelOutputRepository =
            new SolarPanelOutputRepository(dbContext.Value, mockLogger)

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
