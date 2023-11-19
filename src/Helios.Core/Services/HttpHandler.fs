namespace Helios.Core.Services

open System.Text
open System.Net.Http
open Newtonsoft.Json
open System.Threading.Tasks
open System.Net
open System

module HttpUtils =

    let toJsonStringContent (data: obj) =
        new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")

    let parseJsonBody<'T> (response: HttpResponseMessage) =
        match response.Content.ReadAsStringAsync().Result with
        | null -> Error "Response body is null"
        | json ->
            try
                Ok(JsonConvert.DeserializeObject<'T>(json))
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

type IHttpHandler =
    abstract member Get: string -> Result<HttpResponseMessage, string>
    abstract member Post: string * StringContent -> Result<HttpResponseMessage, string>
    abstract member SetCookie: string * string * string -> unit

type HttpHandler(?httpMessageHandler: HttpClientHandler) =
    let handler = defaultArg httpMessageHandler (new HttpClientHandler())

    let doRequest (requestFunc: HttpClient -> string -> Task<HttpResponseMessage>) url =
        async {
            use client = new HttpClient(handler)
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

        member _.SetCookie(url, key, value) =
            if handler.CookieContainer = null then
                handler.UseCookies <- true
                handler.CookieContainer <- new CookieContainer()

            handler.CookieContainer.Add(new Uri(url), new Cookie(key, value))
