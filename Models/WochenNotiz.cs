namespace SchlagzeugVerwaltung.Models;

public class WochenNotiz
{
    public int Id { get; set; }
    public int SchuelerId { get; set; }
    public DateTime Woche { get; set; }
    public string Notiz { get; set; } = "";
}