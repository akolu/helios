module HttpClientTests

open Moq
open Moq.Protected
open System.Net
open System.Net.Http
open Newtonsoft.Json
open Helios.Core.Services
open Xunit
open System.Threading.Tasks
open System.Threading
open System.Text
open Helios.Utils

let setupHandlerMock (response: HttpResponseMessage) =
    let handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict)

    handlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(response)
        .Verifiable()

    handlerMock.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).Verifiable()

    handlerMock

let withOkResponse requestBody =
    let response =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
        )

    response.Headers.Add("Set-Cookie", "XSRF-TOKEN=123456789; path=/; secure; HttpOnly")
    response

let withErrorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)

[<Fact>]
let ``Post should return deserialized object when response is successful`` () =
    // Arrange
    let expected = {| Id = 1 |}
    let mock = setupHandlerMock (withOkResponse expected)
    let httpClient: IHttpClient = new HttpClient(mock.Object)
    let url = "http://test.com/"
    let data = {| Name = "test" |}

    // Act
    let response = httpClient.Post url data

    // Assert
    let responseBody = HttpClient.ParseBody response |> unwrap
    Assert.Equal(expected, responseBody)
    let cookie = HttpClient.ParseCookie "XSRF-TOKEN" response
    Assert.Equal("123456789", cookie.Value)

    mock
        .Protected()
        .Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(fun req ->
                req.RequestUri.ToString() = url
                && req.Content.ReadAsStringAsync().Result = JsonConvert.SerializeObject(data)),
            ItExpr.IsAny<CancellationToken>()
        )

[<Fact>]
let ``Post should throw HttpRequestException when response status code is 400`` () =
    // Arrange
    let handlerMock = setupHandlerMock withErrorResponse

    // Act
    let httpClient: IHttpClient = new HttpClient(handlerMock.Object)

    // Assert
    match httpClient.Post "http://test.com/" {| Name = "test" |} with
    | Ok _ -> Assert.True(false, "Expected exception")
    | Error err ->
        let actualException = Assert.IsType<HttpRequestException>(err)
        Assert.Equal("Received status code 400", actualException.Message)
