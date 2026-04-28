using System.Windows;
using SchlagzeugVerwaltung.Data;
using SchlagzeugVerwaltung.Models;

namespace SchlagzeugVerwaltung;

public partial class TerminBearbeitenWindow : Window
{
    public TerminAnzeige Termin { get; private set; }
    private readonly Database _database;

    public TerminBearbeitenWindow(TerminAnzeige termin)
    {
        InitializeComponent();
        _database = new Database();
        Termin = termin;
        
        DatePickerTermin.SelectedDate = termin.Datum.Date;
        TxtZeit.Text = termin.Datum.ToString("HH:mm");
        ChkBezahlt.IsChecked = termin.Bezahlt;
    }

    private void Speichern_Click(object sender, RoutedEventArgs e)
    {
        if (DatePickerTermin.SelectedDate == null)
        {
            MessageBox.Show("Bitte wählen Sie ein Datum aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TimeSpan zeit = new TimeSpan(16, 0, 0);
        if (!string.IsNullOrWhiteSpace(TxtZeit.Text) && TimeSpan.TryParse(TxtZeit.Text, out var ts))
        {
            zeit = ts;
        }

        var neuesDatum = DatePickerTermin.SelectedDate.Value.Date + zeit;
        
        Database.UpdateTerminDatum(Termin.Id, neuesDatum);
        Database.UpdateTerminBezahlt(Termin.Id, ChkBezahlt.IsChecked == true);

        DialogResult = true;
        Close();
    }

    private void Abbrechen_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}