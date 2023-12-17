module EntsoETests

open Xunit
open TestUtils
open Helios.Core.Services
open Helios.Core.Services.EntsoE.Types
open System.Net.Http
open System.Net
open System.Text
open Moq
open System

module internal DataMocks =
    let DayAheadPricesResponse =
        """
<Publication_MarketDocument xmlns="urn:iec62325.351:tc57wg16:451-3:publicationdocument:7:0">
    <mRID>46bf4f7508b8408eab1fdfe9657ce832</mRID>
    <revisionNumber>1</revisionNumber>
    <type>A44</type>
    <sender_MarketParticipant.mRID codingScheme="A01">10X1001A1001A450</sender_MarketParticipant.mRID>
    <sender_MarketParticipant.marketRole.type>A32</sender_MarketParticipant.marketRole.type>
    <receiver_MarketParticipant.mRID codingScheme="A01">10X1001A1001A450</receiver_MarketParticipant.mRID>
    <receiver_MarketParticipant.marketRole.type>A33</receiver_MarketParticipant.marketRole.type>
    <createdDateTime>2023-12-15T23:31:59Z</createdDateTime>
    <period.timeInterval>
        <start>2023-03-25T23:00Z</start>
        <end>2023-03-27T22:00Z</end>
    </period.timeInterval>
    <TimeSeries>
        <mRID>1</mRID>
        <businessType>A62</businessType>
        <in_Domain.mRID codingScheme="A01">10YFI-1--------U</in_Domain.mRID>
        <out_Domain.mRID codingScheme="A01">10YFI-1--------U</out_Domain.mRID>
        <currency_Unit.name>EUR</currency_Unit.name>
        <price_Measure_Unit.name>MWH</price_Measure_Unit.name>
        <curveType>A01</curveType>
        <Period>
            <timeInterval>
                <start>2023-03-25T23:00Z</start>
                <end>2023-03-26T01:00Z</end>
            </timeInterval>
            <resolution>PT60M</resolution>
            <Point>
                <position>1</position>
                <price.amount>39.66</price.amount>
            </Point>
            <Point>
                <position>2</position>
                <price.amount>39.23</price.amount>
            </Point>
            <Point>
                <position>3</position>
                <price.amount>40.12</price.amount>
            </Point>
        </Period>
    </TimeSeries>
    <TimeSeries>
        <mRID>2</mRID>
        <businessType>A62</businessType>
        <in_Domain.mRID codingScheme="A01">10YFI-1--------U</in_Domain.mRID>
        <out_Domain.mRID codingScheme="A01">10YFI-1--------U</out_Domain.mRID>
        <currency_Unit.name>EUR</currency_Unit.name>
        <price_Measure_Unit.name>MWH</price_Measure_Unit.name>
        <curveType>A01</curveType>
        <Period>
            <timeInterval>
                <start>2023-03-27T00:00Z</start>
                <end>2023-03-27T01:00Z</end>
            </timeInterval>
            <resolution>PT60M</resolution>
            <Point>
                <position>1</position>
                <price.amount>42.13</price.amount>
            </Point>
            <Point>
                <position>2</position>
                <price.amount>40.31</price.amount>
            </Point>
        </Period>
    </TimeSeries>
</Publication_MarketDocument>"""

module internal ResponseMocks =
    let DayAheadPrices =
        new HttpResponseMessage(
            HttpStatusCode.OK,
            Content = new StringContent(DataMocks.DayAheadPricesResponse, Encoding.UTF8)
        )


let mockHttpClientWithResponse (responses: Map<string, Result<HttpResponseMessage, string>>) =
    let mock = new Mock<IHttpHandler>(MockBehavior.Strict)

    responses
    |> Map.iter (fun url response ->
        mock
            .Setup(fun x -> x.Get(It.Is<string>(fun s -> (=) s url)))
            .Returns(response)
            .Verifiable())

    mock

[<Fact>]
let ``getDayAheadPrices should return TimeSeriesPeriod list parsed from returned XML`` () =
    // Arrange
    let apiUrl =
        EntsoE.Constants.API_URL
        + "?documentType=A44&in_Domain=10YFI-1--------U&out_Domain=10YFI-1--------U&securityToken=test&timeInterval=2023-03-25T23%3a00%3a00.0000000%2b00%3a00%2f2023-03-27T22%3a00%3a00.0000000%2b00%3a00"

    let mock =
        mockHttpClientWithResponse (Map.ofList [ apiUrl, (Ok ResponseMocks.DayAheadPrices) ])

    // Act
    let result =
        EntsoE.getDayAheadPrices
            (DateTimeOffset.Parse("2023-03-25T23:00Z"), (DateTimeOffset.Parse("2023-03-27T22:00Z")))
            (EntsoE.init
                { HttpClient = mock.Object
                  Logger = new MockLogger()
                  SecurityToken = "test" })

    // Assert
    let expected =
        [ { TimeSeriesPeriod.TimeInterval =
              { TimeSeriesPeriodInterval.StartDt = DateTimeOffset.Parse("2023-03-25T23:00Z")
                TimeSeriesPeriodInterval.EndDt = DateTimeOffset.Parse("2023-03-26T01:00Z") }
            TimeSeriesPeriod.Points =
              [ { TimeSeriesPoint.Position = 1
                  TimeSeriesPoint.PriceAmount = 39.66m }
                { TimeSeriesPoint.Position = 2
                  TimeSeriesPoint.PriceAmount = 39.23m }
                { TimeSeriesPoint.Position = 3
                  TimeSeriesPoint.PriceAmount = 40.12m } ] }
          { TimeSeriesPeriod.TimeInterval =
              { TimeSeriesPeriodInterval.StartDt = DateTimeOffset.Parse("2023-03-27T00:00Z")
                TimeSeriesPeriodInterval.EndDt = DateTimeOffset.Parse("2023-03-27T01:00Z") }
            TimeSeriesPeriod.Points =
              [ { TimeSeriesPoint.Position = 1
                  TimeSeriesPoint.PriceAmount = 42.13m }
                { TimeSeriesPoint.Position = 2
                  TimeSeriesPoint.PriceAmount = 40.31m } ] } ]

    match result with
    | Ok res -> Assert.Equal<TimeSeriesPeriod list>(res, expected)
    | Error _ -> Assert.True(false, "Expected Ok, got Error")

    mock.VerifyAll()
