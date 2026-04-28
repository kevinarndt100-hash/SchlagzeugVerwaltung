namespace SchlagzeugVerwaltung.Models;

public class TerminAnzeige
{
    public int Id { get; set; }
    public DateTime Datum { get; set; }
    public int SchuelerId { get; set; }
    public string SchuelerName { get; set; } = "";
    public bool Bezahlt { get; set; }
}

public class MonatsuebersichtZeile
{
    public string SchuelerName { get; set; } = "";
    public int Stunden { get; set; }
    public decimal Bezahlt { get; set; }
    public decimal Guthaben { get; set; }
    public decimal Ausstehend { get; set; }
}

public class ZahlungAnzeige
{
    public int Id { get; set; }
    public int SchuelerId { get; set; }
    public string SchuelerName { get; set; } = "";
    public decimal Betrag { get; set; }
    public DateTime Datum { get; set; }
}

public class WochennotizAnzeige
{
    public int Id { get; set; }
    public int SchuelerId { get; set; }
    public DateTime Woche { get; set; }
    public string Notiz { get; set; } = "";
}
