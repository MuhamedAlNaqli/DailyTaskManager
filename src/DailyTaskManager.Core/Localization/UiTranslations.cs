using System;

namespace DailyTaskManager.Core.Localization
{
    /// <summary>Translations of the fixed texts in the windows and pages (columns: bs, hr, sr, de, tr)</summary>
    internal static class UiTranslations
    {
        public static void Register(Action<string, string, string, string, string, string> a)
        {
            // Navigation and shell
            a("Daily Tasks", "Dnevni zadaci", "Dnevni zadaci", "Dnevni zadaci", "Tagesaufgaben", "Günlük Görevler");
            a("Calendar", "Kalendar", "Kalendar", "Kalendar", "Kalender", "Takvim");
            a("Analytics", "Analitika", "Analitika", "Analitika", "Analysen", "Analizler");
            a("Reports", "Izvještaji", "Izvješća", "Izveštaji", "Berichte", "Raporlar");
            a("Settings", "Postavke", "Postavke", "Podešavanja", "Einstellungen", "Ayarlar");
            a("Help", "Pomoć", "Pomoć", "Pomoć", "Hilfe", "Yardım");
            a("Logout", "Odjava", "Odjava", "Odjava", "Abmelden", "Çıkış yap");
            a("Light Mode", "Svijetla tema", "Svijetla tema", "Svetla tema", "Helles Design", "Açık tema");
            a("Dark Mode", "Tamna tema", "Tamna tema", "Tamna tema", "Dunkles Design", "Koyu tema");
            a("Version {0}", "Verzija {0}", "Verzija {0}", "Verzija {0}", "Version {0}", "Sürüm {0}");
            a("Welcome, User", "Dobrodošli, korisniče", "Dobrodošli, korisniče", "Dobrodošli, korisniče", "Willkommen, Benutzer", "Hoş geldiniz, kullanıcı");
            a("Welcome, {0}", "Dobrodošli, {0}", "Dobrodošli, {0}", "Dobrodošli, {0}", "Willkommen, {0}", "Hoş geldiniz, {0}");
            a("Welcome, Guest", "Dobrodošli, gostu", "Dobrodošli, gostu", "Dobrodošli, gostu", "Willkommen, Gast", "Hoş geldiniz, misafir");
            a("Dismiss", "Odbaci", "Odbaci", "Odbaci", "Schließen", "Kapat");
            a("Loading...", "Učitavanje...", "Učitavanje...", "Učitavanje...", "Wird geladen...", "Yükleniyor...");
            a("Loading tasks...", "Učitavanje zadataka...", "Učitavanje zadataka...", "Učitavanje zadataka...", "Aufgaben werden geladen...", "Görevler yükleniyor...");
            a("Loading calendar...", "Učitavanje kalendara...", "Učitavanje kalendara...", "Učitavanje kalendara...", "Kalender wird geladen...", "Takvim yükleniyor...");
            a("Loading analytics...", "Učitavanje analitike...", "Učitavanje analitike...", "Učitavanje analitike...", "Analysen werden geladen...", "Analizler yükleniyor...");
            a("Loading reports...", "Učitavanje izvještaja...", "Učitavanje izvješća...", "Učitavanje izveštaja...", "Berichte werden geladen...", "Raporlar yükleniyor...");
            a("Loading settings...", "Učitavanje postavki...", "Učitavanje postavki...", "Učitavanje podešavanja...", "Einstellungen werden geladen...", "Ayarlar yükleniyor...");
            a("Logging out...", "Odjavljivanje...", "Odjavljivanje...", "Odjavljivanje...", "Abmeldung läuft...", "Çıkış yapılıyor...");
            a("Loading your time entries...", "Učitavanje vaših unosa...", "Učitavanje vaših unosa...", "Učitavanje vaših unosa...", "Ihre Zeiteinträge werden geladen...", "Zaman kayıtlarınız yükleniyor...");
            a("Saving settings...", "Spremanje postavki...", "Spremanje postavki...", "Čuvanje podešavanja...", "Einstellungen werden gespeichert...", "Ayarlar kaydediliyor...");
            a("Daily Task Manager", "Dnevni menadžer zadataka", "Dnevni upravitelj zadataka", "Dnevni menadžer zadataka", "Tagesaufgaben-Manager", "Günlük Görev Yöneticisi");
            a("Daily Task Manager - Login", "Dnevni menadžer zadataka - Prijava", "Dnevni upravitelj zadataka - Prijava", "Dnevni menadžer zadataka - Prijava", "Tagesaufgaben-Manager - Anmeldung", "Günlük Görev Yöneticisi - Giriş");

            // Login
            a("Sign in to your account", "Prijavite se na svoj račun", "Prijavite se na svoj račun", "Prijavite se na svoj nalog", "Melden Sie sich bei Ihrem Konto an", "Hesabınıza giriş yapın");
            a("Username", "Korisničko ime", "Korisničko ime", "Korisničko ime", "Benutzername", "Kullanıcı adı");
            a("Enter your username", "Unesite korisničko ime", "Unesite korisničko ime", "Unesite korisničko ime", "Benutzernamen eingeben", "Kullanıcı adınızı girin");
            a("Password", "Lozinka", "Lozinka", "Lozinka", "Passwort", "Parola");
            a("Enter your password", "Unesite lozinku", "Unesite lozinku", "Unesite lozinku", "Passwort eingeben", "Parolanızı girin");
            a("Remember me", "Zapamti me", "Zapamti me", "Zapamti me", "Angemeldet bleiben", "Beni hatırla");
            a("Sign In", "Prijava", "Prijava", "Prijava", "Anmelden", "Giriş yap");
            a("Signing in...", "Prijavljivanje...", "Prijavljivanje...", "Prijavljivanje...", "Anmeldung läuft...", "Giriş yapılıyor...");
            a("Or continue with", "Ili nastavite sa", "Ili nastavite s", "Ili nastavite sa", "Oder weiter mit", "Veya şununla devam edin");
            a("Continue with Google", "Nastavi putem Googlea", "Nastavi putem Googlea", "Nastavi preko Google-a", "Weiter mit Google", "Google ile devam et");
            a("Don't have an account?", "Nemate račun?", "Nemate račun?", "Nemate nalog?", "Noch kein Konto?", "Hesabınız yok mu?");
            a("Create Account", "Kreiraj račun", "Izradi račun", "Napravi nalog", "Konto erstellen", "Hesap oluştur");

            // Daily Tasks page
            a("Time Tracking", "Evidencija vremena", "Evidencija vremena", "Evidencija vremena", "Zeiterfassung", "Zaman Takibi");
            a("Track and manage your daily work hours", "Pratite i uređujte svoje dnevne radne sate", "Pratite i uređujte svoje dnevne radne sate", "Pratite i uređujte svoje dnevne radne sate", "Erfassen und verwalten Sie Ihre täglichen Arbeitsstunden", "Günlük çalışma saatlerinizi takip edin ve yönetin");
            a("Week", "Sedmica", "Tjedan", "Sedmica", "Woche", "Hafta");
            a("List", "Lista", "Popis", "Lista", "Liste", "Liste");
            a("Quick add", "Brzi unos", "Brzi unos", "Brzi unos", "Schnell hinzufügen", "Hızlı ekle");
            a("Quick add for today", "Brzi unos za danas", "Brzi unos za danas", "Brzi unos za danas", "Schnell für heute hinzufügen", "Bugün için hızlı ekle");
            a("Delete template", "Obriši šablon", "Izbriši predložak", "Obriši šablon", "Vorlage löschen", "Şablonu sil");
            a("No templates yet. Tick \"Save as a template\" when adding an entry.", "Još nema šablona. Označite \"Sačuvaj kao šablon\" pri dodavanju unosa.", "Još nema predložaka. Označite \"Spremi kao predložak\" pri dodavanju unosa.", "Još nema šablona. Označite \"Sačuvaj kao šablon\" pri dodavanju unosa.", "Noch keine Vorlagen. Aktivieren Sie beim Hinzufügen eines Eintrags \"Als Vorlage speichern\".", "Henüz şablon yok. Kayıt eklerken \"Şablon olarak kaydet\" seçeneğini işaretleyin.");
            a("Add Entry", "Dodaj unos", "Dodaj unos", "Dodaj unos", "Eintrag hinzufügen", "Kayıt ekle");
            a("Export", "Izvoz", "Izvoz", "Izvoz", "Exportieren", "Dışa aktar");
            a("Excel", "Excel", "Excel", "Excel", "Excel", "Excel");
            a("Target: 7h", "Cilj: 7h", "Cilj: 7h", "Cilj: 7h", "Ziel: 7h", "Hedef: 7sa");
            a("Target: 35h", "Cilj: 35h", "Cilj: 35h", "Cilj: 35h", "Ziel: 35h", "Hedef: 35sa");
            a("Today", "Danas", "Danas", "Danas", "Heute", "Bugün");
            a("This Week", "Ova sedmica", "Ovaj tjedan", "Ova sedmica", "Diese Woche", "Bu hafta");
            a("Total Entries", "Ukupno unosa", "Ukupno unosa", "Ukupno unosa", "Einträge gesamt", "Toplam kayıt");
            a("Matching filters", "Odgovara filterima", "Odgovara filtrima", "Odgovara filterima", "Passend zu den Filtern", "Filtrelere uyan");
            a("Daily Avg", "Dnevni prosjek", "Dnevni prosjek", "Dnevni prosek", "Tagesdurchschnitt", "Günlük ort.");
            a("Last 30 days", "Zadnjih 30 dana", "Zadnjih 30 dana", "Poslednjih 30 dana", "Letzte 30 Tage", "Son 30 gün");
            a("Previous week", "Prethodna sedmica", "Prethodni tjedan", "Prethodna sedmica", "Vorherige Woche", "Önceki hafta");
            a("Next week", "Sljedeća sedmica", "Sljedeći tjedan", "Sledeća sedmica", "Nächste Woche", "Sonraki hafta");
            a("This week", "Ova sedmica", "Ovaj tjedan", "Ova sedmica", "Diese Woche", "Bu hafta");
            a("Week total: {0}", "Ukupno za sedmicu: {0}", "Ukupno za tjedan: {0}", "Ukupno za sedmicu: {0}", "Wochensumme: {0}", "Haftalık toplam: {0}");
            a("Add an entry for this day", "Dodaj unos za ovaj dan", "Dodaj unos za ovaj dan", "Dodaj unos za ovaj dan", "Eintrag für diesen Tag hinzufügen", "Bu gün için kayıt ekle");
            a("Add", "Dodaj", "Dodaj", "Dodaj", "Hinzufügen", "Ekle");
            a("Duplicate to today", "Dupliciraj za danas", "Dupliciraj za danas", "Dupliciraj za danas", "Auf heute duplizieren", "Bugüne kopyala");
            a("Delete entry", "Obriši unos", "Izbriši unos", "Obriši unos", "Eintrag löschen", "Kaydı sil");
            a("Filters", "Filteri", "Filtri", "Filteri", "Filter", "Filtreler");
            a("Search by task description (min 3 characters)...", "Pretraga po opisu zadatka (najmanje 3 znaka)...", "Pretraga po opisu zadatka (najmanje 3 znaka)...", "Pretraga po opisu zadatka (najmanje 3 znaka)...", "Nach Aufgabenbeschreibung suchen (mind. 3 Zeichen)...", "Görev açıklamasına göre ara (en az 3 karakter)...");
            a("Select Date Range", "Odaberi raspon datuma", "Odaberi raspon datuma", "Izaberi opseg datuma", "Zeitraum wählen", "Tarih aralığı seç");
            a("Click start date, then click end date", "Kliknite početni datum, zatim završni datum", "Kliknite početni datum, zatim završni datum", "Kliknite početni datum, zatim krajnji datum", "Startdatum anklicken, dann Enddatum anklicken", "Önce başlangıç, sonra bitiş tarihine tıklayın");
            a("Clear All Filters", "Očisti sve filtere", "Očisti sve filtre", "Očisti sve filtere", "Alle Filter zurücksetzen", "Tüm filtreleri temizle");
            a("Apply", "Primijeni", "Primijeni", "Primeni", "Anwenden", "Uygula");
            a("Assign client", "Dodijeli klijenta", "Dodijeli klijenta", "Dodeli klijenta", "Kunde zuweisen", "Müşteri ata");
            a("Applies to every entry matching the current search, date range and client filter (all pages).", "Odnosi se na svaki unos koji odgovara trenutnoj pretrazi, rasponu datuma i filteru klijenta (sve stranice).", "Odnosi se na svaki unos koji odgovara trenutnoj pretrazi, rasponu datuma i filtru klijenta (sve stranice).", "Odnosi se na svaki unos koji odgovara trenutnoj pretrazi, opsegu datuma i filteru klijenta (sve strane).", "Gilt für jeden Eintrag, der zur aktuellen Suche, zum Zeitraum und zum Kundenfilter passt (alle Seiten).", "Geçerli arama, tarih aralığı ve müşteri filtresine uyan tüm kayıtlara uygulanır (tüm sayfalar).");
            a("Assign to matching entries", "Dodijeli odgovarajućim unosima", "Dodijeli odgovarajućim unosima", "Dodeli odgovarajućim unosima", "Passenden Einträgen zuweisen", "Uyan kayıtlara ata");
            a("Date", "Datum", "Datum", "Datum", "Datum", "Tarih");
            a("Description", "Opis", "Opis", "Opis", "Beschreibung", "Açıklama");
            a("Hours", "Sati", "Sati", "Sati", "Stunden", "Saat");
            a("Actions", "Radnje", "Radnje", "Radnje", "Aktionen", "İşlemler");
            a("Double-click to edit", "Dvoklik za uređivanje", "Dvoklik za uređivanje", "Dvoklik za izmenu", "Doppelklick zum Bearbeiten", "Düzenlemek için çift tıklayın");
            a("Edit task", "Uredi zadatak", "Uredi zadatak", "Izmeni zadatak", "Aufgabe bearbeiten", "Görevi düzenle");
            a("Delete task", "Obriši zadatak", "Izbriši zadatak", "Obriši zadatak", "Aufgabe löschen", "Görevi sil");
            a("First page", "Prva stranica", "Prva stranica", "Prva strana", "Erste Seite", "İlk sayfa");
            a("Last page", "Zadnja stranica", "Zadnja stranica", "Poslednja strana", "Letzte Seite", "Son sayfa");
            a("‹ Previous", "‹ Prethodna", "‹ Prethodna", "‹ Prethodna", "‹ Zurück", "‹ Önceki");
            a("Next ›", "Sljedeća ›", "Sljedeća ›", "Sledeća ›", "Weiter ›", "Sonraki ›");
            a("No time entries found", "Nema pronađenih unosa", "Nema pronađenih unosa", "Nema pronađenih unosa", "Keine Zeiteinträge gefunden", "Zaman kaydı bulunamadı");
            a("Start tracking your time by adding your first entry", "Počnite pratiti vrijeme dodavanjem prvog unosa", "Počnite pratiti vrijeme dodavanjem prvog unosa", "Počnite da pratite vreme dodavanjem prvog unosa", "Beginnen Sie mit Ihrem ersten Eintrag", "İlk kaydınızı ekleyerek zamanınızı takip etmeye başlayın");
            a("Add Your First Entry", "Dodaj prvi unos", "Dodaj prvi unos", "Dodaj prvi unos", "Ersten Eintrag hinzufügen", "İlk kaydı ekle");
            a("Undo", "Poništi", "Poništi", "Poništi", "Rückgängig", "Geri al");
            a("Ctrl+Enter or click away to save, Esc to cancel", "Ctrl+Enter ili klik izvan polja za spremanje, Esc za otkazivanje", "Ctrl+Enter ili klik izvan polja za spremanje, Esc za odustajanje", "Ctrl+Enter ili klik van polja za čuvanje, Esc za otkazivanje", "Strg+Eingabe oder Klick daneben zum Speichern, Esc zum Abbrechen", "Kaydetmek için Ctrl+Enter veya dışarı tıklayın, iptal için Esc");
            a("Enter or click away to save, Esc to cancel", "Enter ili klik izvan polja za spremanje, Esc za otkazivanje", "Enter ili klik izvan polja za spremanje, Esc za odustajanje", "Enter ili klik van polja za čuvanje, Esc za otkazivanje", "Eingabe oder Klick daneben zum Speichern, Esc zum Abbrechen", "Kaydetmek için Enter veya dışarı tıklayın, iptal için Esc");
            a("{0} → {1}", "{0} → {1}", "{0} → {1}", "{0} → {1}", "{0} → {1}", "{0} → {1}");

            // Entry dialogs
            a("Add New Task", "Dodaj novi zadatak", "Dodaj novi zadatak", "Dodaj novi zadatak", "Neue Aufgabe hinzufügen", "Yeni görev ekle");
            a("Add New Daily Task", "Dodaj novi dnevni zadatak", "Dodaj novi dnevni zadatak", "Dodaj novi dnevni zadatak", "Neue Tagesaufgabe hinzufügen", "Yeni günlük görev ekle");
            a("Edit Task", "Uredi zadatak", "Uredi zadatak", "Izmeni zadatak", "Aufgabe bearbeiten", "Görevi düzenle");
            a("Edit Daily Task", "Uredi dnevni zadatak", "Uredi dnevni zadatak", "Izmeni dnevni zadatak", "Tagesaufgabe bearbeiten", "Günlük görevi düzenle");
            a("Template", "Šablon", "Predložak", "Šablon", "Vorlage", "Şablon");
            a("Start from a saved template (optional)", "Počni od sačuvanog šablona (nije obavezno)", "Počni od spremljenog predloška (nije obavezno)", "Počni od sačuvanog šablona (nije obavezno)", "Mit gespeicherter Vorlage beginnen (optional)", "Kayıtlı bir şablonla başla (isteğe bağlı)");
            a("Description *", "Opis *", "Opis *", "Opis *", "Beschreibung *", "Açıklama *");
            a("Dictate the description with your microphone", "Diktirajte opis mikrofonom", "Diktirajte opis mikrofonom", "Diktirajte opis mikrofonom", "Beschreibung per Mikrofon diktieren", "Açıklamayı mikrofonla dikte edin");
            a("Enter task description (supports multiple lines)", "Unesite opis zadatka (podržava više redova)", "Unesite opis zadatka (podržava više redaka)", "Unesite opis zadatka (podržava više redova)", "Aufgabenbeschreibung eingeben (mehrere Zeilen möglich)", "Görev açıklamasını girin (birden çok satır desteklenir)");
            a("Dictate", "Diktiraj", "Diktiraj", "Diktiraj", "Diktieren", "Dikte et");
            a("Stop", "Zaustavi", "Zaustavi", "Zaustavi", "Stopp", "Durdur");
            a("Date *", "Datum *", "Datum *", "Datum *", "Datum *", "Tarih *");
            a("Hours *", "Sati *", "Sati *", "Sati *", "Stunden *", "Saat *");
            a("Client", "Klijent", "Klijent", "Klijent", "Kunde", "Müşteri");
            a("Or add a new client", "Ili dodajte novog klijenta", "Ili dodajte novog klijenta", "Ili dodajte novog klijenta", "Oder neuen Kunden hinzufügen", "Veya yeni müşteri ekleyin");
            a("Client name", "Naziv klijenta", "Naziv klijenta", "Naziv klijenta", "Kundenname", "Müşteri adı");
            a("Save as a template for quick add", "Sačuvaj kao šablon za brzi unos", "Spremi kao predložak za brzi unos", "Sačuvaj kao šablon za brzi unos", "Als Vorlage für schnelles Hinzufügen speichern", "Hızlı ekleme için şablon olarak kaydet");
            a("Template name (optional)", "Naziv šablona (nije obavezno)", "Naziv predloška (nije obavezno)", "Naziv šablona (nije obavezno)", "Vorlagenname (optional)", "Şablon adı (isteğe bağlı)");
            a("Cancel", "Otkaži", "Odustani", "Otkaži", "Abbrechen", "İptal");
            a("Add Task", "Dodaj zadatak", "Dodaj zadatak", "Dodaj zadatak", "Aufgabe hinzufügen", "Görev ekle");
            a("Save Changes", "Sačuvaj promjene", "Spremi promjene", "Sačuvaj izmene", "Änderungen speichern", "Değişiklikleri kaydet");
            a("No client", "Bez klijenta", "Bez klijenta", "Bez klijenta", "Kein Kunde", "Müşteri yok");
            a("All clients", "Svi klijenti", "Svi klijenti", "Svi klijenti", "Alle Kunden", "Tüm müşteriler");
            a("Unassigned", "Nedodijeljeno", "Nedodijeljeno", "Nedodeljeno", "Nicht zugewiesen", "Atanmamış");

            // Calendar
            a("Mon", "Pon", "Pon", "Pon", "Mo", "Pzt");
            a("Tue", "Uto", "Uto", "Uto", "Di", "Sal");
            a("Wed", "Sri", "Sri", "Sre", "Mi", "Çar");
            a("Thu", "Čet", "Čet", "Čet", "Do", "Per");
            a("Fri", "Pet", "Pet", "Pet", "Fr", "Cum");
            a("Sat", "Sub", "Sub", "Sub", "Sa", "Cmt");
            a("Sun", "Ned", "Ned", "Ned", "So", "Paz");
            a("Previous month", "Prethodni mjesec", "Prethodni mjesec", "Prethodni mesec", "Vorheriger Monat", "Önceki ay");
            a("Next month", "Sljedeći mjesec", "Sljedeći mjesec", "Sledeći mesec", "Nächster Monat", "Sonraki ay");
            a("7h or more", "7h ili više", "7h ili više", "7h ili više", "7h oder mehr", "7sa veya daha fazla");
            a("under 7h", "manje od 7h", "manje od 7h", "manje od 7h", "unter 7h", "7sa'ten az");
            a("working day without an entry", "radni dan bez unosa", "radni dan bez unosa", "radni dan bez unosa", "Arbeitstag ohne Eintrag", "kaydı olmayan iş günü");
            a("Click a day to add an entry, or to edit the one it already has.", "Kliknite na dan da dodate unos ili uredite postojeći.", "Kliknite na dan da dodate unos ili uredite postojeći.", "Kliknite na dan da dodate unos ili izmenite postojeći.", "Klicken Sie auf einen Tag, um einen Eintrag hinzuzufügen oder den vorhandenen zu bearbeiten.", "Kayıt eklemek veya mevcut kaydı düzenlemek için bir güne tıklayın.");

            // Analytics
            a("See where your hours go", "Pogledajte kamo idu vaši sati", "Pogledajte kamo idu vaši sati", "Pogledajte kuda idu vaši sati", "Sehen Sie, wohin Ihre Stunden fließen", "Saatlerinizin nereye gittiğini görün");
            a("Total hours", "Ukupno sati", "Ukupno sati", "Ukupno sati", "Gesamtstunden", "Toplam saat");
            a("Days worked", "Radnih dana", "Radnih dana", "Radnih dana", "Gearbeitete Tage", "Çalışılan gün");
            a("with at least one entry", "s barem jednim unosom", "s barem jednim unosom", "sa barem jednim unosom", "mit mindestens einem Eintrag", "en az bir kaydı olan");
            a("Average per day", "Prosjek po danu", "Prosjek po danu", "Prosek po danu", "Durchschnitt pro Tag", "Günlük ortalama");
            a("on days worked", "na radne dane", "na radne dane", "na radne dane", "an Arbeitstagen", "çalışılan günlerde");
            a("Target (7h per working day)", "Cilj (7h po radnom danu)", "Cilj (7h po radnom danu)", "Cilj (7h po radnom danu)", "Ziel (7h pro Arbeitstag)", "Hedef (iş günü başına 7sa)");
            a("missing", "nedostaje", "nedostaje", "nedostaje", "fehlt", "eksik");
            a("No entries in this period", "Nema unosa u ovom razdoblju", "Nema unosa u ovom razdoblju", "Nema unosa u ovom periodu", "Keine Einträge in diesem Zeitraum", "Bu dönemde kayıt yok");
            a("Hours by client", "Sati po klijentu", "Sati po klijentu", "Sati po klijentu", "Stunden pro Kunde", "Müşteriye göre saatler");
            a("Assign clients to your entries to see a breakdown here.", "Dodijelite klijente svojim unosima da ovdje vidite raspodjelu.", "Dodijelite klijente svojim unosima da ovdje vidite raspodjelu.", "Dodelite klijente svojim unosima da ovde vidite raspodelu.", "Weisen Sie Ihren Einträgen Kunden zu, um hier eine Aufschlüsselung zu sehen.", "Burada dökümü görmek için kayıtlarınıza müşteri atayın.");
            a("This month", "Ovaj mjesec", "Ovaj mjesec", "Ovaj mesec", "Dieser Monat", "Bu ay");
            a("Last month", "Prošli mjesec", "Prošli mjesec", "Prošli mesec", "Letzter Monat", "Geçen ay");
            a("Last 3 months", "Zadnja 3 mjeseca", "Zadnja 3 mjeseca", "Poslednja 3 meseca", "Letzte 3 Monate", "Son 3 ay");
            a("This year", "Ova godina", "Ova godina", "Ova godina", "Dieses Jahr", "Bu yıl");
            a("All time", "Cijelo vrijeme", "Cijelo vrijeme", "Sve vreme", "Gesamte Zeit", "Tüm zamanlar");
            a("0 entries", "0 unosa", "0 unosa", "0 unosa", "0 Einträge", "0 kayıt");
            a("1 entry", "1 unos", "1 unos", "1 unos", "1 Eintrag", "1 kayıt");
            a("{0} entries", "{0} unosa", "{0} unosa", "{0} unosa", "{0} Einträge", "{0} kayıt");
            a("Hours per day", "Sati po danu", "Sati po danu", "Sati po danu", "Stunden pro Tag", "Günlük saatler");
            a("Hours per month", "Sati po mjesecu", "Sati po mjesecu", "Sati po mesecu", "Stunden pro Monat", "Aylık saatler");
            a("No working days yet", "Još nema radnih dana", "Još nema radnih dana", "Još nema radnih dana", "Noch keine Arbeitstage", "Henüz iş günü yok");
            a("target reached", "cilj ostvaren", "cilj ostvaren", "cilj ostvaren", "Ziel erreicht", "hedefe ulaşıldı");
            a("below target", "ispod cilja", "ispod cilja", "ispod cilja", "unter dem Ziel", "hedefin altında");
            a("no entry", "bez unosa", "bez unosa", "bez unosa", "kein Eintrag", "kayıt yok");
            a("{0}h ahead of {1} working days", "{0}h ispred plana ({1} radnih dana)", "{0}h ispred plana ({1} radnih dana)", "{0}h ispred plana ({1} radnih dana)", "{0}h voraus bei {1} Arbeitstagen", "{1} iş gününde {0}sa ileride");
            a("{0}h behind over {1} working days", "{0}h iza plana ({1} radnih dana)", "{0}h iza plana ({1} radnih dana)", "{0}h iza plana ({1} radnih dana)", "{0}h im Rückstand bei {1} Arbeitstagen", "{1} iş gününde {0}sa geride");
            a("{0}h logged on {1} days", "{0}h evidentirano u {1} dana", "{0}h evidentirano u {1} dana", "{0}h evidentirano u {1} dana", "{0}h an {1} Tagen erfasst", "{1} günde {0}sa kaydedildi");
            a("{0}h logged on 1 day", "{0}h evidentirano u 1 dan", "{0}h evidentirano u 1 dan", "{0}h evidentirano u 1 dan", "{0}h an 1 Tag erfasst", "1 günde {0}sa kaydedildi");

            // Reports
            a("Create work orders and invoices for a month", "Izradite radne naloge i račune za mjesec", "Izradite radne naloge i račune za mjesec", "Napravite radne naloge i fakture za mesec", "Arbeitsaufträge und Rechnungen für einen Monat erstellen", "Bir ay için iş emirleri ve faturalar oluşturun");
            a("Work order", "Radni nalog", "Radni nalog", "Radni nalog", "Arbeitsauftrag", "İş emri");
            a("Month", "Mjesec", "Mjesec", "Mesec", "Monat", "Ay");
            a("Export PDF", "Izvezi PDF", "Izvezi PDF", "Izvezi PDF", "PDF exportieren", "PDF dışa aktar");
            a("Export Word", "Izvezi Word", "Izvezi Word", "Izvezi Word", "Word exportieren", "Word dışa aktar");
            a("Export Excel", "Izvezi Excel", "Izvezi Excel", "Izvezi Excel", "Excel exportieren", "Excel dışa aktar");
            a("Header, company and recipient details come from Settings. The files are saved to your configured export folders.", "Zaglavlje, podaci o firmi i primatelju dolaze iz postavki. Datoteke se spremaju u vaše postavljene mape za izvoz.", "Zaglavlje, podaci o tvrtki i primatelju dolaze iz postavki. Datoteke se spremaju u vaše postavljene mape za izvoz.", "Zaglavlje, podaci o firmi i primaocu dolaze iz podešavanja. Datoteke se čuvaju u vašim podešenim fasciklama za izvoz.", "Kopfzeile, Firmen- und Empfängerdaten stammen aus den Einstellungen. Die Dateien werden in Ihren konfigurierten Exportordnern gespeichert.", "Üst bilgi, firma ve alıcı bilgileri Ayarlar'dan gelir. Dosyalar yapılandırdığınız dışa aktarma klasörlerine kaydedilir.");
            a("Invoice", "Račun", "Račun", "Faktura", "Rechnung", "Fatura");
            a("Invoices the hours of the month and client selected above.", "Fakturira sate odabranog mjeseca i klijenta.", "Fakturira sate odabranog mjeseca i klijenta.", "Fakturiše sate izabranog meseca i klijenta.", "Berechnet die Stunden des oben gewählten Monats und Kunden.", "Yukarıda seçilen ay ve müşterinin saatlerini faturalandırır.");
            a("Invoice number", "Broj računa", "Broj računa", "Broj fakture", "Rechnungsnummer", "Fatura numarası");
            a("Issue date", "Datum izdavanja", "Datum izdavanja", "Datum izdavanja", "Ausstellungsdatum", "Düzenleme tarihi");
            a("Currency", "Valuta", "Valuta", "Valuta", "Währung", "Para birimi");
            a("Hourly rate", "Cijena sata", "Cijena sata", "Cena sata", "Stundensatz", "Saatlik ücret");
            a("VAT % (0 = no VAT)", "PDV % (0 = bez PDV-a)", "PDV % (0 = bez PDV-a)", "PDV % (0 = bez PDV-a)", "MwSt. % (0 = keine MwSt.)", "KDV % (0 = KDV yok)");
            a("Payment note (bank account, deadline...)", "Napomena o plaćanju (račun, rok...)", "Napomena o plaćanju (račun, rok...)", "Napomena o plaćanju (račun, rok...)", "Zahlungshinweis (Bankkonto, Frist...)", "Ödeme notu (banka hesabı, vade...)");
            a("Net (", "Neto (", "Neto (", "Neto (", "Netto (", "Net (");
            a("VAT", "PDV", "PDV", "PDV", "MwSt.", "KDV");
            a("Total", "Ukupno", "Ukupno", "Ukupno", "Gesamt", "Toplam");
            a("Create invoice (PDF)", "Izradi račun (PDF)", "Izradi račun (PDF)", "Napravi fakturu (PDF)", "Rechnung erstellen (PDF)", "Fatura oluştur (PDF)");
            a("No entries for this selection", "Nema unosa za ovaj odabir", "Nema unosa za ovaj odabir", "Nema unosa za ovaj izbor", "Keine Einträge für diese Auswahl", "Bu seçim için kayıt yok");

            // Settings
            a("Configure application preferences and PDF export settings", "Podesite postavke aplikacije i izvoza u PDF", "Podesite postavke aplikacije i izvoza u PDF", "Podesite podešavanja aplikacije i izvoza u PDF", "Anwendungseinstellungen und PDF-Export konfigurieren", "Uygulama tercihlerini ve PDF dışa aktarma ayarlarını yapılandırın");
            a("Language", "Jezik", "Jezik", "Jezik", "Sprache", "Dil");
            a("Choose the language of the application. The change applies immediately.", "Odaberite jezik aplikacije. Promjena se primjenjuje odmah.", "Odaberite jezik aplikacije. Promjena se primjenjuje odmah.", "Izaberite jezik aplikacije. Promena se primenjuje odmah.", "Wählen Sie die Sprache der Anwendung. Die Änderung gilt sofort.", "Uygulamanın dilini seçin. Değişiklik hemen uygulanır.");
            a("Interface language", "Jezik sučelja", "Jezik sučelja", "Jezik interfejsa", "Oberflächensprache", "Arayüz dili");
            a("PDF Export Customization", "Prilagodba PDF izvoza", "Prilagodba PDF izvoza", "Prilagođavanje PDF izvoza", "PDF-Export anpassen", "PDF dışa aktarma özelleştirmesi");
            a("Save PDF Settings", "Sačuvaj PDF postavke", "Spremi PDF postavke", "Sačuvaj PDF podešavanja", "PDF-Einstellungen speichern", "PDF ayarlarını kaydet");
            a("Header Settings", "Postavke zaglavlja", "Postavke zaglavlja", "Podešavanja zaglavlja", "Kopfzeilen-Einstellungen", "Üst bilgi ayarları");
            a("Company Name", "Naziv firme", "Naziv tvrtke", "Naziv firme", "Firmenname", "Firma adı");
            a("Company ID", "ID firme", "OIB/ID tvrtke", "ID firme", "Firmen-ID", "Firma kimliği");
            a("Address Line 1", "Adresa, red 1", "Adresa, redak 1", "Adresa, red 1", "Adresszeile 1", "Adres satırı 1");
            a("Address Line 2", "Adresa, red 2", "Adresa, redak 2", "Adresa, red 2", "Adresszeile 2", "Adres satırı 2");
            a("Phone", "Telefon", "Telefon", "Telefon", "Telefon", "Telefon");
            a("Email", "E-pošta", "E-pošta", "E-pošta", "E-Mail", "E-posta");
            a("Client and Document Settings", "Postavke klijenta i dokumenta", "Postavke klijenta i dokumenta", "Podešavanja klijenta i dokumenta", "Kunden- und Dokumenteinstellungen", "Müşteri ve belge ayarları");
            a("Client Name", "Naziv klijenta", "Naziv klijenta", "Naziv klijenta", "Kundenname", "Müşteri adı");
            a("Client ID", "ID klijenta", "ID klijenta", "ID klijenta", "Kunden-ID", "Müşteri kimliği");
            a("Document Title", "Naslov dokumenta", "Naslov dokumenta", "Naslov dokumenta", "Dokumenttitel", "Belge başlığı");
            a("Contract Number Prefix", "Prefiks broja ugovora", "Prefiks broja ugovora", "Prefiks broja ugovora", "Präfix der Vertragsnummer", "Sözleşme numarası öneki");
            a("Decision Number", "Broj odluke", "Broj odluke", "Broj odluke", "Beschlussnummer", "Karar numarası");
            a("Client VAT Number", "PDV broj klijenta", "PDV broj klijenta", "PDV broj klijenta", "USt-IdNr. des Kunden", "Müşteri KDV numarası");
            a("Client Address Line 1", "Adresa klijenta, red 1", "Adresa klijenta, redak 1", "Adresa klijenta, red 1", "Kundenadresse, Zeile 1", "Müşteri adresi, satır 1");
            a("Client Address Line 2", "Adresa klijenta, red 2", "Adresa klijenta, redak 2", "Adresa klijenta, red 2", "Kundenadresse, Zeile 2", "Müşteri adresi, satır 2");
            a("Show Company Logo Border", "Prikaži okvir logotipa firme", "Prikaži okvir logotipa tvrtke", "Prikaži okvir logotipa firme", "Rahmen um das Firmenlogo anzeigen", "Firma logosu çerçevesini göster");
            a("Include Signature Section", "Uključi odjeljak za potpis", "Uključi odjeljak za potpis", "Uključi odeljak za potpis", "Unterschriftsbereich einfügen", "İmza bölümünü ekle");
            a("Preview PDF", "Pregled PDF-a", "Pregled PDF-a", "Pregled PDF-a", "PDF-Vorschau", "PDF önizleme");
            a("Application Settings (Read-only)", "Postavke aplikacije (samo za čitanje)", "Postavke aplikacije (samo za čitanje)", "Podešavanja aplikacije (samo za čitanje)", "Anwendungseinstellungen (schreibgeschützt)", "Uygulama ayarları (salt okunur)");
            a("Key", "Ključ", "Ključ", "Ključ", "Schlüssel", "Anahtar");
            a("Value", "Vrijednost", "Vrijednost", "Vrednost", "Wert", "Değer");
            a("Export Settings", "Postavke izvoza", "Postavke izvoza", "Podešavanja izvoza", "Exporteinstellungen", "Dışa aktarma ayarları");
            a("PDF Export Location", "Lokacija za PDF izvoz", "Lokacija za PDF izvoz", "Lokacija za PDF izvoz", "PDF-Exportordner", "PDF dışa aktarma konumu");
            a("Select folder path for PDF exports", "Odaberite putanju mape za PDF izvoz", "Odaberite putanju mape za PDF izvoz", "Izaberite putanju fascikle za PDF izvoz", "Ordnerpfad für PDF-Exporte wählen", "PDF dışa aktarmaları için klasör yolunu seçin");
            a("Browse", "Pregledaj", "Pregledaj", "Pregledaj", "Durchsuchen", "Gözat");
            a("PDF documents will be exported to this location", "PDF dokumenti će biti izvezeni na ovu lokaciju", "PDF dokumenti bit će izvezeni na ovu lokaciju", "PDF dokumenti će biti izvezeni na ovu lokaciju", "PDF-Dokumente werden an diesen Ort exportiert", "PDF belgeleri bu konuma aktarılacak");
            a("Word Export Location", "Lokacija za Word izvoz", "Lokacija za Word izvoz", "Lokacija za Word izvoz", "Word-Exportordner", "Word dışa aktarma konumu");
            a("Select folder path for Word exports", "Odaberite putanju mape za Word izvoz", "Odaberite putanju mape za Word izvoz", "Izaberite putanju fascikle za Word izvoz", "Ordnerpfad für Word-Exporte wählen", "Word dışa aktarmaları için klasör yolunu seçin");
            a("Word documents will be exported to this location", "Word dokumenti će biti izvezeni na ovu lokaciju", "Word dokumenti bit će izvezeni na ovu lokaciju", "Word dokumenti će biti izvezeni na ovu lokaciju", "Word-Dokumente werden an diesen Ort exportiert", "Word belgeleri bu konuma aktarılacak");
            a("Excel Export Location", "Lokacija za Excel izvoz", "Lokacija za Excel izvoz", "Lokacija za Excel izvoz", "Excel-Exportordner", "Excel dışa aktarma konumu");
            a("Select folder path for Excel exports", "Odaberite putanju mape za Excel izvoz", "Odaberite putanju mape za Excel izvoz", "Izaberite putanju fascikle za Excel izvoz", "Ordnerpfad für Excel-Exporte wählen", "Excel dışa aktarmaları için klasör yolunu seçin");
            a("Excel files will be exported to this location", "Excel datoteke će biti izvezene na ovu lokaciju", "Excel datoteke bit će izvezene na ovu lokaciju", "Excel datoteke će biti izvezene na ovu lokaciju", "Excel-Dateien werden an diesen Ort exportiert", "Excel dosyaları bu konuma aktarılacak");
            a("Backup and import", "Sigurnosna kopija i uvoz", "Sigurnosna kopija i uvoz", "Rezervna kopija i uvoz", "Sicherung und Import", "Yedekleme ve içe aktarma");
            a("Save your entries and templates to a file, restore them, or bring entries in from a spreadsheet", "Sačuvajte unose i šablone u datoteku, vratite ih ili uvezite unose iz tablice", "Spremite unose i predloške u datoteku, vratite ih ili uvezite unose iz tablice", "Sačuvajte unose i šablone u datoteku, vratite ih ili uvezite unose iz tabele", "Speichern Sie Einträge und Vorlagen in einer Datei, stellen Sie sie wieder her oder importieren Sie Einträge aus einer Tabelle", "Kayıtlarınızı ve şablonlarınızı bir dosyaya kaydedin, geri yükleyin veya bir tablodan kayıt aktarın");
            a("Back up my data", "Sačuvaj moje podatke", "Spremi moje podatke", "Sačuvaj moje podatke", "Meine Daten sichern", "Verilerimi yedekle");
            a("Restore from backup", "Vrati iz sigurnosne kopije", "Vrati iz sigurnosne kopije", "Vrati iz rezervne kopije", "Aus Sicherung wiederherstellen", "Yedekten geri yükle");
            a("Import CSV / Excel", "Uvezi CSV / Excel", "Uvezi CSV / Excel", "Uvezi CSV / Excel", "CSV / Excel importieren", "CSV / Excel içe aktar");
            a("Restoring or importing never deletes anything: entries that already exist (same date, description and hours) are skipped. CSV and Excel files need Date, Description and Hours columns (the app's own exports work).", "Vraćanje ili uvoz nikada ništa ne briše: unosi koji već postoje (isti datum, opis i sati) se preskaču. CSV i Excel datoteke trebaju kolone Datum, Opis i Sati (izvozi same aplikacije rade).", "Vraćanje ili uvoz nikada ništa ne briše: unosi koji već postoje (isti datum, opis i sati) preskaču se. CSV i Excel datoteke trebaju stupce Datum, Opis i Sati (izvozi same aplikacije rade).", "Vraćanje ili uvoz nikada ništa ne briše: unosi koji već postoje (isti datum, opis i sati) se preskaču. CSV i Excel datoteke treba da imaju kolone Datum, Opis i Sati (izvozi same aplikacije rade).", "Wiederherstellen oder Importieren löscht nie etwas: bereits vorhandene Einträge (gleiches Datum, gleiche Beschreibung und Stunden) werden übersprungen. CSV- und Excel-Dateien benötigen die Spalten Datum, Beschreibung und Stunden (die Exporte der App funktionieren).", "Geri yükleme veya içe aktarma hiçbir şeyi silmez: zaten var olan kayıtlar (aynı tarih, açıklama ve saat) atlanır. CSV ve Excel dosyalarında Tarih, Açıklama ve Saat sütunları gerekir (uygulamanın kendi dışa aktarmaları uygundur).");
            a("Reminders", "Podsjetnici", "Podsjetnici", "Podsetnici", "Erinnerungen", "Hatırlatıcılar");
            a("Show a banner when a working day (Mon-Fri) has no entry", "Prikaži traku kada radni dan (pon-pet) nema unosa", "Prikaži traku kada radni dan (pon-pet) nema unosa", "Prikaži traku kada radni dan (pon-pet) nema unosa", "Banner anzeigen, wenn ein Arbeitstag (Mo-Fr) keinen Eintrag hat", "Bir iş gününün (Pzt-Cum) kaydı olmadığında şerit göster");
            a("Remind me about working days without an entry", "Podsjeti me na radne dane bez unosa", "Podsjeti me na radne dane bez unosa", "Podseti me na radne dane bez unosa", "Mich an Arbeitstage ohne Eintrag erinnern", "Kaydı olmayan iş günlerini hatırlat");
            a("Remind me about today from (hour, 0-23)", "Podsjeti me na današnji dan od (sat, 0-23)", "Podsjeti me na današnji dan od (sat, 0-23)", "Podseti me na današnji dan od (sat, 0-23)", "Mich an heute erinnern ab (Stunde, 0-23)", "Bugünü şu saatten itibaren hatırlat (saat, 0-23)");
            a("Also remind me with a Windows notification when the app is closed", "Podsjeti me i Windows obavijesti kada je aplikacija zatvorena", "Podsjeti me i Windows obavijesti kada je aplikacija zatvorena", "Podseti me i Windows obaveštenjem kada je aplikacija zatvorena", "Mich auch mit einer Windows-Benachrichtigung erinnern, wenn die App geschlossen ist", "Uygulama kapalıyken de Windows bildirimiyle hatırlat");
            a("Show a test notification", "Prikaži probnu obavijest", "Prikaži probnu obavijest", "Prikaži probno obaveštenje", "Testbenachrichtigung anzeigen", "Test bildirimi göster");
            a("Dictation", "Diktiranje", "Diktiranje", "Diktiranje", "Diktat", "Dikte");
            a("Speech is turned into text on this computer with the Whisper model; nothing is sent anywhere. The model (about 190 MB) is downloaded the first time you press Dictate.", "Govor se pretvara u tekst na ovom računaru pomoću Whisper modela; ništa se nigdje ne šalje. Model (oko 190 MB) se preuzima kada prvi put pritisnete Diktiraj.", "Govor se pretvara u tekst na ovom računalu pomoću Whisper modela; ništa se nigdje ne šalje. Model (oko 190 MB) preuzima se kada prvi put pritisnete Diktiraj.", "Govor se pretvara u tekst na ovom računaru pomoću Whisper modela; ništa se nigde ne šalje. Model (oko 190 MB) se preuzima kada prvi put pritisnete Diktiraj.", "Sprache wird auf diesem Computer mit dem Whisper-Modell in Text umgewandelt; nichts wird irgendwohin gesendet. Das Modell (ca. 190 MB) wird beim ersten Drücken von Diktieren heruntergeladen.", "Konuşma bu bilgisayarda Whisper modeliyle metne dönüştürülür; hiçbir şey başka yere gönderilmez. Model (yaklaşık 190 MB) Dikte et'e ilk bastığınızda indirilir.");
            a("Language you speak", "Jezik kojim govorite", "Jezik kojim govorite", "Jezik kojim govorite", "Gesprochene Sprache", "Konuştuğunuz dil");
            a("Choosing the language is more accurate than detecting it, especially for short phrases.", "Odabir jezika je tačniji od automatskog prepoznavanja, posebno kod kratkih fraza.", "Odabir jezika je točniji od automatskog prepoznavanja, posebno kod kratkih fraza.", "Izbor jezika je tačniji od automatskog prepoznavanja, posebno kod kratkih fraza.", "Die Sprache zu wählen ist genauer als sie erkennen zu lassen, besonders bei kurzen Sätzen.", "Dili seçmek, özellikle kısa cümlelerde, algılatmaktan daha doğrudur.");
            a("Detect automatically", "Prepoznaj automatski", "Prepoznaj automatski", "Prepoznaj automatski", "Automatisch erkennen", "Otomatik algıla");
            a("Bosnian", "Bosanski", "Bosanski", "Bosanski", "Bosnisch", "Boşnakça");
            a("Croatian", "Hrvatski", "Hrvatski", "Hrvatski", "Kroatisch", "Hırvatça");
            a("Serbian", "Srpski", "Srpski", "Srpski", "Serbisch", "Sırpça");
            a("German", "Njemački", "Njemački", "Nemački", "Deutsch", "Almanca");
            a("Turkish", "Turski", "Turski", "Turski", "Türkisch", "Türkçe");
            a("Updates", "Ažuriranja", "Ažuriranja", "Ažuriranja", "Updates", "Güncellemeler");
            a("Check for and manage application updates", "Provjerite i upravljajte ažuriranjima aplikacije", "Provjerite i upravljajte ažuriranjima aplikacije", "Proverite i upravljajte ažuriranjima aplikacije", "Anwendungsupdates prüfen und verwalten", "Uygulama güncellemelerini kontrol edin ve yönetin");
            a("Tell me about new versions with a Windows notification when the app is closed", "Obavijesti me o novim verzijama Windows obavijesti kada je aplikacija zatvorena", "Obavijesti me o novim verzijama Windows obavijesti kada je aplikacija zatvorena", "Obavesti me o novim verzijama Windows obaveštenjem kada je aplikacija zatvorena", "Mich über neue Versionen per Windows-Benachrichtigung informieren, wenn die App geschlossen ist", "Uygulama kapalıyken yeni sürümler için Windows bildirimiyle haber ver");
            a("Windows checks a few times a day and notifies you once per release. Clicking the notification opens the app, which offers the update.", "Windows provjerava nekoliko puta dnevno i obavještava vas jednom po izdanju. Klik na obavijest otvara aplikaciju koja nudi ažuriranje.", "Windows provjerava nekoliko puta dnevno i obavještava vas jednom po izdanju. Klik na obavijest otvara aplikaciju koja nudi ažuriranje.", "Windows proverava nekoliko puta dnevno i obaveštava vas jednom po izdanju. Klik na obaveštenje otvara aplikaciju koja nudi ažuriranje.", "Windows prüft mehrmals täglich und benachrichtigt Sie einmal pro Version. Ein Klick auf die Benachrichtigung öffnet die App, die das Update anbietet.", "Windows günde birkaç kez kontrol eder ve sürüm başına bir kez bildirir. Bildirime tıklamak, güncellemeyi sunan uygulamayı açar.");
            a("Check for Updates", "Provjeri ažuriranja", "Provjeri ažuriranja", "Proveri ažuriranja", "Nach Updates suchen", "Güncellemeleri denetle");
            a("Help", "Pomoć", "Pomoć", "Pomoć", "Hilfe", "Yardım");
            a("Shortcuts and features that are easy to miss", "Prečice i mogućnosti koje se lako previde", "Prečaci i mogućnosti koje se lako previde", "Prečice i mogućnosti koje se lako previde", "Tastenkürzel und Funktionen, die man leicht übersieht", "Kolayca gözden kaçan kısayollar ve özellikler");

            // Statuses shown by the dialogs and the settings page
            a("Listening... speak now", "Slušam... govorite", "Slušam... govorite", "Slušam... govorite", "Ich höre zu... bitte sprechen", "Dinliyorum... şimdi konuşun");
        }
    }
}
