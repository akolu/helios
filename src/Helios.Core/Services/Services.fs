namespace Helios.Core.Services

open Microsoft.Extensions.Logging
open Helios.Core.Repository
open Microsoft.Extensions.Configuration

type Services =
    { FusionSolar: FusionSolarImport
      EntsoE: EntsoEImport
      Fingrid: FingridImport
      Reporting: Reporting }

    static member Init(repositories: Repositories, logger: ILogger, config: IConfiguration) =
        { FusionSolar = FusionSolarImport.Init(repositories, logger, config)
          EntsoE = EntsoEImport.Init(repositories, logger, config)
          Fingrid = FingridImport.Init(repositories, logger, config)
          Reporting = Reporting.Init(repositories, logger, config) }
