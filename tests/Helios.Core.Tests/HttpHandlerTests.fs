module HttpHandlerTests

open Moq
open Moq.Protected
open System.Net
open System.Net.Http
open Helios.Core.Services
open Xunit
open System.Threading.Tasks
open System.Threading
open System.Text
open Helios.Core.Utils
open Helios.Core.Models

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
                && req.Content.ReadAsStringAsync().Result = Json.JsonSerializer.Serialize(data)),
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
            Content = new StringContent(Json.JsonSerializer.Serialize {| Id = 1 |}, Encoding.UTF8)
        )

    // Act
    let result = HttpUtils.parseJsonBody response

    // Assert
    let responseBody = result |> unwrap
    Assert.Equal({| Id = 1 |}, responseBody)

[<Fact>]
let ``parseJsonBody deserializes FusionSolar.GetHourlyData.ResponseBody correctly`` () =
    let data =
        """
{
  "data": [
    {
      "collectTime": 1696042800000,
      "stationCode": "123",
      "dataItemMap": {
        "radiation_intensity": null,
        "inverter_power": null,
        "power_profit": null,
        "theory_power": null,
        "ongrid_power": null
      }
    },
    {
      "collectTime": 1696046400000,
      "stationCode": "123",
      "dataItemMap": {
        "radiation_intensity": null,
        "inverter_power": 0,
        "power_profit": null,
        "theory_power": null,
        "ongrid_power": null
      }
    },
    {
      "collectTime": 1696050000000,
      "stationCode": "123",
      "dataItemMap": {
        "radiation_intensity": null,
        "inverter_power": 0.01,
        "power_profit": null,
        "theory_power": null,
        "ongrid_power": null
      }
    },
  ],
  "failCode": 0,
  "message": null,
  "params": {
    "currentTime": 1701818143912,
    "collectTime": 1696021200000,
    "stationCodes": "123"
  },
  "success": true
}
"""

    let responseMessage =
        new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent(data, Encoding.UTF8))

    let expected: FusionSolar.Types.GetHourlyData.ResponseBody =
        { success = true
          failCode = 0
          ``params`` =
            {| stationCodes = "123"
               currentTime = 1701818143912L
               collectTime = 1696021200000L |}
          message = None
          data =
            [ { stationCode = "123"
                collectTime = 1696042800000L
                dataItemMap =
                  { radiation_intensity = None
                    inverter_power = None
                    power_profit = None
                    theory_power = None
                    ongrid_power = None } }
              { stationCode = "123"
                collectTime = 1696046400000L
                dataItemMap =
                  { radiation_intensity = None
                    inverter_power = Some 0.0
                    power_profit = None
                    theory_power = None
                    ongrid_power = None } }
              { stationCode = "123"
                collectTime = 1696050000000L
                dataItemMap =
                  { radiation_intensity = None
                    inverter_power = Some 0.01
                    power_profit = None
                    theory_power = None
                    ongrid_power = None } } ] }

    match HttpUtils.parseJsonBody<FusionSolar.Types.GetHourlyData.ResponseBody> (responseMessage) with
    | Ok responseBody -> Assert.Equal(responseBody, expected)
    | Error err -> Assert.True(false, err)
