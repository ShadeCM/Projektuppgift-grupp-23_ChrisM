# Personalregistret

Litet konsollprogram för att hantera personal ("myror" och framtida "bin"). Programmet är menat som en enkel demo av OO-principer och en liten lokal databas.

Kort snabbstart

- Bygg projektet:

  ```powershell
  dotnet build Personalregistret/Program.csproj
  ```

- Kör programmet:

  ```powershell
  dotnet run --project Personalregistret/Program.csproj
  ```

Snabbguide i programmet

- Menyn visar nummer för varje åtgärd. Skriv numret och tryck Enter.
- Efter varje åtgärd visas en bekräftelse och programmet väntar på Enter så att du hinner läsa meddelandet.
- När du tar bort en post visas posten som kommer att raderas och du uppmanas att bekräfta med `j` (ja) eller `n` (nej).

Utvecklingstips

- Källkoden finns i `Personalregistret/Program.cs`. Databaslogiken är separerad via `IEmployeeRepository` i `Data/`.
- Projektet innehåller en repo-lokal `NuGet.Config` som pekar mot `nuget.org` så `dotnet restore` fungerar för alla som klonar repot.

Felkällor

- Om `dotnet build` klagar om att `Program.exe` är låst så kan en tidigare körning fortfarande vara aktiv. Stäng konsolfönstret eller döda processen och försök igen.

Kontakt

- För fler ändringar, öppna en issue eller fråga i projektet.
