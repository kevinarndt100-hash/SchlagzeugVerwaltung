using System;

namespace SchlagzeugVerwaltung.Models
{
    public class DrumNotation
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Inhalt { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}