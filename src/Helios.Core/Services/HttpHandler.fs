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

    let parseJsonBody<'T> (response: Result<HttpResponseMessage, HttpRequestException>) =
        match response with
        | Ok response ->
            async {
                let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return Ok(JsonConvert.DeserializeObject<'T>(responseContent))
            }
            |> Async.RunSynchronously
        | Error err -> Error err

    let parseCookie (key: string) (response: Result<HttpResponseMessage, HttpRequestException>) =
        match response with
        | Ok response ->
            let mutable values: seq<string> = Seq.empty

            if response.Headers.TryGetValues("Set-Cookie", &values) then
                values
                |> Seq.map (fun x -> x.Split(';').[0].Split('='))
                |> Seq.tryFind (fun parts -> parts.[0] = key)
                |> Option.map (fun parts -> parts.[1])
            else
                None
        | Error _ -> None // TODO: handle thrown HttpResponseExceptions here

type IHttpHandler =
    abstract member Get: string -> Result<HttpResponseMessage, HttpRequestException>
    abstract member Post: string * StringContent -> Result<HttpResponseMessage, HttpRequestException>
    abstract member SetCookie: string * string * string -> unit

type HttpHandler(?httpMessageHandler: HttpClientHandler) =
    let handler = defaultArg httpMessageHandler (new HttpClientHandler())

    let doRequest (requestFunc: HttpClient -> string -> Task<HttpResponseMessage>) url =
        async {
            use client = new HttpClient(handler)
            let! response = requestFunc client url |> Async.AwaitTask

            if not response.IsSuccessStatusCode then
                return Error(new HttpRequestException(sprintf "Received status code %d" (int response.StatusCode)))
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
