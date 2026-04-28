## Wymagane

- .NET 10 SDK
- SQL Server LocalDB

## Baza danych

Projekt używa SQL Server LocalDB.
Przed uruchomieniem API należy wykonać skrypt:

```txt
Database/01_create_and_seed_clinic.sql
```

## Uruchomienie

```bash
git clone https://github.com/s33045/apbd-proj5-clinic-ado-net-api.git
cd apbd-proj5-clinic-ado-net-api/ClinicAdoNetApi/ClinicAdoNetApi
dotnet build
dotnet run
```