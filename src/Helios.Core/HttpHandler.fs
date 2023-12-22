module Helios.Core.HttpHandler

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Web
open System.Xml.Linq
open System.Threading.Tasks

module HttpUtils =
    let toJsonStringContent (data: obj) =
        new StringContent(Json.JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")

    let parseJsonBody<'T> (response: HttpResponseMessage) =

        match response.Content.ReadAsStringAsync().Result with
        | null -> Error "Response body is null"
        | json ->
            try
                let options = JsonSerializerOptions()
                options.AllowTrailingCommas <- true
                Ok(Json.JsonSerializer.Deserialize<'T>(json, options))
            with :? JsonException as ex ->
                Error ex.Message

    let parseCookie (key: string) (response: HttpResponseMessage) : Result<string, string> =
        let mutable values: seq<string> = Seq.empty

        if response.Headers.TryGetValues("Set-Cookie", &values) then
            match
                values
                |> Seq.map (fun x -> x.Split(';').[0].Split('='))
                |> Seq.tryFind (fun parts -> parts.[0] = key)
                |> Option.map (fun parts -> parts.[1])
            with
            | Some cookie -> Ok cookie
            | None -> Error "Cookie not found"
        else
            Error "Cookie not found"

    let parseXmlBody (response: HttpResponseMessage) =
        async {
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask

            match content with
            | null -> return Error "Response body is null"
            | xml ->
                try
                    let doc = XDocument.Parse(xml)
                    return Ok doc
                with :? Xml.XmlException as ex ->
                    return Error ex.Message
        }
        |> Async.RunSynchronously

    let buildUrlWithParams (url: string) (parameters: Map<string, string>) =
        let uriBuilder = new UriBuilder(url)
        let query = HttpUtility.ParseQueryString(uriBuilder.Query)

        parameters |> Map.iter (fun key value -> query.[key] <- value)

        uriBuilder.Query <- query.ToString()
        uriBuilder.Uri.AbsoluteUri

type IHttpHandler =
    abstract member Get: string -> Result<HttpResponseMessage, string>
    abstract member Post: string * StringContent -> Result<HttpResponseMessage, string>
    abstract member SetAuthHeader: string * string -> unit // TODO: this sucks, improve

type HttpHandler(?httpMessageHandler: HttpClientHandler) =
    let mutable authHeader = ""

    let doRequest (requestFunc: HttpClient -> string -> Task<HttpResponseMessage>) url =
        async {
            use client = new HttpClient(defaultArg httpMessageHandler (new HttpClientHandler()))
            // client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
            if authHeader <> "" then
                client.DefaultRequestHeaders.Add("XSRF-TOKEN", authHeader) |> ignore

            let! response = requestFunc client url |> Async.AwaitTask

            if not response.IsSuccessStatusCode then
                return Error(sprintf "Received status code %d" (int response.StatusCode))
            else
                return Ok response
        }
        |> Async.RunSynchronously

    interface IHttpHandler with
        member _.Get(url: string) =
            doRequest (fun client url -> client.GetAsync(url)) url

        member _.Post(url, content) =
            doRequest (fun client url -> client.PostAsync(url, content)) url

        member _.SetAuthHeader(key, value) = authHeader <- value
