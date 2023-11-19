module HttpHandlerTests

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
open Helios.Core.Utils

let TEST_URL = "http://test.com/"

let setupHandlerMock (response: HttpResponseMessage) =
    let handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict)

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
        new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent(requestBody, Encoding.UTF8))

    response.Headers.Add("Set-Cookie", "XSRF-TOKEN=123456789; path=/; secure; HttpOnly")
    response

let withErrorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)

[<Fact>]
let ``Get should return result when response is successful`` () =
    // Arrange
    let mock = setupHandlerMock (withOkResponse "response")
    let http: IHttpHandler = new HttpHandler(mock.Object)

    // Act
    let response = http.Get TEST_URL

    // Assert
    let body = (response |> unwrap).Content.ReadAsStringAsync().Result
    Assert.Equal("response", body)

    mock
        .Protected()
        .Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(fun req -> req.RequestUri.ToString() = TEST_URL),
            ItExpr.IsAny<CancellationToken>()
        )

[<Fact>]
let ``Post should return result when response is successful & parse cookie from responde`` () =
    // Arrange
    let mock = setupHandlerMock (withOkResponse "result")
    let http: IHttpHandler = new HttpHandler(mock.Object)
    let data = {| Name = "test" |}

    // Act
    let response = http.Post(TEST_URL, (HttpUtils.toJsonStringContent data))

    // Assert
    let body = (response |> unwrap).Content.ReadAsStringAsync().Result
    Assert.Equal("result", body)
    let cookie = response >>= HttpUtils.parseCookie "XSRF-TOKEN" |> unwrap
    Assert.Equal("123456789", cookie)

    mock
        .Protected()
        .Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(fun req ->
                req.RequestUri.ToString() = TEST_URL
                && req.Content.ReadAsStringAsync().Result = JsonConvert.SerializeObject(data)),
            ItExpr.IsAny<CancellationToken>()
        )

[<Fact>]
let ``Post should throw HttpRequestException when response status code is 400`` () =
    // Arrange
    let handlerMock = setupHandlerMock withErrorResponse

    // Act
    let http: IHttpHandler = new HttpHandler(handlerMock.Object)

    // Assert
    match http.Post("http://test.com/", (HttpUtils.toJsonStringContent {| Name = "test" |})) with
    | Ok _ -> Assert.True(false, "Expected exception")
    | Error err ->
        let actualException = Assert.IsType<string>(err)
        Assert.Equal("Received status code 400", actualException)

[<Fact>]
let ``parseJsonBody should parse Json successfully from response`` () =
    // Arrange
    let response =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject {| Id = 1 |}, Encoding.UTF8)
        )

    // Act
    let result = HttpUtils.parseJsonBody response

    // Assert
    let responseBody = result |> unwrap
    Assert.Equal({| Id = 1 |}, responseBody)
