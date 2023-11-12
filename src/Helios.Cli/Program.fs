open Helios.Core.Main

let config =
    { userName = "user"
      systemCode = "code" }

let csv = "data.csv"

let fromDate = "2020-01-01"
let toDate = "2020-01-31"

App.Init config |> import csv

App.Init config |> generateReport (fromDate, toDate)
