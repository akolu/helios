module Helios.Tests.RepositoryTests

open Xunit
open Helios.Core.Database
open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Repository
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open System

type RepositoryTests() =
    let mutable dbContext: Option<HeliosDatabaseContext> = None
    let mutable repository: Option<EnergyMeasurementRepository> = None

    let serviceProvider =
        ServiceCollection()
            .AddDbContext<HeliosDatabaseContext>(fun options ->
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()) |> ignore)
            .BuildServiceProvider()

    do
        dbContext <- Some(serviceProvider.GetService<HeliosDatabaseContext>())
        repository <- Some(new EnergyMeasurementRepository(dbContext.Value))

    interface IDisposable with
        member _.Dispose() =
            match dbContext with
            | Some dbContext -> dbContext.Dispose()
            | None -> ()

            dbContext <- None
            repository <- None

    [<Fact>]
    member _.``New EnergyMeasurements can be added to the database``() =
        // Arrange
        let repository: EnergyMeasurementRepository =
            new EnergyMeasurementRepository(dbContext.Value)

        let measurements =
            [ EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-01"), flowType = FlowType.Production, kwh = 100.0)
              EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-02"), flowType = FlowType.Consumption, kwh = 200.0) ]

        // Act
        repository.Save(measurements)

        // Assert
        let allMeasurements =
            dbContext.Value.EnergyMeasurements.ToListAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal(measurements, allMeasurements)

    [<Fact>]
    member _.``Duplicate EnergyMeasurements will be ignored``() =
        // Arrange
        let repository: EnergyMeasurementRepository =
            new EnergyMeasurementRepository(dbContext.Value)

        let measurements =
            [ EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-01"), flowType = FlowType.Production, kwh = 100.0)
              EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-02"), flowType = FlowType.Consumption, kwh = 200.0) ]

        // Act
        repository.Save(measurements)
        repository.Save(measurements) // should be ignored

        // Assert
        let allMeasurements =
            dbContext.Value.EnergyMeasurements.ToListAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal([| measurements.[0]; measurements.[1] |], allMeasurements)

    [<Fact>]
    member _.``EnergyMeasurements can be read from the database, filtered by date & flowType``() =
        // Arrange
        let repository: EnergyMeasurementRepository =
            new EnergyMeasurementRepository(dbContext.Value)

        let measurements =
            [ EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-01"), flowType = FlowType.Production, kwh = 10.0)
              EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-01"), flowType = FlowType.Consumption, kwh = 100.0)
              EnergyMeasurement(time = DateTimeOffset.Parse("2020-01-02"), flowType = FlowType.Consumption, kwh = 200.0) ]

        dbContext.Value.EnergyMeasurements.AddRange(measurements) |> ignore
        dbContext.Value.SaveChanges() |> ignore

        // Act
        let result1 =
            repository.Find(
                DateTimeOffset.Parse("2020-01-01"),
                DateTimeOffset.Parse("2020-01-01"),
                FlowType.Consumption
            )

        let result2 =
            repository.Find(
                DateTimeOffset.Parse("2020-01-01"),
                DateTimeOffset.Parse("2020-02-01"),
                FlowType.Consumption
            )

        let result3 =
            repository.Find(DateTimeOffset.Parse("2020-01-01"), DateTimeOffset.Parse("2022-01-01"), FlowType.Production)

        let noResults =
            repository.Find(DateTimeOffset.Parse("2020-02-01"), DateTimeOffset.Parse("2022-03-01"), FlowType.Production)

        // Assert
        Assert.Equal([| measurements.[1] |], result1)
        Assert.Equal([| measurements.[1]; measurements.[2] |], result2)
        Assert.Equal([| measurements.[0] |], result3)
        Assert.Equal([||], noResults)
