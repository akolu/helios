namespace Helios.Core.Models

open System

type ITimeSeries =
    abstract member Time: DateTimeOffset with get
