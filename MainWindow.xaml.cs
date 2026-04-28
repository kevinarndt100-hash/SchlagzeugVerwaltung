using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SchlagzeugVerwaltung.Data;
using SchlagzeugVerwaltung.Models;

namespace SchlagzeugVerwaltung;

public partial class MainWindow : Window
{
    private const string GrooveScribeUrl = "https://www.mikeslessons.com/gscribe/";
    private List<Schueler> _schuelerList = new List<Schueler>();
    private Schueler? _currentSchueler;
    private int _selectedMonth;
    private int _selectedYear;
    private int _selectedTerminSchuelerId;
    private int _currentDrumNotationId;
    private int _selectedSchuelerIdWochennotiz;
    private int _selectedSchuelerIdZahlung;
    private DateTime _selectedWoche;
    private List<string> _drumNotationLines = new List<string> { "1|--|--|--|--|--|--|--" };
    private int _currentBeat;
    private int _selectedNoteValue = 4;

    public MainWindow()
    {
        InitializeComponent();
        Database.Initialize();
        InitializeComboBoxes();
        LoadSchueler();
        LoadTermineForCurrentMonth();
        LoadZahlungen();
        InitializeDrumNotationTab();
    }

    private void InitializeComboBoxes()
    {
        string[] monate = new string[12]
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober",
            "November", "Dezember"
        };
        for (int i = 0; i < monate.Length; i++)
        {
            CmbMonat.Items.Add(monate[i]);
            CmbTerminMonat.Items.Add(monate[i]);
        }
        CmbMonat.SelectedIndex = DateTime.Now.Month - 1;
        CmbTerminMonat.SelectedIndex = DateTime.Now.Month - 1;
        for (int j = 2024; j <= 2030; j++)
        {
            CmbJahr.Items.Add(j.ToString());
        }
        CmbJahr.SelectedIndex = DateTime.Now.Year - 2024;
        _selectedMonth = DateTime.Now.Month;
        _selectedYear = DateTime.Now.Year;
        DatePickerZahlung.SelectedDate = DateTime.Now;
        DatePickerWochennotiz.SelectedDate = DateTime.Now;
        DateTime now = DateTime.Now;
        int dayOfWeek = (int)now.DayOfWeek;
        DateTime wocheStart = now.AddDays(-((dayOfWeek == 0) ? 6 : (dayOfWeek - 1)));
        _selectedWoche = wocheStart;
        TxtAktuelleWoche.Text = $"Woche vom {wocheStart:dd.MM.yyyy}";
    }

    private void LoadSchueler()
    {
        _schuelerList = Database.GetAllSchueler();
        SchuelerListBox.ItemsSource = _schuelerList;
        CmbTerminSchueler.Items.Clear();
        CmbSchuelerZahlung.Items.Clear();
        CmbSchuelerWochennotiz.Items.Clear();
        foreach (Schueler s in _schuelerList)
        {
            CmbTerminSchueler.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Id });
            CmbSchuelerZahlung.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Id });
            CmbSchuelerWochennotiz.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Id });
        }
    }

    private void SchuelerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SchuelerListBox.SelectedItem is not Schueler schueler) return;
        _currentSchueler = schueler;
        SchuelerDetailsPanel.Visibility = Visibility.Visible;
        TxtName.Text = schueler.Name;
        TxtTelefon.Text = schueler.Telefon;
        TxtEmail.Text = schueler.Email;
        TxtStundensatz.Text = schueler.Stundensatz.ToString();
        TxtNotizen.Text = schueler.Notizen;
        for (int i = 0; i < CmbFixerWochentag.Items.Count; i++)
        {
            if (CmbFixerWochentag.Items[i] is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var result) && result == schueler.FixerWochentag)
            {
                CmbFixerWochentag.SelectedIndex = i;
                break;
            }
        }
        TxtFixeUhrzeit.Text = schueler.FixeUhrzeit.ToString(@"hh\:mm");
    }

    private void NeuerSchueler_Click(object sender, RoutedEventArgs e)
    {
        _currentSchueler = null;
        SchuelerDetailsPanel.Visibility = Visibility.Visible;
        TxtName.Text = "";
        TxtTelefon.Text = "";
        TxtEmail.Text = "";
        TxtStundensatz.Text = "";
        TxtNotizen.Text = "";
        CmbFixerWochentag.SelectedIndex = 0;
        TxtFixeUhrzeit.Text = "";
    }

    private void SchuelerSpeichern_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Bitte geben Sie einen Namen ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (!decimal.TryParse(TxtStundensatz.Text, out var result))
        {
            result = 30m;
        }
        int fixerWochentag = 0;
        if (CmbFixerWochentag.SelectedItem is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var result2))
        {
            fixerWochentag = result2;
        }
        TimeSpan fixeUhrzeit = TimeSpan.Zero;
        if (!string.IsNullOrEmpty(TxtFixeUhrzeit.Text) && TimeSpan.TryParse(TxtFixeUhrzeit.Text, out var result3))
        {
            fixeUhrzeit = result3;
        }
        Schueler s = new Schueler
        {
            Id = (_currentSchueler?.Id ?? 0),
            Name = TxtName.Text.Trim(),
            Telefon = TxtTelefon.Text.Trim(),
            Email = TxtEmail.Text.Trim(),
            Stundensatz = result,
            Notizen = TxtNotizen.Text.Trim(),
            Guthaben = (_currentSchueler?.Guthaben ?? 0m),
            FixerWochentag = fixerWochentag,
            FixeUhrzeit = fixeUhrzeit
        };
        if (_currentSchueler == null)
        {
            Database.AddSchueler(s);
        }
        else
        {
            Database.UpdateSchueler(s);
        }
        LoadSchueler();
        MessageBox.Show("Schüler gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private void SchuelerLoeschen_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSchueler != null && MessageBox.Show("Möchten Sie " + _currentSchueler.Name + " löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Database.DeleteSchueler(_currentSchueler.Id);
            SchuelerDetailsPanel.Visibility = Visibility.Collapsed;
            LoadSchueler();
        }
    }

    private void TermineDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TermineDataGrid.SelectedItem is TerminAnzeige termin)
        {
            new TerminBearbeitenWindow(termin).ShowDialog();
            LoadTermineForCurrentMonth();
        }
    }

    private void DatePickerTermin_SelectedDateChanged(object sender, SelectionChangedEventArgs e) { }

    private void TerminHinzufuegen_Click(object sender, RoutedEventArgs e)
    {
        if (!DatePickerTermin.SelectedDate.HasValue)
        {
            MessageBox.Show("Bitte wählen Sie ein Datum aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (CmbTerminSchueler.SelectedItem == null)
        {
            MessageBox.Show("Bitte wählen Sie einen Schüler aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        object tag = (CmbTerminSchueler.SelectedItem as ComboBoxItem)?.Tag;
        if (tag is not int schuelerId) return;
        DateTime dateTime = DatePickerTermin.SelectedDate.Value;
        if (!string.IsNullOrEmpty(TxtTerminZeit.Text) && TimeSpan.TryParse(TxtTerminZeit.Text, out var result))
        {
            dateTime = dateTime.Date + result;
        }
        DateTime now = DateTime.Now;
        if (dateTime < now)
        {
            MessageBox.Show("Der Termin liegt in der Vergangenheit.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        Database.AddTermin(new Termin { SchuelerId = schuelerId, Datum = dateTime, Bezahlt = false });
        Schueler schueler = _schuelerList.FirstOrDefault((Schueler s) => s.Id == schuelerId);
        if (schueler != null && schueler.Guthaben > 0m)
        {
            decimal betrag = schueler.Guthaben - schueler.Stundensatz;
            Database.UpdateGuthaben(schuelerId, betrag);
        }
        LoadTermineForCurrentMonth();
        LoadSchueler();
    }

    private void LoadTermineForCurrentMonth()
    {
        List<Termin> termine = Database.GetTermine(null, _selectedMonth, _selectedYear);
        List<TerminAnzeige> items = (from t in termine
            select new TerminAnzeige
            {
                Id = t.Id,
                Datum = t.Datum,
                SchuelerId = t.SchuelerId,
                SchuelerName = (_schuelerList.FirstOrDefault((Schueler s) => s.Id == t.SchuelerId)?.Name ?? ""),
                Bezahlt = t.Bezahlt
            }).ToList();
        TermineDataGrid.ItemsSource = items;
    }

    private void CmbTerminMonat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTerminMonat.SelectedIndex >= 0)
        {
            _selectedMonth = CmbTerminMonat.SelectedIndex + 1;
            _selectedYear = int.Parse(CmbJahr.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
            LoadTermineForCurrentMonth();
        }
    }

    private void CmbMonat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbMonat.SelectedIndex >= 0 && CmbJahr.SelectedIndex >= 0)
        {
            _selectedMonth = CmbMonat.SelectedIndex + 1;
            _selectedYear = int.Parse(CmbJahr.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
            LoadMonatsuebersicht();
        }
    }

    private void LoadMonatsuebersicht()
    {
        List<MonatsuebersichtZeile> list = new List<MonatsuebersichtZeile>();
        List<Schueler> allSchueler = Database.GetAllSchueler();
        List<Termin> termineImMonat = Database.GetTermine(null, _selectedMonth, _selectedYear);
        List<Zahlung> alleZahlungen = Database.GetZahlungen();
        DateTime monatStart = new DateTime(_selectedYear, _selectedMonth, 1);
        decimal gesamteinnahmen = 0m;
        foreach (Schueler s in allSchueler)
        {
            List<Termin> alleTermineSchueler = Database.GetAllTermine(s.Id);
            int anzahl = termineImMonat.Count((Termin t) => t.SchuelerId == s.Id);
            decimal zuZahlen = (decimal)anzahl * s.Stundensatz;
            decimal gezahlt = alleZahlungen.Where((Zahlung z) => z.SchuelerId == s.Id && z.Datum.Month == _selectedMonth && z.Datum.Year == _selectedYear).Sum((Zahlung z) => z.Betrag);

            decimal gezahltVorMonat = alleZahlungen.Where((Zahlung z) => z.SchuelerId == s.Id && z.Datum < monatStart).Sum((Zahlung z) => z.Betrag);
            int termineVorMonat = alleTermineSchueler.Count((Termin t) => t.Datum < monatStart);
            decimal guthabenVormonat = gezahltVorMonat - (decimal)termineVorMonat * s.Stundensatz;

            decimal guthaben = guthabenVormonat + gezahlt - zuZahlen;
            decimal ausstehend = zuZahlen - gezahlt;
            if (anzahl > 0 || gezahlt > 0m || guthabenVormonat > 0m)
            {
                list.Add(new MonatsuebersichtZeile
                {
                    SchuelerName = s.Name,
                    Stunden = anzahl,
                    Bezahlt = zuZahlen,
                    Guthaben = guthaben,
                    Ausstehend = ausstehend
                });
                gesamteinnahmen += zuZahlen;
            }
        }
        MonatsuebersichtDataGrid.ItemsSource = list;
        TxtGesamteinnahmen.Text = $"{gesamteinnahmen:0.00} €";
    }

    private void ExportCSV_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            FileName = $"Monatsuebersicht_{_selectedMonth}_{_selectedYear}.csv",
            Filter = "CSV-Datei (*.csv)|*.csv"
        };
        if (saveFileDialog.ShowDialog() != true) return;
        List<string> lines = new List<string> { "Schüler;Stunden;Zu zahlen;Guthaben;Ausstehend" };
        foreach (MonatsuebersichtZeile item in MonatsuebersichtDataGrid.Items)
        {
            lines.Add($"{item.SchuelerName};{item.Stunden};{item.Bezahlt:0.00};{item.Guthaben:0.00};{item.Ausstehend:0.00}");
        }
        System.IO.File.WriteAllLines(saveFileDialog.FileName, lines);
        MessageBox.Show("Export erfolgreich.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private void CmbTerminSchueler_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTerminSchueler.SelectedItem is not ComboBoxItem { Tag: var tag } || tag is not int num) return;
        _selectedTerminSchuelerId = num;
        List<TerminAnzeige> itemsSource = (from t in Database.GetAllTermine(num)
            where t.Datum.Month == _selectedMonth && t.Datum.Year == _selectedYear
            select new TerminAnzeige
            {
                Id = t.Id,
                Datum = t.Datum,
                SchuelerId = t.SchuelerId,
                SchuelerName = (_schuelerList.FirstOrDefault((Schueler s) => s.Id == t.SchuelerId)?.Name ?? ""),
                Bezahlt = t.Bezahlt
            }).ToList();
        TermineDataGrid.ItemsSource = itemsSource;
    }

    private void TerminLoeschen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TerminAnzeige dataContext } && MessageBox.Show("Termin löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Database.DeleteTermin(dataContext.Id);
            LoadTermineForCurrentMonth();
        }
    }

    private void TerminBezahlt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: TerminAnzeige { Id: var id }, IsChecked: var isChecked })
        {
            Database.UpdateTerminBezahlt(id, isChecked == true);
            LoadTermineForCurrentMonth();
        }
    }

    private void LoadZahlungen()
    {
        List<Zahlung> zahlungen = Database.GetZahlungen();
        List<ZahlungAnzeige> items = (from z in zahlungen
            select new ZahlungAnzeige
            {
                Id = z.Id,
                SchuelerId = z.SchuelerId,
                Betrag = z.Betrag,
                Datum = z.Datum,
                SchuelerName = (_schuelerList.FirstOrDefault((Schueler s) => s.Id == z.SchuelerId)?.Name ?? "")
            }).ToList();
        ZahlungenDataGrid.ItemsSource = items;
    }

    private void ZahlungSpeichern_Click(object sender, RoutedEventArgs e)
    {
        if (CmbSchuelerZahlung.SelectedItem is ComboBoxItem { Tag: var tag } && tag is int schuelerId)
        {
            if (!decimal.TryParse(TxtBetrag.Text, out var result) || result <= 0m)
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Betrag ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (!DatePickerZahlung.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte wählen Sie ein Datum aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            Database.AddZahlung(new Zahlung { SchuelerId = schuelerId, Betrag = result, Datum = DatePickerZahlung.SelectedDate.Value });
            Schueler schueler = _schuelerList.FirstOrDefault((Schueler s) => s.Id == schuelerId);
            if (schueler != null)
            {
                decimal betrag = schueler.Guthaben + result;
                Database.UpdateGuthaben(schuelerId, betrag);
            }
            TxtBetrag.Text = "";
            LoadZahlungen();
            LoadSchueler();
            MessageBox.Show("Zahlung gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }
        else
        {
            MessageBox.Show("Bitte wählen Sie einen Schüler aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }

    private void ZahlungLoeschen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: var dataContext }) return;
        ZahlungAnzeige? z = dataContext as ZahlungAnzeige;
        if (z == null && dataContext is Zahlung zahlung)
        {
            z = new ZahlungAnzeige
            {
                Id = zahlung.Id,
                SchuelerId = zahlung.SchuelerId,
                Betrag = zahlung.Betrag,
                Datum = zahlung.Datum
            };
        }
        if (z != null && MessageBox.Show("Zahlung löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Schueler schueler = _schuelerList.FirstOrDefault((Schueler s) => s.Id == z.SchuelerId);
            if (schueler != null)
            {
                decimal betrag = schueler.Guthaben - z.Betrag;
                Database.UpdateGuthaben(z.SchuelerId, betrag);
            }
            Database.DeleteZahlung(z.Id);
            LoadZahlungen();
            LoadSchueler();
        }
    }

    private void ZahlungenZuruecksetzen_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Alle Zahlungen zurücksetzen? Dies kann nicht rückgängig gemacht werden.", "Warnung", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes) return;
        Database.ResetZahlungen();
        foreach (Schueler schueler in _schuelerList)
        {
            Database.UpdateGuthaben(schueler.Id, 0m);
        }
        LoadZahlungen();
        LoadSchueler();
        MessageBox.Show("Zahlungen zurückgesetzt.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private void CmbSchuelerWochennotiz_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSchuelerWochennotiz.SelectedItem is ComboBoxItem { Tag: var tag } && tag is int selectedSchuelerIdWochennotiz)
        {
            _selectedSchuelerIdWochennotiz = selectedSchuelerIdWochennotiz;
            LoadWochennotizen();
        }
    }

    private void DatePickerWochennotiz_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DatePickerWochennotiz.SelectedDate.HasValue)
        {
            DateTime value = DatePickerWochennotiz.SelectedDate.Value;
            int dayOfWeek = (int)value.DayOfWeek;
            DateTime wocheStart = value.AddDays(-((dayOfWeek == 0) ? 6 : (dayOfWeek - 1)));
            _selectedWoche = wocheStart;
            TxtAktuelleWoche.Text = $"Woche vom {wocheStart:dd.MM.yyyy}";
            LoadWochennotizen();
        }
    }

    private void LoadWochennotizen()
    {
        if (_selectedSchuelerIdWochennotiz > 0)
        {
            List<WochenNotiz> wochenNotizen = Database.GetWochenNotizen(_selectedSchuelerIdWochennotiz);
            WochennotizenDataGrid.ItemsSource = wochenNotizen;
        }
    }

    private void WochennotizSpeichern_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSchuelerIdWochennotiz == 0)
        {
            MessageBox.Show("Bitte wählen Sie einen Schüler aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (string.IsNullOrWhiteSpace(TxtNeueWochennotiz.Text))
        {
            MessageBox.Show("Bitte geben Sie eine Notiz ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        Database.SaveWochenNotiz(new WochenNotiz
        {
            SchuelerId = _selectedSchuelerIdWochennotiz,
            Woche = _selectedWoche,
            Notiz = TxtNeueWochennotiz.Text.Trim()
        });
        TxtNeueWochennotiz.Text = "";
        LoadWochennotizen();
        MessageBox.Show("Notiz gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private void WochennotizLoeschen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: WochennotizAnzeige dataContext } && MessageBox.Show("Möchten Sie diese Notiz löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Database.DeleteWochenNotiz(dataContext.Id);
            LoadWochennotizen();
        }
    }

    private void LoadDrumNotationen()
    {
        DrumNotationListBox.Items.Clear();
        foreach (var item in Database.GetAllDrumNotationen(null))
        {
            ListBoxItem newItem = new ListBoxItem { Content = item.Name, Tag = item.Id };
            DrumNotationListBox.Items.Add(newItem);
        }
    }

    private void DrumNotationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrumNotationListBox.SelectedItem is ListBoxItem { Tag: var tag } && tag is int num && num > 0)
        {
            LoadDrumNotation(num);
        }
    }

    private void LoadDrumNotation(int id)
    {
        (int Id, string Name, string Inhalt, int? SchuelerId) tuple = Database.GetAllDrumNotationen(null).FirstOrDefault(x => x.Id == id);
        if (tuple.Id != 0)
        {
            _currentDrumNotationId = id;
            TxtDrumName.Text = tuple.Name;
            _drumNotationLines = tuple.Inhalt.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (_drumNotationLines.Count == 0)
            {
                _drumNotationLines = new List<string> { "1|--|--|--|--|--|--|--" };
            }
            _currentBeat = 0;
            DrawDrumNotation(tuple.Inhalt);
        }
    }

    private void DrawDrumNotation(string inhalt)
    {
        DrumStaffCanvas.Children.Clear();
        double yBase = 60.0;
        double lineSpacing = 15.0;
        for (int i = 0; i < 5; i++)
        {
            Line element = new Line { X1 = 20.0, Y1 = yBase + i * lineSpacing, X2 = 580.0, Y2 = yBase + i * lineSpacing, Stroke = Brushes.White, StrokeThickness = 1.0 };
            DrumStaffCanvas.Children.Add(element);
        }
        if (string.IsNullOrEmpty(inhalt)) return;
        string[] lines = inhalt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        double yPos = 50.0;
        List<Tuple<double, string, string, int>> notes = new List<Tuple<double, string, string, int>>();
        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            if (parts.Length < 7) continue;
            double yNote = 40.0;
            int[] noteValues = new int[7];
            for (int k = 1; k <= 7; k++)
            {
                int nv = 4;
                string text = parts[k].Trim();
                if (text != "--" && text.Contains("_"))
                {
                    string[] sub = text.Split('_');
                    if (sub.Length > 1 && int.TryParse(sub[1], out var r)) nv = r;
                }
                noteValues[k - 1] = nv;
            }
            yNote = noteValues.Max() switch { 1 => 80, 2 => 60, 4 => 50, 8 => 40, 16 => 30, _ => 50 };
            string[] instruments = new string[7] { "HH", "SD", "BD", "TT", "FT", "CR", "RD" };
            for (int l = 0; l < 7; l++)
            {
                string note = parts[l + 1].Trim();
                if (note == "--") continue;
                int noteVal = 4;
                if (note.Contains("_"))
                {
                    string[] sub = note.Split('_');
                    if (sub.Length > 1 && int.TryParse(sub[1], out var r2)) noteVal = r2;
                }
                notes.Add(Tuple.Create(yPos, note, instruments[l], noteVal));
            }
            yPos += yNote;
        }
        Dictionary<double, List<Tuple<double, string, string, int>>> grouped = new Dictionary<double, List<Tuple<double, string, string, int>>>();
        foreach (var note in notes)
        {
            if (!grouped.ContainsKey(note.Item1)) grouped[note.Item1] = new List<Tuple<double, string, string, int>>();
            grouped[note.Item1].Add(note);
        }
        HashSet<double> beamedPositions = new HashSet<double>();
        foreach (var group in grouped)
        {
            double yKey = group.Key;
            var value = group.Value;
            if (value.Count < 2) continue;
            value.Sort((a, b) =>
            {
                double posA = a.Item3 switch { "RD" => -30, "CR" => -20, "HH" => -10, "TT" => 10, "FT" => 20, "SD" => 20, "BD" => 50, _ => 0 };
                double posB = b.Item3 switch { "RD" => -30, "CR" => -20, "HH" => -10, "TT" => 10, "FT" => 20, "SD" => 20, "BD" => 50, _ => 0 };
                return posA.CompareTo(posB);
            });
            double top = value[0].Item3 switch { "RD" => -30, "CR" => -20, "HH" => -10, "TT" => 10, "FT" => 20, "SD" => 20, "BD" => 50, _ => 0 };
            double bottom = value[value.Count - 1].Item3 switch { "RD" => -30, "CR" => -20, "HH" => -10, "TT" => 10, "FT" => 20, "SD" => 20, "BD" => 50, _ => 0 };
            double stemTop = yBase + top - 25.0;
            double stemBottom = yBase + top;
            double noteBottom = yBase + bottom;
            DrumStaffCanvas.Children.Add(new Line { X1 = yKey + 5, Y1 = stemTop, X2 = yKey + 5, Y2 = stemTop, Stroke = Brushes.White, StrokeThickness = 2.0 });
            DrumStaffCanvas.Children.Add(new Line { X1 = yKey + 5, Y1 = stemBottom, X2 = yKey + 5, Y2 = stemTop, Stroke = Brushes.White, StrokeThickness = 1.0 });
            if (noteBottom != stemBottom)
            {
                DrumStaffCanvas.Children.Add(new Line { X1 = yKey + 5, Y1 = noteBottom, X2 = yKey + 5, Y2 = stemTop, Stroke = Brushes.White, StrokeThickness = 1.0 });
            }
            foreach (var note in value) beamedPositions.Add(note.Item1);
        }
        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];
            if (note.Item4 < 8) continue;
            int end = i;
            double fill = 0.0;
            for (int j = i; j < notes.Count; j++)
            {
                var n = notes[j];
                if (n.Item1 - note.Item1 >= 100) break;
                double noteFill = 4.0 / n.Item4;
                if (fill + noteFill > 1.0) break;
                fill += noteFill;
                end = j;
            }
            if (end - i >= 2)
            {
                for (int j = i; j + 1 < end && j + 1 < notes.Count; j++)
                {
                    var n1 = notes[j];
                    var n2 = notes[j + 1];
                    int noteValue = Math.Min(n1.Item4, n2.Item4);
                    DrawBeam(n1.Item1, n2.Item1, yBase, noteValue, notes[i].Item3);
                    beamedPositions.Add(n1.Item1);
                    beamedPositions.Add(n2.Item1);
                }
                i = end - 1;
            }
            else if (end - i == 1)
            {
                i = end;
            }
        }
        foreach (var note in notes)
        {
            bool isBeamed = beamedPositions.Contains(note.Item1);
            DrawNote(note.Item1, yBase, note.Item2, note.Item3, true, isBeamed);
        }
    }

    private void DrawBeam(double x1, double x2, double yBase, int noteValue, string instrument)
    {
        double yBeam = instrument switch { "RD" => yBase - 30, "CR" => yBase - 20, "HH" => yBase - 10, "TT" => yBase + 10, "FT" => yBase + 20, "SD" => yBase + 20, "BD" => yBase + 50, _ => yBase } - 25;
        double thickness = noteValue == 16 ? 10.0 : 4.0;
        Polygon beam = new Polygon
        {
            Points = new PointCollection { new Point(x1 + 5, yBeam), new Point(x2 + 5, yBeam), new Point(x2 + 5, yBeam + thickness), new Point(x1 + 5, yBeam + thickness) },
            Fill = Brushes.White
        };
        DrumStaffCanvas.Children.Add(beam);
        if (noteValue == 16)
        {
            Polygon beam2 = new Polygon
            {
                Points = new PointCollection { new Point(x1 + 5, yBeam + 6), new Point(x2 + 5, yBeam + 6), new Point(x2 + 5, yBeam + 10), new Point(x1 + 5, yBeam + 10) },
                Fill = Brushes.White
            };
            DrumStaffCanvas.Children.Add(beam2);
        }
    }

    private void DrawNote(double x, double yBase, string note, string instrument, bool drawStem, bool isBeamed)
    {
        if (note == "--" || string.IsNullOrEmpty(note)) return;
        int noteValue = 4;
        string text = note.Trim();
        if (text.Contains("_"))
        {
            string[] sub = text.Split('_');
            text = sub[0];
            if (sub.Length > 1 && int.TryParse(sub[1], out var r)) noteValue = r;
        }
        double y = instrument switch { "RD" => yBase - 30, "CR" => yBase - 20, "HH" => yBase - 10, "TT" => yBase + 10, "FT" => yBase + 20, "SD" => yBase + 20, "BD" => yBase + 50, _ => yBase };
        bool isCymbal = instrument == "HH" || instrument == "CR" || instrument == "RD";
        if (isCymbal)
        {
            TextBlock element = new TextBlock { Text = noteValue switch { 8 => "X", 4 => "X", 2 => "X", 1 => "X", _ => "X" }, Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(element, x - 5);
            Canvas.SetTop(element, y - 8);
            DrumStaffCanvas.Children.Add(element);
            if (drawStem) DrawNoteStem(x, y, noteValue, isCymbal, isBeamed, false);
        }
        else
        {
            Ellipse element = new Ellipse { Width = 12, Height = 8, Fill = Brushes.White };
            Canvas.SetLeft(element, x - 6);
            Canvas.SetTop(element, y - 4);
            DrumStaffCanvas.Children.Add(element);
            if (drawStem) DrawNoteStem(x, y, noteValue, isCymbal, isBeamed, false);
        }
    }

    private void DrawNoteStem(double x, double yPos, int noteValue, bool isCymbal, bool isBeamed, bool stemDown)
    {
        double stemX = x + 5;
        double stemTop = stemDown ? yPos : yPos - 25;
        double stemBottom = stemDown ? yPos + 20 : yPos;
        Line stem = new Line { X1 = stemX, Y1 = stemTop, X2 = stemX, Y2 = stemBottom, Stroke = Brushes.White, StrokeThickness = 1.0 };
        DrumStaffCanvas.Children.Add(stem);
        if (noteValue >= 8 && !isBeamed)
        {
            for (int i = 0; i < (noteValue == 16 ? 2 : 1); i++)
            {
                double flagY = stemTop + 5 + i * 5;
                Line flag = new Line { X1 = stemX, Y1 = flagY, X2 = stemX + 10, Y2 = flagY + 8, Stroke = Brushes.White, StrokeThickness = 1.5 };
                DrumStaffCanvas.Children.Add(flag);
            }
        }
    }

    private void DrumNotationNeu_Click(object sender, RoutedEventArgs e)
    {
        _currentDrumNotationId = 0;
        TxtDrumName.Text = "";
        _drumNotationLines = new List<string> { "1|--|--|--|--|--|--|--" };
        _currentBeat = 0;
        DrawDrumNotation(string.Join("\n", _drumNotationLines));
    }

    private void DrumNotationZeileAdd_Click(object sender, RoutedEventArgs e)
    {
        _drumNotationLines.Add("1|--|--|--|--|--|--|--");
        string inhalt = string.Join("\n", _drumNotationLines);
        DrawDrumNotation(inhalt);
    }

    private void DrumNotationZeileRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_drumNotationLines.Count > 1)
        {
            _drumNotationLines.RemoveAt(_drumNotationLines.Count - 1);
            if (_currentBeat >= _drumNotationLines.Count) _currentBeat = _drumNotationLines.Count - 1;
            string inhalt = string.Join("\n", _drumNotationLines);
            DrawDrumNotation(inhalt);
        }
    }

    private void DrumNotationSpeichern_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDrumName.Text))
        {
            MessageBox.Show("Bitte geben Sie einen Namen ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        string inhalt = string.Join("\n", _drumNotationLines);
        Database.SaveDrumNotation(_currentDrumNotationId, TxtDrumName.Text.Trim(), inhalt, null);
        LoadDrumNotationen();
        MessageBox.Show("Drum Notation gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    }

    private void DrumNotationSpeichernUnter_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDrumName.Text))
        {
            MessageBox.Show("Bitte geben Sie einen Namen ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = TxtDrumName.Text, DefaultExt = ".txt", Filter = "Textdatei (*.txt)|*.txt" };
        if (saveFileDialog.ShowDialog() == true)
        {
            string text = "Drum Notation: " + TxtDrumName.Text + "\n";
            text += "==============================\n\n";
            text += string.Join("\n", _drumNotationLines);
            System.IO.File.WriteAllText(saveFileDialog.FileName, text);
            MessageBox.Show("Drum Notation exportiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }
    }

    private void DrumNotationLoeschen_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDrumNotationId > 0 && MessageBox.Show("Möchten Sie diese Notation löschen?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Database.DeleteDrumNotation(_currentDrumNotationId);
            _currentDrumNotationId = 0;
            TxtDrumName.Text = "";
            _drumNotationLines = new List<string> { "1|--|--|--|--|--|--|--" };
            _currentBeat = 0;
            DrawDrumNotation(string.Join("\n", _drumNotationLines));
        }
    }

    private void DrumPad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && _currentBeat < _drumNotationLines.Count)
        {
            string[] parts = _drumNotationLines[_currentBeat].Split('|');
            int idx = tag switch { "HH" => 1, "SD" => 2, "BD" => 3, "TT" => 4, "FT" => 5, "CR" => 6, "RD" => 7, _ => 0 };
            if (idx >= 1 && idx < parts.Length)
            {
                parts[idx] = (_selectedNoteValue == 4) ? "X" : $"X_{_selectedNoteValue}";
            }
            _drumNotationLines[_currentBeat] = string.Join("|", parts);
            string inhalt = string.Join("\n", _drumNotationLines);
            DrawDrumNotation(inhalt);
        }
    }

    private void NextBeat_Click(object sender, RoutedEventArgs e)
    {
        _currentBeat++;
        if (_currentBeat >= _drumNotationLines.Count)
        {
            _drumNotationLines.Add("1|--|--|--|--|--|--|--");
        }
        string inhalt = string.Join("\n", _drumNotationLines);
        DrawDrumNotation(inhalt);
    }

    private void DrumNotationClear_Click(object sender, RoutedEventArgs e)
    {
        _drumNotationLines = new List<string> { "1|--|--|--|--|--|--|--" };
        _currentBeat = 0;
        DrawDrumNotation(string.Join("\n", _drumNotationLines));
    }

    private void InitializeDrumNotationTab()
    {
        LoadDrumNotationen();
        _ = InitializeGrooveScribeAsync();
    }

    private async Task InitializeGrooveScribeAsync()
    {
        try
        {
            await GrooveScribeWebView.EnsureCoreWebView2Async();
            GrooveScribeWebView.Source = new Uri(GrooveScribeUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"GrooveScribe konnte nicht geladen werden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }

    private void GrooveScribeReload_Click(object sender, RoutedEventArgs e)
    {
        if (GrooveScribeWebView.CoreWebView2 != null)
        {
            GrooveScribeWebView.Reload();
            return;
        }

        _ = InitializeGrooveScribeAsync();
    }

    private void GrooveScribeOpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GrooveScribeUrl,
            UseShellExecute = true
        });
    }

    private void NoteValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && int.TryParse(tag, out var result))
        {
            _selectedNoteValue = result;
            SelectedNoteValueText.Text = $"Ausgewählt: 1/{result}";
        }
    }
}
