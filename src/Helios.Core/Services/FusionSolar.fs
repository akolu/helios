namespace Helios.Core.Services

module private Constants =
    let XSRF_TOKEN_COOKIE_KEY = "XSRF-TOKEN"

module private EndpointUrls =
    let baseUrl = "https://eu5.fusionsolar.huawei.com/thirdData"
    let login = baseUrl + "/login"

module Types =
    type State = { xsrfToken: string }

    module Login =
        type RequestBody =
            { userName: string; systemCode: string }

        type ResponseBody =
            { success: bool
              data: obj
              failCode: int
              params: {| currentTime: int64 |}
              message: string }

type FusionSolar =
    { Client: IHttpClient
      State: Types.State }

    member this.Login(body: Types.Login.RequestBody) =
        match
            this.Client.Post EndpointUrls.login body
            |> HttpClient.ParseCookie Constants.XSRF_TOKEN_COOKIE_KEY
        with
        | Some xsrfToken ->
            Ok
                { this with
                    State = { xsrfToken = xsrfToken } }
        | None -> Error "Could not log in"

    static member Init(httpClient, ?state: Types.State) =
        { Client = httpClient
          State = defaultArg state { xsrfToken = "" } }
