using System.IO;
using Microsoft.Data.Sqlite;
using SchlagzeugVerwaltung.Models;

namespace SchlagzeugVerwaltung.Data;

public class Database
{
    private static string _connectionString = null!;

    public static void Initialize()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.db");
        _connectionString = $"Data Source={dbPath}";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

var command = connection.CreateCommand();
command.CommandText = @"
              CREATE TABLE IF NOT EXISTS DrumNotation (
                  Id INTEGER PRIMARY KEY AUTOINCREMENT,
                  Name TEXT NOT NULL,
                  Inhalt TEXT NOT NULL,
                  SchuelerId INTEGER,
                  CreatedAt TEXT NOT NULL
              );

              CREATE TABLE IF NOT EXISTS DrumNotationSchuelerMigrate (Id INTEGER PRIMARY KEY);
              INSERT OR IGNORE INTO DrumNotationSchuelerMigrate VALUES (1);
          ";
        command.ExecuteNonQuery();
        command.CommandText = @"
            PRAGMA table_info(DrumNotation);
            INSERT OR IGNORE INTO DrumNotationSchuelerMigrate VALUES (1);
        ";
        command.ExecuteNonQuery();

        using var reader = command.ExecuteReader();
        bool hasSchuelerId = false;
        while (reader.Read())
        {
            if (reader.GetString(1) == "SchuelerId") hasSchuelerId = true;
        }
        reader.Close();
        if (!hasSchuelerId)
        {
            command.CommandText = "ALTER TABLE DrumNotation ADD COLUMN SchuelerId INTEGER";
            command.ExecuteNonQuery();
        }
    }

    private static SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public static List<Schueler> GetAllSchueler()
    {
        var schueler = new List<Schueler>();
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Telefon, Email, Stundensatz, Notizen, Guthaben, FixerWochentag, FixeUhrzeit FROM Schueler ORDER BY Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            schueler.Add(new Schueler
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Telefon = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stundensatz = reader.GetDecimal(4),
                Notizen = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Guthaben = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                FixerWochentag = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                FixeUhrzeit = reader.IsDBNull(8) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(8))
            });
        }
        return schueler;
    }

    public static void AddSchueler(Schueler s)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Schueler (Name, Telefon, Email, Stundensatz, Notizen, Guthaben, FixerWochentag, FixeUhrzeit)
            VALUES (@Name, @Telefon, @Email, @Stundensatz, @Notizen, @Guthaben, @FixerWochentag, @FixeUhrzeit)";
        command.Parameters.AddWithValue("@Name", s.Name);
        command.Parameters.AddWithValue("@Telefon", s.Telefon);
        command.Parameters.AddWithValue("@Email", s.Email);
        command.Parameters.AddWithValue("@Stundensatz", s.Stundensatz);
        command.Parameters.AddWithValue("@Notizen", s.Notizen);
        command.Parameters.AddWithValue("@Guthaben", s.Guthaben);
        command.Parameters.AddWithValue("@FixerWochentag", s.FixerWochentag);
        command.Parameters.AddWithValue("@FixeUhrzeit", s.FixeUhrzeit.ToString());
        command.ExecuteNonQuery();
    }

    public static void UpdateSchueler(Schueler s)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Schueler SET Name = @Name, Telefon = @Telefon, Email = @Email,
            Stundensatz = @Stundensatz, Notizen = @Notizen, Guthaben = @Guthaben,
            FixerWochentag = @FixerWochentag, FixeUhrzeit = @FixeUhrzeit WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", s.Id);
        command.Parameters.AddWithValue("@Name", s.Name);
        command.Parameters.AddWithValue("@Telefon", s.Telefon);
        command.Parameters.AddWithValue("@Email", s.Email);
        command.Parameters.AddWithValue("@Stundensatz", s.Stundensatz);
        command.Parameters.AddWithValue("@Notizen", s.Notizen);
        command.Parameters.AddWithValue("@Guthaben", s.Guthaben);
        command.Parameters.AddWithValue("@FixerWochentag", s.FixerWochentag);
        command.Parameters.AddWithValue("@FixeUhrzeit", s.FixeUhrzeit.ToString());
        command.ExecuteNonQuery();
    }

    public static void UpdateGuthaben(int schuelerId, decimal betrag)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Schueler SET Guthaben = @Guthaben WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", schuelerId);
        command.Parameters.AddWithValue("@Guthaben", betrag);
        command.ExecuteNonQuery();
    }

    public static void DeleteSchueler(int id)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Schueler WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public static List<Termin> GetTermine(int? schuelerId = null, int? month = null, int? year = null)
    {
        var termine = new List<Termin>();
        using var connection = GetConnection();
        var command = connection.CreateCommand();

        string whereClause = "";
        if (schuelerId.HasValue) whereClause += " WHERE SchuelerId = @SchuelerId";
        if (month.HasValue && year.HasValue)
        {
            whereClause += whereClause == "" ? " WHERE" : " AND";
            whereClause += " strftime('%m',Datum) = @Month AND strftime('%Y',Datum) = @Year";
        }

        command.CommandText = $"SELECT Id, SchuelerId, Datum, Bezahlt FROM Termin{whereClause} ORDER BY Datum DESC";
        if (schuelerId.HasValue) command.Parameters.AddWithValue("@SchuelerId", schuelerId.Value);
        if (month.HasValue) command.Parameters.AddWithValue("@Month", month.Value.ToString("D2"));
        if (year.HasValue) command.Parameters.AddWithValue("@Year", year.Value.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            termine.Add(new Termin
            {
                Id = reader.GetInt32(0),
                SchuelerId = reader.GetInt32(1),
                Datum = DateTime.Parse(reader.GetString(2)),
                Bezahlt = reader.GetInt32(3) == 1
            });
        }
        return termine;
    }

public static List<Termin> GetTermineForMonth(int month, int year)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id, SchuelerId, Datum, Bezahlt FROM Termin WHERE strftime('%m', Datum) = @Month AND strftime('%Y', Datum) = @Year ORDER BY Datum DESC";
        command.Parameters.AddWithValue("@Month", month.ToString("D2"));
        command.Parameters.AddWithValue("@Year", year.ToString());

        var termine = new List<Termin>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            termine.Add(new Termin
            {
                Id = reader.GetInt32(0),
                SchuelerId = reader.GetInt32(1),
                Datum = DateTime.Parse(reader.GetString(2)),
                Bezahlt = reader.GetInt32(3) == 1
            });
        }
        return termine;
    }

    public static List<Termin> GetAllTermine(int schuelerId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SchuelerId, Datum, Bezahlt FROM Termin WHERE SchuelerId = @SchuelerId ORDER BY Datum";
        command.Parameters.AddWithValue("@SchuelerId", schuelerId);

        var termine = new List<Termin>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            termine.Add(new Termin
            {
                Id = reader.GetInt32(0),
                SchuelerId = reader.GetInt32(1),
                Datum = DateTime.Parse(reader.GetString(2)),
                Bezahlt = reader.GetInt32(3) == 1
            });
        }
        return termine;
    }

    public static void AddTermin(Termin t)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Termin (SchuelerId, Datum, Bezahlt)
            VALUES (@SchuelerId, @Datum, @Bezahlt);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@SchuelerId", t.SchuelerId);
        command.Parameters.AddWithValue("@Datum", t.Datum.ToString("yyyy-MM-dd HH:mm"));
        command.Parameters.AddWithValue("@Bezahlt", t.Bezahlt ? 1 : 0);
        t.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    public static void UpdateTerminBezahlt(int id, bool bezahlt)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Termin SET Bezahlt = @Bezahlt WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Bezahlt", bezahlt ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static void DeleteTermin(int id)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Termin WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public static void UpdateTerminDatum(int id, DateTime datum)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Termin SET Datum = @Datum WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Datum", datum.ToString("yyyy-MM-dd HH:mm"));
        command.ExecuteNonQuery();
    }

    public static List<Zahlung> GetZahlungen(int? schuelerId = null)
    {
        var varzahlungen = new List<Zahlung>();
        using var connection = GetConnection();
        var command = connection.CreateCommand();

        if (schuelerId.HasValue)
        {
            command.CommandText = "SELECT Id, SchuelerId, Betrag, Datum FROM Zahlung WHERE SchuelerId = @SchuelerId ORDER BY Datum DESC";
            command.Parameters.AddWithValue("@SchuelerId", schuelerId.Value);
        }
        else
        {
            command.CommandText = "SELECT Id, SchuelerId, Betrag, Datum FROM Zahlung ORDER BY Datum DESC";
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            varzahlungen.Add(new Zahlung
            {
                Id = reader.GetInt32(0),
                SchuelerId = reader.GetInt32(1),
                Betrag = reader.GetDecimal(2),
                Datum = DateTime.Parse(reader.GetString(3))
            });
        }
        return varzahlungen;
    }

    public static void AddZahlung(Zahlung z)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Zahlung (SchuelerId, Betrag, Datum)
            VALUES (@SchuelerId, @Betrag, @Datum)";
        command.Parameters.AddWithValue("@SchuelerId", z.SchuelerId);
        command.Parameters.AddWithValue("@Betrag", z.Betrag);
        command.Parameters.AddWithValue("@Datum", z.Datum.ToString("yyyy-MM-dd HH:mm"));
        command.ExecuteNonQuery();
    }

    public static void DeleteZahlung(int id)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Zahlung WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public static List<WochenNotiz> GetWochenNotizen(int schuelerId)
    {
        var notizen = new List<WochenNotiz>();
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SchuelerId, Woche, Notiz FROM WochenNotiz WHERE SchuelerId = @SchuelerId ORDER BY Woche DESC";
        command.Parameters.AddWithValue("@SchuelerId", schuelerId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            notizen.Add(new WochenNotiz
            {
                Id = reader.GetInt32(0),
                SchuelerId = reader.GetInt32(1),
                Woche = DateTime.Parse(reader.GetString(2)),
                Notiz = reader.IsDBNull(3) ? "" : reader.GetString(3)
            });
        }
        return notizen;
    }

    public static void SaveWochenNotiz(WochenNotiz n)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO WochenNotiz (SchuelerId, Woche, Notiz)
            VALUES (@SchuelerId, @Woche, @Notiz)";
        command.Parameters.AddWithValue("@SchuelerId", n.SchuelerId);
        command.Parameters.AddWithValue("@Woche", n.Woche.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@Notiz", n.Notiz);
        command.ExecuteNonQuery();
    }

    public static void DeleteWochenNotiz(int id)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM WochenNotiz WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public static void ResetZahlungen()
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Zahlung; DELETE FROM Guthaben; UPDATE Termin SET Bezahlt = 0;";
        command.ExecuteNonQuery();
    }

    public static decimal GetGuthaben(int schuelerId, int monat, int jahr)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Betrag FROM Guthaben WHERE SchuelerId = @SchuelerId AND Monat = @Monat AND Jahr = @Jahr";
        command.Parameters.AddWithValue("@SchuelerId", schuelerId);
        command.Parameters.AddWithValue("@Monat", monat);
        command.Parameters.AddWithValue("@Jahr", jahr);
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToDecimal(result) : 0;
    }

    public static void SetGuthaben(int schuelerId, decimal betrag, int monat, int jahr)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Guthaben (SchuelerId, Betrag, Monat, Jahr)
            VALUES (@SchuelerId, @Betrag, @Monat, @Jahr)";
        command.Parameters.AddWithValue("@SchuelerId", schuelerId);
        command.Parameters.AddWithValue("@Betrag", betrag);
        command.Parameters.AddWithValue("@Monat", monat);
        command.Parameters.AddWithValue("@Jahr", jahr);
        command.ExecuteNonQuery();
    }

     public static decimal GetGuthabenVormonat(int schuelerId, int monat, int jahr)
    {
        int vormonat = monat == 1 ? 12 : monat - 1;
        int vorjahr = monat == 1 ? jahr - 1 : jahr;
        return GetGuthaben(schuelerId, vormonat, vorjahr);
    }

    public static void SaveDrumNotation(int id, string name, string inhalt, int? schuelerId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        if (id == 0)
        {
            command.CommandText = "INSERT INTO DrumNotation (Name, Inhalt, SchuelerId) VALUES (@Name, @Inhalt, @SchuelerId)";
            command.Parameters.AddWithValue("@SchuelerId", schuelerId ?? (object)DBNull.Value);
        }
        else
        {
            command.CommandText = "UPDATE DrumNotation SET Name = @Name, Inhalt = @Inhalt WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);
        }
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Inhalt", inhalt);
        command.ExecuteNonQuery();
    }

    public static List<(int Id, string Name, string Inhalt, int? SchuelerId)> GetAllDrumNotationen(int? schuelerId)
    {
        var result = new List<(int, string, string, int?)>();
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        
        if (schuelerId.HasValue)
            command.CommandText = "SELECT Id, Name, Inhalt, SchuelerId FROM DrumNotation WHERE SchuelerId = @SchuelerId ORDER BY Name";
        else
            command.CommandText = "SELECT Id, Name, Inhalt, SchuelerId FROM DrumNotation ORDER BY Name";
        
        if (schuelerId.HasValue)
        {
            command.Parameters.AddWithValue("@SchuelerId", schuelerId.Value);
        }
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int? sid = reader.IsDBNull(3) ? null : reader.GetInt32(3);
            result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), sid));
        }
        return result;
    }

    public static void DeleteDrumNotation(int id)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DrumNotation WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public static List<Zahlung> GetAllZahlungen() => GetZahlungen(null);

    public static int GetTerminCount(int schuelerId, int month, int year) =>
        GetTermine(schuelerId, month, year).Count;

    public static void DeductGuthaben(int schuelerId, int month, int year)
    {
        var termine = GetTermine(schuelerId, month, year);
        var schueler = GetAllSchueler().FirstOrDefault(s => s.Id == schuelerId);
        if (schueler == null) return;
        decimal gesamt = termine.Count * schueler.Stundensatz;
        var akt = GetGuthaben(schuelerId, month, year);
        if (akt > 0) SetGuthaben(schuelerId, akt - gesamt, month, year);
    }

    public static List<WochenNotiz> GetWochennotizen(int schuelerId) => GetWochenNotizen(schuelerId);

    public static void SaveWochennotiz(WochenNotiz n) => SaveWochenNotiz(n);

    public static void ResetGuthaben(int schuelerId, int month, int year) =>
        SetGuthaben(schuelerId, 0, month, year);

    public static void AddTermin(DateTime datum, int schuelerId, string uhrzeit, string notizen)
    {
        var t = new Termin { SchuelerId = schuelerId, Datum = datum, Bezahlt = false };
        AddTermin(t);
    }
}