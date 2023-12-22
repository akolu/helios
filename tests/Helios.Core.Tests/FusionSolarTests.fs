module FusionSolarTests

open Xunit
open Helios.Core.DataProviders.ApiClients.FusionSolarClient
open Helios.Core.DataProviders.ApiClients.FusionSolarClient.Types
open System.Net.Http
open System.Net
open System.Text
open Moq
open Helios.Core.Logger
open Helios.Core.HttpHandler

module internal DataMocks =
    let LoginSuccessResponse: Login.ResponseBody =
        { success = true
          failCode = 0
          ``params`` = {| currentTime = 1L |}
          message = None }

    let LoginFailureResponse: Login.ResponseBody =
        { success = false
          failCode = 1
          ``params`` = {| currentTime = 1L |}
          message = Some "login failure" }

    let GetStationsResponse: GetStations.ResponseBody =
        { success = true
          failCode = 0
          message = None
          data =
            { total = 1L
              pageCount = 1L
              pageNo = 1
              pageSize = 1
              list =
                [ { plantCode = "mockPlantCode"
                    plantName = "mockPlantName"
                    plantAddress = "mockPlantAddress"
                    longitude = 1.0
                    latitude = 1.0
                    capacity = 1.0
                    contactPerson = "mockContactPerson"
                    contactMethod = "mockContactMethod"
                    gridConnectionDate = "mockGridConnectionDate" } ] } }

    let GetHourlyDataResponse: GetHourlyData.ResponseBody =
        { success = true
          failCode = 0
          ``params`` =
            {| stationCodes = "ABCD123"
               currentTime = 1L
               collectTime = 1L |}
          message = None
          data =
            [ { stationCode = "mockStationCode"
                collectTime = 1L
                dataItemMap =
                  { radiation_intensity = Some 1
                    inverter_power = Some 1
                    power_profit = Some 1
                    theory_power = None
                    ongrid_power = None } } ] }

module internal ResponseMocks =
    let LoginSuccess =
        let response =
            new HttpResponseMessage(
                HttpStatusCode.OK,
                Content =
                    new StringContent(Json.JsonSerializer.Serialize(DataMocks.LoginSuccessResponse), Encoding.UTF8)
            )

        response.Headers.Add("Set-Cookie", "XSRF-TOKEN=mockXsrfToken; path=/; secure; HttpOnly")
        response

    let LoginFailureNoCookie =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(Json.JsonSerializer.Serialize(DataMocks.LoginFailureResponse), Encoding.UTF8)
        )

    let LoginFailureHttpError = new HttpResponseMessage(HttpStatusCode.BadRequest)

    let GetStationsSuccess =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(Json.JsonSerializer.Serialize(DataMocks.GetStationsResponse), Encoding.UTF8)
        )

    let getHourlyDataSuccess =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(Json.JsonSerializer.Serialize(DataMocks.GetHourlyDataResponse), Encoding.UTF8)
        )

let mockHttpClientWithResponse (responses: Map<string, Result<HttpResponseMessage, string>>) =
    let mock = new Mock<IHttpHandler>(MockBehavior.Strict)

    responses
    |> Map.iter (fun url response ->
        mock
            .Setup(fun x -> x.Post(It.Is<string>(fun s -> (=) s url), It.IsAny<StringContent>()))
            .Returns(response)
            .Verifiable())

    mock

let mockLogger = createLogger (new HeliosLoggerProvider(LoggerOptions.None))

[<Fact>]
let ``Init function should initialize record with config data & isLoggedIn to false`` () =
    // Arrange
    let config: Config =
        { HttpClient = new HttpHandler()
          Logger = mockLogger
          UserName = "testUser"
          SystemCode = "testSystem" }

    // Act
    let result = FusionSolar.Init config

    // Assert
    Assert.Equal(false, result.IsLoggedIn)
    Assert.Equal(config, result.Config)

[<Fact>]
let ``getStations should call login first if isLoggedIn is false, should return stations response`` () =
    // Arrange
    let mock =
        mockHttpClientWithResponse (
            Map.ofList
                [ EndpointUrls.Login, (Ok ResponseMocks.LoginSuccess)
                  EndpointUrls.GetStations, (Ok ResponseMocks.GetStationsSuccess) ]
        )

    mock
        .Setup(fun x -> x.SetAuthHeader(Constants.XSRF_TOKEN_COOKIE_KEY, "mockXsrfToken"))
        .Verifiable()

    // Act
    let result =
        getStations (
            FusionSolar.Init
                { HttpClient = mock.Object
                  Logger = mockLogger
                  UserName = "test"
                  SystemCode = "user" }
        )

    // Assert
    match result with
    | Ok res -> Assert.Equal(DataMocks.GetStationsResponse, res)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.VerifyAll()

[<Fact>]
let ``getStations should not call login if isLoggedIn is true, should return stations response`` () =
    // Arrange
    let mock =
        mockHttpClientWithResponse (Map.ofList [ EndpointUrls.GetStations, (Ok ResponseMocks.GetStationsSuccess) ])

    // Act
    let result =
        getStations (
            { IsLoggedIn = true
              Config =
                { HttpClient = mock.Object
                  Logger = mockLogger
                  UserName = "test"
                  SystemCode = "user" } }
        )

    // Assert
    match result with
    | Ok res -> Assert.Equal(DataMocks.GetStationsResponse, res)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.VerifyAll()

[<Fact>]
let ``getHourlyData should call login first if isLoggedIn is false, should return hourly data response`` () =
    // Arrange
    let mock =
        mockHttpClientWithResponse (
            Map.ofList
                [ EndpointUrls.Login, (Ok ResponseMocks.LoginSuccess)
                  EndpointUrls.GetHourlyData, (Ok ResponseMocks.getHourlyDataSuccess) ]
        )

    mock
        .Setup(fun x -> x.SetAuthHeader(Constants.XSRF_TOKEN_COOKIE_KEY, "mockXsrfToken"))
        .Verifiable()

    // Act
    let result =
        getHourlyData
            { stationCodes = "ABCD123"
              collectTime = 1L }
            (FusionSolar.Init
                { HttpClient = mock.Object
                  Logger = mockLogger
                  UserName = "test"
                  SystemCode = "user" })

    // Assert
    match result with
    | Ok res -> Assert.Equal(DataMocks.GetHourlyDataResponse, res)
    | Error err -> Assert.True(false, err)

    mock.VerifyAll()

[<Fact>]
let ``getHourlyData should not call login if isLoggedIn is true, should return hourly data response`` () =
    // Arrange
    let mock =
        mockHttpClientWithResponse (Map.ofList [ EndpointUrls.GetHourlyData, (Ok ResponseMocks.getHourlyDataSuccess) ])

    // Act
    let result =
        getHourlyData
            { stationCodes = "ABCD123"
              collectTime = 1L }
            { IsLoggedIn = true
              Config =
                { HttpClient = mock.Object
                  Logger = mockLogger
                  UserName = "test"
                  SystemCode = "user" } }

    // Assert
    match result with
    | Ok res -> Assert.Equal(DataMocks.GetHourlyDataResponse, res)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.VerifyAll()
