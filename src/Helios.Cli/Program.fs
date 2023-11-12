open Helios

let app =
    new App(
        { userName = "test"
          systemCode = "test" }
    )

app.hello "world"
