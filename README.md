# Helios

Helios is a console application designed to import energy measurement data into a database.

## Domain models

### SolarPanelOutput

Represents total solar panel output in kilowatt hours at a given hour.

```mermaid
classDiagram
    class SolarPanelOutput {
        -DateTimeOffset Time
        -double KWh
        +ToString(): string
        +Equals(obj: obj): bool
        +GetHashCode(): int
    }
```

### HouseholdEnergyReading

Represents measurements of household energy production or consumption at a given hour. Production is the electricity produced that was not used by the household. Consumption is the gross electricity consumption of the household. Production is substracted from Consumption by the electricity company and net consumption is billed to the household.

If production is more than consumption, the excess energy was fed back to the grid and sold.

```mermaid
classDiagram
    class HouseholdEnergyReading {
        -DateTimeOffset Time
        -double Production
        -double Consumption
        +ToString(): string
        +Equals(obj: obj): bool
        +GetHashCode(): int
    }
```

### ElectricitySpotPrice

Represents the price of electricity at a given hour. The price is in Euro cents per kilowatt hour.

```mermaid
classDiagram
    class ElectricitySpotPrice {
        -DateTimeOffset Time
        -decimal EuroCentsPerKWh
        +ToString(): string
        +Equals(obj: obj): bool
        +GetHashCode(): int
    }
```

## Database

Helios uses SQLite database to store energy measurements. The application will create a database if it does not exist and run any missing migrations.

### Database tables

```mermaid
erDiagram
    SolarPanelOutput {
        TEXT Time
        REAL KWh
    }
    HouseholdEnergyReading {
        TEXT Time
        REAL Production
        REAL Consumption
    }
    ElectricitySpotPrice {
        TEXT Time
        TEXT EuroCentsPerKWh
    }
```

### Schema changes

When making changes to the database schema, the following steps need to be taken:

1. Install `dotnet-ef` tool using `dotnet tool install --global dotnet-ef` command
2. Add a new migration using `dotnet ef migrations add [name] --project ../Helios.Migrations` command in `src/Helios.Core` directory
3. Apply all migrations using `dotnet ef database update` command in `src/Helios.Core` directory

## Development

1. Clone the repo
2. Initialize user secrets using `dotnet user-secrets init` command in `src/Helios.Core` directory
3. Add required secrets with `dotnet user-secrets set "[key]" "[value]"` command. For example: `dotnet user-secrets set "FusionSolar:Username" "testUser"`

## Publishing

1. Build the solution using `dotnet build` command in project root directory
2. Run `dotnet publish --configuration Release --runtime osx-arm64 --self-contained true -p:PublishSingleFile=true -p:DebugType=none` in `src/Helios.Console` directory to publish a single file executable for Apple Silicon

### User secrets

```
FusionSolar {
  Username = testUser
  Password = testPass
  StationCode = Station123
}
```

## Environment variables

When running the application in production, the following environment variables must be set:

```
FUSIONSOLAR__USERNAME
FUSIONSOLAR__PASSWORD
FUSIONSOLAR__STATIONCODE
ENTSOE__SECURITYTOKEN
FINGRID__SITEIDENTIFIERS_PRODUCTION
FINGRID__SITEIDENTIFIERS_CONSUMPTION
```
