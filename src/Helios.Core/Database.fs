module Helios.Core.Database

open Microsoft.EntityFrameworkCore
open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Models.ElectricitySpotPrice

type HeliosDatabaseContext(options: DbContextOptions<HeliosDatabaseContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private energyMeasurements: DbSet<EnergyMeasurement>

    [<DefaultValue>]
    val mutable private electricitySpotPrices: DbSet<ElectricitySpotPrice>

    member this.EnergyMeasurements
        with get () = this.energyMeasurements
        and set v = this.energyMeasurements <- v

    member this.ElectricitySpotPrices
        with get () = this.electricitySpotPrices
        and set v = this.electricitySpotPrices <- v

    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.Entity<EnergyMeasurement>().HasKey("Time", "FlowType") |> ignore
        modelBuilder.Entity<ElectricitySpotPrice>().HasKey("Time") |> ignore

    new() =
        let optionsBuilder = DbContextOptionsBuilder<HeliosDatabaseContext>()

        optionsBuilder.UseSqlite(
            "Data Source=Helios.sqlite",
            (fun f -> f.MigrationsAssembly("Helios.Migrations") |> ignore)
        )
        |> ignore

        new HeliosDatabaseContext(optionsBuilder.Options)
