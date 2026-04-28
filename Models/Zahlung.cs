namespace SchlagzeugVerwaltung.Models;

public class Zahlung
{
    public int Id { get; set; }
    public int SchuelerId { get; set; }
    public decimal Betrag { get; set; }
    public DateTime Datum { get; set; }
}