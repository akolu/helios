module Helios.Core.Database

open Microsoft.EntityFrameworkCore
open Helios.Core.Models.SolarPanelOutput
open Helios.Core.Models.HouseholdEnergyReading
open Helios.Core.Models.ElectricitySpotPrice

type HeliosDatabaseContext(options: DbContextOptions<HeliosDatabaseContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private solarPanelOutputs: DbSet<SolarPanelOutput>

    [<DefaultValue>]
    val mutable private householdEnergyReadings: DbSet<HouseholdEnergyReading>

    [<DefaultValue>]
    val mutable private electricitySpotPrices: DbSet<ElectricitySpotPrice>

    member this.SolarPanelOutputs
        with get () = this.solarPanelOutputs
        and set v = this.solarPanelOutputs <- v

    member this.HouseholdEnergyReadings
        with get () = this.householdEnergyReadings
        and set v = this.householdEnergyReadings <- v

    member this.ElectricitySpotPrices
        with get () = this.electricitySpotPrices
        and set v = this.electricitySpotPrices <- v

    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.Entity<SolarPanelOutput>().HasKey("Time") |> ignore
        modelBuilder.Entity<HouseholdEnergyReading>().HasKey("Time") |> ignore
        modelBuilder.Entity<ElectricitySpotPrice>().HasKey("Time") |> ignore

    new() =
        let optionsBuilder = DbContextOptionsBuilder<HeliosDatabaseContext>()

        optionsBuilder.UseSqlite(
            "Data Source=Helios.sqlite",
            (fun f -> f.MigrationsAssembly("Helios.Migrations") |> ignore)
        )
        |> ignore

        new HeliosDatabaseContext(optionsBuilder.Options)
