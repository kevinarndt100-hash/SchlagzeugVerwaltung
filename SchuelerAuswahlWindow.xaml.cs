using System.Windows;
using SchlagzeugVerwaltung.Models;

namespace SchlagzeugVerwaltung;

public partial class SchuelerAuswahlWindow : Window
{
    public Schueler? SelectedSchueler { get; private set; }

    public SchuelerAuswahlWindow(List<Schueler> schueler)
    {
        InitializeComponent();
        SchuelerListBox.ItemsSource = schueler;
    }

    private void Auswaehlen_Click(object sender, RoutedEventArgs e)
    {
        if (SchuelerListBox.SelectedItem is Schueler s)
        {
            SelectedSchueler = s;
            DialogResult = true;
            Close();
        }
    }

    private void Abbrechen_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}