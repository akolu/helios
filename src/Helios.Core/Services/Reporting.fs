namespace Helios.Core.Services

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Helios.Core.Repository
open Helios.Core.Utils

type Reporting =
    { repositories: Repositories
      config: IConfiguration
      logger: ILogger }

    member this.generateReport() =
        this.repositories.Reports.SolarEnergySavingsReport
        |> tap (List.iter (fun x -> this.logger.LogDebug(sprintf "%A" x)))

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { repositories = repositories
          config = config
          logger = logger }
