namespace Helios.Core

module Main =
    type App =
        { userName: string
          systemCode: string }

        static member Init config =
            { userName = config.userName
              systemCode = config.systemCode }

    let import (csv: string) (app: App) = printfn "Importing %s... (stub)" csv

    let generateReport (startDate, endDate) (app: App) =
        printfn "Generating report from %s to %s... (stub)" startDate endDate
