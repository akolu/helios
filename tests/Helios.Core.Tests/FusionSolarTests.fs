module FusionSolarTests

open Moq
open Xunit
open Helios.Core.Services
open System.Net.Http
open System.Net
open System.Text

module internal Mocks =
    let LoginSuccess =
        let response =
            new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent("login success", Encoding.UTF8))

        response.Headers.Add("Set-Cookie", "XSRF-TOKEN=mockXsrfToken; path=/; secure; HttpOnly")
        response

    let LoginFailureNoCookie =
        new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent("login failure", Encoding.UTF8))

    let LoginFailureHttpError = new HttpResponseMessage(HttpStatusCode.BadRequest)


let mockHttpClientWithResponse (response: HttpResponseMessage) =
    let mock = new Mock<IHttpHandler>(MockBehavior.Strict)

    mock
        .Setup(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))
        .Returns(Ok response)
        .Verifiable()

    mock

module FusionSolarTests =

    [<Fact>]
    let ``Init function should initialize record with config data & xsrfToken to empty string`` () =
        // Arrange
        let config: FusionSolar.Config =
            { httpClient = new HttpHandler()
              userName = "testUser"
              systemCode = "testSystem" }

        // Act
        let result = FusionSolar.init config

        // Assert
        Assert.Equal("", result.xsrfToken)
        Assert.Equal(config, result.config)


[<Fact>]
let ``Login function should return Ok with xsrfToken from httpClient`` () =
    // Arrange
    let mock = mockHttpClientWithResponse Mocks.LoginSuccess

    // Act
    let result =
        FusionSolar.login (
            FusionSolar.init
                { httpClient = mock.Object
                  userName = "test"
                  systemCode = "user" }
        )

    // Assert
    match result with
    | Ok fs -> Assert.Equal("mockXsrfToken", fs.xsrfToken)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

[<Fact>]
let ``Login function should return error if xsrfToken is not found in response`` () =
    // Arrange
    let mock = mockHttpClientWithResponse Mocks.LoginFailureNoCookie

    // Act
    let result =
        FusionSolar.login (
            FusionSolar.init
                { httpClient = mock.Object
                  userName = "test"
                  systemCode = "user" }
        )

    // Assert
    match result with
    | Ok _ -> Assert.True(false, "Expected Error, got Ok")
    | Error _ -> Assert.True(true)

    mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

[<Fact>]
let ``Login function should return error if httpRequest returns error`` () =
    // Arrange
    let mock = mockHttpClientWithResponse Mocks.LoginFailureHttpError

    // Act
    let result =
        FusionSolar.login (
            FusionSolar.init
                { httpClient = mock.Object
                  userName = "test"
                  systemCode = "user" }
        )

    // Assert
    match result with
    | Ok _ -> Assert.True(false, "Expected Error, got Ok")
    | Error _ -> Assert.True(true)

    mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))

[<Fact>]
let ``getStations should call login if xsrfToken is empty`` () =
    // Arrange
    let mock = mockHttpClientWithResponse Mocks.LoginSuccess

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
    | Ok str -> Assert.Equal("getStations stub", str)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.Verify(fun x -> x.Post(It.IsAny<string>(), It.IsAny<StringContent>()))
