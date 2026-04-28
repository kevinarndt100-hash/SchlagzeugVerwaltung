namespace SchlagzeugVerwaltung.Models;

public class Termin
{
    public int Id { get; set; }
    public int SchuelerId { get; set; }
    public DateTime Datum { get; set; }
    public bool Bezahlt { get; set; }
}