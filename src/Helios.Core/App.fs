namespace Helios

type Config =
    { userName: string; systemCode: string }

type App(config: Config) =

    member private this.fusionSolarApiToken = None

    member this.hello name = printfn "Hello %s" name
