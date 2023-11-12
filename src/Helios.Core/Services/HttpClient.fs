﻿namespace Helios.Core.Services

open System.Net.Http
open System.Text
open Newtonsoft.Json

type IHttpClient =
    abstract member Post: string -> obj -> Async<Result<HttpResponseMessage, HttpRequestException>>

type HttpClient(?httpMessageHandler: HttpMessageHandler) =

    interface IHttpClient with
        member this.Post (url: string) (data: obj) =
            async {
                use client =
                    new System.Net.Http.HttpClient(defaultArg httpMessageHandler (new HttpClientHandler()))

                let json = JsonConvert.SerializeObject(data)
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                let! response = client.PostAsync(url, content) |> Async.AwaitTask

                if not response.IsSuccessStatusCode then
                    return Error(new HttpRequestException(sprintf "Received status code %d" (int response.StatusCode)))
                else
                    return Ok response
            }

    static member ParseBody<'T>(response: HttpResponseMessage) =
        async {
            let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return JsonConvert.DeserializeObject<'T>(responseContent)
        }

    static member ParseCookie (key: string) (response: HttpResponseMessage) =
        let mutable values: seq<string> = Seq.empty

        if response.Headers.TryGetValues("Set-Cookie", &values) then
            values
            |> Seq.map (fun x -> x.Split(';').[0].Split('='))
            |> Seq.tryFind (fun parts -> parts.[0] = key)
            |> Option.map (fun parts -> parts.[1])
        else
            None
