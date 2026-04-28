namespace SchlagzeugVerwaltung.Models;

public class Schueler
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Stundensatz { get; set; }
    public string Notizen { get; set; } = "";
    public decimal Guthaben { get; set; }
    public int FixerWochentag { get; set; }
    public TimeSpan FixeUhrzeit { get; set; }
}