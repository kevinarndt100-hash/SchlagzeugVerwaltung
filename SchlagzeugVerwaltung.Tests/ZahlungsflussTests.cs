using Microsoft.Data.Sqlite;
using SchlagzeugVerwaltung.Data;
using SchlagzeugVerwaltung.Models;

namespace SchlagzeugVerwaltung.Tests;

public class ZahlungsflussTests
{
    private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.db");

    public ZahlungsflussTests()
    {
        Database.Initialize();
        EnsureCoreTables();
        ResetDatabaseContent();
    }

    [Fact]
    public void Zahlung_Hinzufuegen_Anzeigen_Loeschen_Funktioniert()
    {
        var schueler = new Schueler
        {
            Name = "Max Mustermann",
            Telefon = "",
            Email = "",
            Stundensatz = 30m,
            Notizen = "",
            Guthaben = 0m,
            FixerWochentag = 0,
            FixeUhrzeit = TimeSpan.Zero
        };

        Database.AddSchueler(schueler);
        var savedSchueler = Database.GetAllSchueler().Single();

        var zahlung = new Zahlung
        {
            SchuelerId = savedSchueler.Id,
            Betrag = 25m,
            Datum = new DateTime(2026, 4, 28, 16, 0, 0)
        };

        Database.AddZahlung(zahlung);
        Database.UpdateGuthaben(savedSchueler.Id, savedSchueler.Guthaben + zahlung.Betrag);

        var zahlungen = Database.GetZahlungen(savedSchueler.Id);
        Assert.Single(zahlungen);
        Assert.Equal(25m, zahlungen[0].Betrag);

        var schuelerNachZahlung = Database.GetAllSchueler().Single(s => s.Id == savedSchueler.Id);
        Assert.Equal(25m, schuelerNachZahlung.Guthaben);

        Database.DeleteZahlung(zahlungen[0].Id);
        Database.UpdateGuthaben(savedSchueler.Id, schuelerNachZahlung.Guthaben - zahlung.Betrag);

        Assert.Empty(Database.GetZahlungen(savedSchueler.Id));

        var schuelerNachLoeschen = Database.GetAllSchueler().Single(s => s.Id == savedSchueler.Id);
        Assert.Equal(0m, schuelerNachLoeschen.Guthaben);
    }

    [Fact]
    public void ResetZahlungen_Entfernt_Zahlungen_und_Setzt_Bezahlt_Zurueck()
    {
        var schueler = new Schueler
        {
            Name = "Erika Musterfrau",
            Telefon = "",
            Email = "",
            Stundensatz = 35m,
            Notizen = "",
            Guthaben = 0m,
            FixerWochentag = 0,
            FixeUhrzeit = TimeSpan.Zero
        };

        Database.AddSchueler(schueler);
        var savedSchueler = Database.GetAllSchueler().Single();

        Database.AddTermin(new Termin
        {
            SchuelerId = savedSchueler.Id,
            Datum = new DateTime(2026, 4, 29, 17, 0, 0),
            Bezahlt = true
        });

        Database.AddZahlung(new Zahlung
        {
            SchuelerId = savedSchueler.Id,
            Betrag = 40m,
            Datum = new DateTime(2026, 4, 29, 18, 0, 0)
        });

        Database.SetGuthaben(savedSchueler.Id, 40m, 4, 2026);

        Database.ResetZahlungen();

        Assert.Empty(Database.GetZahlungen(savedSchueler.Id));
        Assert.Equal(0m, Database.GetGuthaben(savedSchueler.Id, 4, 2026));

        var termine = Database.GetAllTermine(savedSchueler.Id);
        Assert.Single(termine);
        Assert.False(termine[0].Bezahlt);
    }

    private static void EnsureCoreTables()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Schueler (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Telefon TEXT,
                Email TEXT,
                Stundensatz REAL NOT NULL,
                Notizen TEXT,
                Guthaben REAL NOT NULL DEFAULT 0,
                FixerWochentag INTEGER NOT NULL DEFAULT 0,
                FixeUhrzeit TEXT
            );

            CREATE TABLE IF NOT EXISTS Termin (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SchuelerId INTEGER NOT NULL,
                Datum TEXT NOT NULL,
                Bezahlt INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Zahlung (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SchuelerId INTEGER NOT NULL,
                Betrag REAL NOT NULL,
                Datum TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Guthaben (
                SchuelerId INTEGER NOT NULL,
                Betrag REAL NOT NULL,
                Monat INTEGER NOT NULL,
                Jahr INTEGER NOT NULL,
                PRIMARY KEY (SchuelerId, Monat, Jahr)
            );

            CREATE TABLE IF NOT EXISTS WochenNotiz (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SchuelerId INTEGER NOT NULL,
                Woche TEXT NOT NULL,
                Notiz TEXT
            );
        ";

        command.ExecuteNonQuery();
    }

    private static void ResetDatabaseContent()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Zahlung;
            DELETE FROM Termin;
            DELETE FROM Guthaben;
            DELETE FROM WochenNotiz;
            DELETE FROM Schueler;
            DELETE FROM sqlite_sequence WHERE name IN ('Zahlung','Termin','WochenNotiz','Schueler');
        ";
        command.ExecuteNonQuery();
    }
}
