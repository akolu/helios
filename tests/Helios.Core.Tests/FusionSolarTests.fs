module FusionSolarTests

open Moq
open Xunit
open Helios.Core.Services
open System.Net.Http
open System.Net
open System.Text
open Newtonsoft.Json

module internal DataMocks =
    let GetStationsResponse: FusionSolar.Types.GetStations.GetStationsResponse =
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

module internal ResponseMocks =
    let LoginSuccess =
        let response =
            new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent("login success", Encoding.UTF8))

        response.Headers.Add("Set-Cookie", "XSRF-TOKEN=mockXsrfToken; path=/; secure; HttpOnly")
        response

    let LoginFailureNoCookie =
        new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent("login failure", Encoding.UTF8))

    let LoginFailureHttpError = new HttpResponseMessage(HttpStatusCode.BadRequest)

    let GetStationsSuccess =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(DataMocks.GetStationsResponse), Encoding.UTF8)
        )


let mockHttpClientWithResponse (responses: Map<string, Result<HttpResponseMessage, HttpRequestException>>) =
    let mock = new Mock<IHttpHandler>(MockBehavior.Strict)

    responses
    |> Map.iter (fun url response ->
        mock
            .Setup(fun x -> x.Post(It.Is<string>(fun s -> (=) s url), It.IsAny<StringContent>()))
            .Returns(response)
            .Verifiable())

    mock

[<Fact>]
let ``Init function should initialize record with config data & isLoggedIn to false`` () =
    // Arrange
    let config: FusionSolar.Config =
        { httpClient = new HttpHandler()
          userName = "testUser"
          systemCode = "testSystem" }

    // Act
    let result = FusionSolar.init config

    // Assert
    Assert.Equal(false, result.isLoggedIn)
    Assert.Equal(config, result.config)


// [<Fact>]
// let ``Login function should return Ok with response payload and add xsrfToken to cookies`` () =
//     // Arrange
//     let mock = mockHttpClientWithResponse Mocks.LoginSuccess

//     // Act
//     let result =
//         FusionSolar.login (
//             FusionSolar.init
//                 { httpClient = mock.Object
//                   userName = "test"
//                   systemCode = "user" }
//         )

//     // Assert
//     match result with
//     | Ok fs -> Assert.Equal("mockXsrfToken", fs.xsrfToken)
//     | Error _ -> Assert.True(false, "Expected Ok, got Error")

//     mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

// [<Fact>]
// let ``Login function should return error if xsrfToken is not found in response`` () =
//     // Arrange
//     let mock = mockHttpClientWithResponse Mocks.LoginFailureNoCookie

//     // Act
//     let result =
//         FusionSolar.login (
//             FusionSolar.init
//                 { httpClient = mock.Object
//                   userName = "test"
//                   systemCode = "user" }
//         )

//     // Assert
//     match result with
//     | Ok _ -> Assert.True(false, "Expected Error, got Ok")
//     | Error _ -> Assert.True(true)

//     mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

// [<Fact>]
// let ``Login function should return error if httpRequest returns error`` () =
//     // Arrange
//     let mock = mockHttpClientWithResponse Mocks.LoginFailureHttpError

//     // Act
//     let result =
//         FusionSolar.login (
//             FusionSolar.init
//                 { httpClient = mock.Object
//                   userName = "test"
//                   systemCode = "user" }
//         )

//     // Assert
//     match result with
//     | Ok _ -> Assert.True(false, "Expected Error, got Ok")
//     | Error _ -> Assert.True(true)

//     mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

[<Fact>]
let ``getStations should call login first if isLoggedIn is false`` () =
    // Arrange
    let mock =
        mockHttpClientWithResponse (
            Map.ofList
                [ FusionSolar.EndpointUrls.login, (Ok ResponseMocks.LoginSuccess)
                  FusionSolar.EndpointUrls.getStations, (Ok ResponseMocks.GetStationsSuccess) ]
        )

    mock
        .Setup(fun x ->
            x.SetCookie(FusionSolar.EndpointUrls.baseUrl, FusionSolar.Constants.XSRF_TOKEN_COOKIE_KEY, "mockXsrfToken"))
        .Verifiable()

    // Act
    let result =
        FusionSolar.getStations (
            FusionSolar.init
                { httpClient = mock.Object
                  userName = "test"
                  systemCode = "user" }
        )

    // Assert
    match result with
    | Ok res -> Assert.Equal(DataMocks.GetStationsResponse, res)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.VerifyAll()
