module Helios.Core.Database

open Microsoft.EntityFrameworkCore
open Helios.Core.Models.EnergyMeasurement

type HeliosDatabaseContext(options: DbContextOptions<HeliosDatabaseContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable private energyMeasurements: DbSet<EnergyMeasurement>

    member this.EnergyMeasurements
        with get () = this.energyMeasurements
        and set v = this.energyMeasurements <- v

    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.Entity<EnergyMeasurement>().HasKey("Time", "FlowType") |> ignore
