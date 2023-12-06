# Helios

Helios is a console application designed to import energy measurement data into a database.

## Database

Helios uses SQLite database to store energy measurements. The database needs to be created before running the application and Entity Framework Core migrations need to be applied:

1. Install `dotnet-ef` tool using `dotnet tool install --global dotnet-ef` command
2. Run `dotnet ef database update --startup-project ../Helios.Core` command in `src/Helios.Migrations` directory

### Schema changes

When making changes to the database schema, the following steps need to be taken:

1. Add a new migration using `dotnet ef migrations add [name] --startup-project ../Helios.Core` command in `src/Helios.Migrations` directory
2. Apply the migration using `dotnet ef database update --startup-project ../Helios.Core` command in `src/Helios.Migrations` directory

## Development

1. Clone the repo
2. Initialize user secrets using `dotnet user-secrets init` command in `src/Helios.Core` directory
3. Add required secrets with `dotnet user-secrets set "[key]" "[value]"` command. For example: `dotnet user-secrets set "FusionSolar:Username" "testUser"`

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
```
