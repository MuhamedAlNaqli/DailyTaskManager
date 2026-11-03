using System;

namespace DailyTaskManager.Core.Localization
{
    /// <summary>
    /// Translations for the business profile, clients and contracts in Settings (columns: bs, hr, sr, de, tr).
    /// </summary>
    internal static class BusinessTranslations
    {
        public static void Register(Action<string, string, string, string, string, string> a)
        {
            // Page and cards
            a("Configure your business, clients and application preferences",
                "Podesite svoju firmu, klijente i postavke aplikacije",
                "Postavite svoju tvrtku, klijente i postavke aplikacije",
                "Podesite svoju firmu, klijente i podešavanja aplikacije",
                "Konfigurieren Sie Ihr Unternehmen, Ihre Kunden und die Anwendungseinstellungen",
                "İşletmenizi, müşterilerinizi ve uygulama tercihlerinizi yapılandırın");
            a("My business", "Moja firma", "Moja tvrtka", "Moja firma", "Mein Unternehmen", "İşletmem");
            a("Your own company details, printed at the top of every document, and the signature and stamp placed on the work order.",
                "Podaci vaše firme, ispisani na vrhu svakog dokumenta, te potpis i pečat koji se stavljaju na radni nalog.",
                "Podaci vaše tvrtke, ispisani na vrhu svakog dokumenta, te potpis i pečat koji se stavljaju na radni nalog.",
                "Podaci vaše firme, odštampani na vrhu svakog dokumenta, i potpis i pečat koji se stavljaju na radni nalog.",
                "Die Daten Ihres eigenen Unternehmens, die oben auf jedem Dokument stehen, sowie Unterschrift und Stempel, die auf den Arbeitsauftrag gesetzt werden.",
                "Her belgenin üstünde yazdırılan kendi şirket bilgileriniz ile iş emrine yerleştirilen imza ve kaşe.");
            a("Preview work order", "Pregled radnog naloga", "Pregled radnog naloga", "Pregled radnog naloga", "Arbeitsauftrag ansehen", "İş emri önizlemesi");
            a("Save", "Sačuvaj", "Spremi", "Sačuvaj", "Speichern", "Kaydet");
            a("Company name", "Naziv firme", "Naziv tvrtke", "Naziv firme", "Firmenname", "Şirket adı");
            a("VAT number", "PDV broj", "PDV broj", "PDV broj", "USt-IdNr.", "KDV numarası");
            a("Address line 1", "Adresa, red 1", "Adresa, redak 1", "Adresa, red 1", "Adresszeile 1", "Adres satırı 1");
            a("Address line 2", "Adresa, red 2", "Adresa, redak 2", "Adresa, red 2", "Adresszeile 2", "Adres satırı 2");
            a("Decision number (Broj rješenja)", "Broj rješenja", "Broj rješenja", "Broj rešenja", "Bescheidnummer (Broj rješenja)", "Karar numarası (Broj rješenja)");
            a("Signature image", "Slika potpisa", "Slika potpisa", "Slika potpisa", "Unterschriftsbild", "İmza görseli");
            a("No signature image", "Nema slike potpisa", "Nema slike potpisa", "Nema slike potpisa", "Kein Unterschriftsbild", "İmza görseli yok");
            a("Stamp image", "Slika pečata", "Slika pečata", "Slika pečata", "Stempelbild", "Kaşe görseli");
            a("No stamp image", "Nema slike pečata", "Nema slike pečata", "Nema slike pečata", "Kein Stempelbild", "Kaşe görseli yok");
            a("Choose image", "Odaberi sliku", "Odaberi sliku", "Izaberi sliku", "Bild auswählen", "Görsel seç");
            a("Remove", "Ukloni", "Ukloni", "Ukloni", "Entfernen", "Kaldır");
            a("Use PNG or JPEG files, ideally a PNG with a transparent background. Both images are placed above your signature line on the work order (Word and PDF).",
                "Koristite PNG ili JPEG datoteke, najbolje PNG s prozirnom pozadinom. Obje slike se stavljaju iznad vaše linije za potpis na radnom nalogu (Word i PDF).",
                "Koristite PNG ili JPEG datoteke, najbolje PNG s prozirnom pozadinom. Obje slike postavljaju se iznad vaše crte za potpis na radnom nalogu (Word i PDF).",
                "Koristite PNG ili JPEG datoteke, najbolje PNG sa providnom pozadinom. Obe slike se stavljaju iznad vaše linije za potpis na radnom nalogu (Word i PDF).",
                "Verwenden Sie PNG- oder JPEG-Dateien, am besten PNG mit transparentem Hintergrund. Beide Bilder werden über Ihrer Unterschriftslinie auf dem Arbeitsauftrag platziert (Word und PDF).",
                "PNG veya JPEG dosyaları kullanın, tercihen şeffaf arka planlı PNG. Her iki görsel de iş emrinde imza satırınızın üstüne yerleştirilir (Word ve PDF).");

            // Clients
            a("Each client has its own company details, contract and folder for documents. The details are printed on that client's work orders and invoices.",
                "Svaki klijent ima svoje podatke o firmi, ugovor i folder za dokumente. Podaci se ispisuju na radnim nalozima i računima tog klijenta.",
                "Svaki klijent ima svoje podatke o tvrtki, ugovor i mapu za dokumente. Podaci se ispisuju na radnim nalozima i računima tog klijenta.",
                "Svaki klijent ima svoje podatke o firmi, ugovor i fasciklu za dokumente. Podaci se štampaju na radnim nalozima i računima tog klijenta.",
                "Jeder Kunde hat eigene Firmendaten, einen Vertrag und einen Ordner für Dokumente. Die Angaben werden auf den Arbeitsaufträgen und Rechnungen dieses Kunden gedruckt.",
                "Her müşterinin kendi şirket bilgileri, sözleşmesi ve belge klasörü vardır. Bilgiler o müşterinin iş emirlerine ve faturalarına yazdırılır.");
            a("New client", "Novi klijent", "Novi klijent", "Novi klijent", "Neuer Kunde", "Yeni müşteri");
            a("Inactive", "Neaktivan", "Neaktivan", "Neaktivan", "Inaktiv", "Pasif");
            a("No clients yet. Add one to print its details on your documents.",
                "Još nema klijenata. Dodajte jednog da se njegovi podaci ispišu na vašim dokumentima.",
                "Još nema klijenata. Dodajte jednog kako bi se njegovi podaci ispisali na vašim dokumentima.",
                "Još nema klijenata. Dodajte jednog da bi se njegovi podaci štampali na vašim dokumentima.",
                "Noch keine Kunden. Fügen Sie einen hinzu, damit seine Daten auf Ihren Dokumenten stehen.",
                "Henüz müşteri yok. Bilgilerinin belgelerinizde yer alması için bir tane ekleyin.");
            a("Delete", "Obriši", "Obriši", "Obriši", "Löschen", "Sil");
            a("Save client", "Sačuvaj klijenta", "Spremi klijenta", "Sačuvaj klijenta", "Kunde speichern", "Müşteriyi kaydet");
            a("Client name (short, shown in lists)",
                "Naziv klijenta (kratko, prikazuje se u listama)", "Naziv klijenta (kratko, prikazuje se na popisima)",
                "Naziv klijenta (kratko, prikazuje se u listama)", "Kundenname (kurz, in Listen angezeigt)", "Müşteri adı (kısa, listelerde gösterilir)");
            a("Company name on documents", "Naziv firme na dokumentima", "Naziv tvrtke na dokumentima", "Naziv firme na dokumentima", "Firmenname auf Dokumenten", "Belgelerdeki şirket adı");
            a("Active (offered for new entries)", "Aktivan (nudi se za nove unose)", "Aktivan (nudi se za nove unose)", "Aktivan (nudi se za nove unose)",
                "Aktiv (für neue Einträge angeboten)", "Aktif (yeni kayıtlar için sunulur)");
            a("Folder for this client's documents", "Folder za dokumente ovog klijenta", "Mapa za dokumente ovog klijenta", "Fascikla za dokumente ovog klijenta",
                "Ordner für die Dokumente dieses Kunden", "Bu müşterinin belgeleri için klasör");
            a("Empty = the default export folders", "Prazno = zadani folderi za izvoz", "Prazno = zadane mape za izvoz", "Prazno = podrazumevane fascikle za izvoz",
                "Leer = die Standard-Exportordner", "Boş = varsayılan dışa aktarma klasörleri");
            a("Clear", "Očisti", "Očisti", "Očisti", "Leeren", "Temizle");
            a("Notes", "Bilješke", "Bilješke", "Beleške", "Notizen", "Notlar");

            // Contracts
            a("Contracts", "Ugovori", "Ugovori", "Ugovori", "Verträge", "Sözleşmeler");
            a("The contract valid in a month supplies the number on that month's documents. Adding a contract ends the open one the day before.",
                "Ugovor koji važi u mjesecu daje broj na dokumentima tog mjeseca. Dodavanjem ugovora završava se otvoreni ugovor dan ranije.",
                "Ugovor koji vrijedi u mjesecu daje broj na dokumentima tog mjeseca. Dodavanjem ugovora završava se otvoreni ugovor dan ranije.",
                "Ugovor koji važi u mesecu daje broj na dokumentima tog meseca. Dodavanjem ugovora završava se otvoreni ugovor dan ranije.",
                "Der in einem Monat gültige Vertrag liefert die Nummer auf den Dokumenten dieses Monats. Ein neuer Vertrag beendet den offenen am Vortag.",
                "Bir ayda geçerli olan sözleşme, o ayın belgelerindeki numarayı verir. Yeni sözleşme eklemek, açık olanı bir gün önce sonlandırır.");
            a("New contract", "Novi ugovor", "Novi ugovor", "Novi ugovor", "Neuer Vertrag", "Yeni sözleşme");
            a("Contract number, e.g. 078-01", "Broj ugovora, npr. 078-01", "Broj ugovora, npr. 078-01", "Broj ugovora, npr. 078-01",
                "Vertragsnummer, z. B. 078-01", "Sözleşme numarası, örn. 078-01");
            a("Hourly rate (optional)", "Cijena po satu (neobavezno)", "Cijena po satu (neobavezno)", "Cena po satu (opciono)", "Stundensatz (optional)", "Saatlik ücret (isteğe bağlı)");
            a("Valid from", "Važi od", "Vrijedi od", "Važi od", "Gültig ab", "Geçerlilik başlangıcı");
            a("Valid to", "Važi do", "Vrijedi do", "Važi do", "Gültig bis", "Geçerlilik sonu");
            a("Open-ended", "Bez kraja", "Bez kraja", "Bez kraja", "Unbefristet", "Süresiz");
            a("Save contract", "Sačuvaj ugovor", "Spremi ugovor", "Sačuvaj ugovor", "Vertrag speichern", "Sözleşmeyi kaydet");
            a("Delete contract", "Obriši ugovor", "Obriši ugovor", "Obriši ugovor", "Vertrag löschen", "Sözleşmeyi sil");
            a("open", "otvoren", "otvoren", "otvoren", "offen", "açık");
            a("Contract", "Ugovor", "Ugovor", "Ugovor", "Vertrag", "Sözleşme");

            // Messages
            a("Your business details were saved.", "Podaci vaše firme su sačuvani.", "Podaci vaše tvrtke su spremljeni.", "Podaci vaše firme su sačuvani.",
                "Ihre Unternehmensdaten wurden gespeichert.", "İşletme bilgileriniz kaydedildi.");
            a("Could not save your business details: {0}", "Podaci firme nisu mogli biti sačuvani: {0}", "Podaci tvrtke nisu mogli biti spremljeni: {0}",
                "Podaci firme nisu mogli biti sačuvani: {0}", "Die Unternehmensdaten konnten nicht gespeichert werden: {0}", "İşletme bilgileri kaydedilemedi: {0}");
            a("Image", "Slika", "Slika", "Slika", "Bild", "Görsel");
            a("Could not create the preview: {0}", "Pregled nije moguće napraviti: {0}", "Pregled nije moguće izraditi: {0}", "Pregled nije moguće napraviti: {0}",
                "Die Vorschau konnte nicht erstellt werden: {0}", "Önizleme oluşturulamadı: {0}");
            a("The client was saved.", "Klijent je sačuvan.", "Klijent je spremljen.", "Klijent je sačuvan.", "Der Kunde wurde gespeichert.", "Müşteri kaydedildi.");
            a("Delete client", "Obriši klijenta", "Obriši klijenta", "Obriši klijenta", "Kunde löschen", "Müşteriyi sil");
            a("Delete \"{0}\"? A client that has entries is made inactive instead, so its entries and documents stay available.",
                "Obrisati \"{0}\"? Klijent koji ima unose umjesto toga postaje neaktivan, pa njegovi unosi i dokumenti ostaju dostupni.",
                "Obrisati \"{0}\"? Klijent koji ima unose umjesto toga postaje neaktivan, pa njegovi unosi i dokumenti ostaju dostupni.",
                "Obrisati \"{0}\"? Klijent koji ima unose umesto toga postaje neaktivan, pa njegovi unosi i dokumenti ostaju dostupni.",
                "\"{0}\" löschen? Ein Kunde mit Einträgen wird stattdessen inaktiv gesetzt, damit seine Einträge und Dokumente verfügbar bleiben.",
                "\"{0}\" silinsin mi? Kayıtları olan müşteri bunun yerine pasif yapılır, böylece kayıtları ve belgeleri erişilebilir kalır.");
            a("The client has entries, so it was made inactive instead of deleted.",
                "Klijent ima unose, pa je postao neaktivan umjesto da bude obrisan.", "Klijent ima unose, pa je postao neaktivan umjesto da bude obrisan.",
                "Klijent ima unose, pa je postao neaktivan umesto da bude obrisan.", "Der Kunde hat Einträge und wurde daher inaktiv gesetzt statt gelöscht.",
                "Müşterinin kayıtları olduğundan silinmek yerine pasif yapıldı.");
            a("Could not delete the client: {0}", "Klijenta nije moguće obrisati: {0}", "Klijenta nije moguće obrisati: {0}", "Klijenta nije moguće obrisati: {0}",
                "Der Kunde konnte nicht gelöscht werden: {0}", "Müşteri silinemedi: {0}");
            a("Could not save the client: {0}", "Klijenta nije moguće sačuvati: {0}", "Klijenta nije moguće spremiti: {0}", "Klijenta nije moguće sačuvati: {0}",
                "Der Kunde konnte nicht gespeichert werden: {0}", "Müşteri kaydedilemedi: {0}");
            a("The hourly rate is not a number.", "Cijena po satu nije broj.", "Cijena po satu nije broj.", "Cena po satu nije broj.",
                "Der Stundensatz ist keine Zahl.", "Saatlik ücret bir sayı değil.");
            a("Choose the date the contract starts.", "Odaberite datum početka ugovora.", "Odaberite datum početka ugovora.", "Izaberite datum početka ugovora.",
                "Wählen Sie das Startdatum des Vertrags.", "Sözleşmenin başlangıç tarihini seçin.");
            a("Delete contract {0}? Documents for its months will have no contract number.",
                "Obrisati ugovor {0}? Dokumenti za njegove mjesece neće imati broj ugovora.", "Obrisati ugovor {0}? Dokumenti za njegove mjesece neće imati broj ugovora.",
                "Obrisati ugovor {0}? Dokumenti za njegove mesece neće imati broj ugovora.", "Vertrag {0} löschen? Dokumente für seine Monate haben dann keine Vertragsnummer.",
                "{0} sözleşmesi silinsin mi? Aylarına ait belgelerde sözleşme numarası olmayacak.");
            a("Could not save the contract: {0}", "Ugovor nije moguće sačuvati: {0}", "Ugovor nije moguće spremiti: {0}", "Ugovor nije moguće sačuvati: {0}",
                "Der Vertrag konnte nicht gespeichert werden: {0}", "Sözleşme kaydedilemedi: {0}");
            a("A client named \"{0}\" already exists.", "Klijent pod nazivom \"{0}\" već postoji.", "Klijent naziva \"{0}\" već postoji.", "Klijent pod nazivom \"{0}\" već postoji.",
                "Ein Kunde namens \"{0}\" existiert bereits.", "\"{0}\" adında bir müşteri zaten var.");
            a("The period overlaps contract {0}. End that contract first.",
                "Period se preklapa s ugovorom {0}. Prvo završite taj ugovor.", "Razdoblje se preklapa s ugovorom {0}. Prvo završite taj ugovor.",
                "Period se preklapa sa ugovorom {0}. Prvo završite taj ugovor.", "Der Zeitraum überschneidet sich mit Vertrag {0}. Beenden Sie zuerst diesen Vertrag.",
                "Dönem {0} sözleşmesiyle çakışıyor. Önce o sözleşmeyi sonlandırın.");
            a("A client needs a name.", "Klijent mora imati naziv.", "Klijent mora imati naziv.", "Klijent mora imati naziv.", "Ein Kunde braucht einen Namen.", "Müşterinin bir adı olmalı.");
            a("A contract needs a number.", "Ugovor mora imati broj.", "Ugovor mora imati broj.", "Ugovor mora imati broj.", "Ein Vertrag braucht eine Nummer.", "Sözleşmenin bir numarası olmalı.");
            a("The contract cannot end before it starts.", "Ugovor ne može završiti prije nego što počne.", "Ugovor ne može završiti prije nego što počne.",
                "Ugovor ne može završiti pre nego što počne.", "Der Vertrag kann nicht enden, bevor er beginnt.", "Sözleşme başlamadan önce bitemez.");
            a("The hourly rate cannot be negative.", "Cijena po satu ne može biti negativna.", "Cijena po satu ne može biti negativna.", "Cena po satu ne može biti negativna.",
                "Der Stundensatz darf nicht negativ sein.", "Saatlik ücret negatif olamaz.");
            a("The image is larger than 5 MB.", "Slika je veća od 5 MB.", "Slika je veća od 5 MB.", "Slika je veća od 5 MB.", "Das Bild ist größer als 5 MB.", "Görsel 5 MB'tan büyük.");
            a("Only PNG and JPEG images can be used.", "Mogu se koristiti samo PNG i JPEG slike.", "Mogu se koristiti samo PNG i JPEG slike.", "Mogu se koristiti samo PNG i JPEG slike.",
                "Es können nur PNG- und JPEG-Bilder verwendet werden.", "Yalnızca PNG ve JPEG görseller kullanılabilir.");
            a("Client not found.", "Klijent nije pronađen.", "Klijent nije pronađen.", "Klijent nije pronađen.", "Kunde nicht gefunden.", "Müşteri bulunamadı.");
            a("Contract not found.", "Ugovor nije pronađen.", "Ugovor nije pronađen.", "Ugovor nije pronađen.", "Vertrag nicht gefunden.", "Sözleşme bulunamadı.");

            // Help page
            a("Clients and documents", "Klijenti i dokumenti", "Klijenti i dokumenti", "Klijenti i dokumenti", "Kunden und Dokumente", "Müşteriler ve belgeler");
            a("One setup per client", "Jedna postavka po klijentu", "Jedna postavka po klijentu", "Jedna postavka po klijentu", "Eine Einrichtung pro Kunde", "Müşteri başına bir kurulum");
            a("Settings, My business holds your company details, decision number, signature image and stamp image. They are used on every document.",
                "Postavke, Moja firma sadrže podatke vaše firme, broj rješenja, sliku potpisa i sliku pečata. Koriste se na svakom dokumentu.",
                "Postavke, Moja tvrtka sadrže podatke vaše tvrtke, broj rješenja, sliku potpisa i sliku pečata. Koriste se na svakom dokumentu.",
                "Podešavanja, Moja firma sadrže podatke vaše firme, broj rešenja, sliku potpisa i sliku pečata. Koriste se na svakom dokumentu.",
                "Einstellungen, Mein Unternehmen enthält Ihre Firmendaten, die Bescheidnummer, das Unterschriftsbild und das Stempelbild. Sie werden auf jedem Dokument verwendet.",
                "Ayarlar, İşletmem bölümü şirket bilgilerinizi, karar numaranızı, imza görselinizi ve kaşe görselinizi içerir. Her belgede kullanılır.");
            a("Signature and stamp", "Potpis i pečat", "Potpis i pečat", "Potpis i pečat", "Unterschrift und Stempel", "İmza ve kaşe");
            a("Upload PNG or JPEG images (a PNG with a transparent background looks best). The Word file and the PDF come out with both already above your signature line, ready to print.",
                "Učitajte PNG ili JPEG slike (najbolje izgleda PNG s prozirnom pozadinom). Word datoteka i PDF dobijaju obje slike već iznad vaše linije za potpis, spremne za štampu.",
                "Učitajte PNG ili JPEG slike (najbolje izgleda PNG s prozirnom pozadinom). Word datoteka i PDF dobivaju obje slike već iznad vaše crte za potpis, spremne za ispis.",
                "Otpremite PNG ili JPEG slike (najbolje izgleda PNG sa providnom pozadinom). Word datoteka i PDF dobijaju obe slike već iznad vaše linije za potpis, spremne za štampu.",
                "Laden Sie PNG- oder JPEG-Bilder hoch (am besten PNG mit transparentem Hintergrund). Word-Datei und PDF enthalten beide Bilder bereits über Ihrer Unterschriftslinie, druckfertig.",
                "PNG veya JPEG görseller yükleyin (şeffaf arka planlı PNG en iyi görünür). Word dosyası ve PDF, her iki görseli de imza satırınızın üstünde hazır olarak içerir; yazdırmaya hazırdır.");
            a("Settings, Clients keeps each client's company name, ID, VAT number, address, currency and notes. A client that has entries is made inactive instead of deleted.",
                "Postavke, Klijenti čuvaju naziv firme, ID, PDV broj, adresu, valutu i bilješke svakog klijenta. Klijent koji ima unose postaje neaktivan umjesto da bude obrisan.",
                "Postavke, Klijenti čuvaju naziv tvrtke, ID, PDV broj, adresu, valutu i bilješke svakog klijenta. Klijent koji ima unose postaje neaktivan umjesto da bude obrisan.",
                "Podešavanja, Klijenti čuvaju naziv firme, ID, PDV broj, adresu, valutu i beleške svakog klijenta. Klijent koji ima unose postaje neaktivan umesto da bude obrisan.",
                "Einstellungen, Kunden enthält Firmenname, ID, USt-IdNr., Adresse, Währung und Notizen jedes Kunden. Ein Kunde mit Einträgen wird inaktiv gesetzt statt gelöscht.",
                "Ayarlar, Müşteriler bölümü her müşterinin şirket adını, kimlik numarasını, KDV numarasını, adresini, para birimini ve notlarını tutar. Kayıtları olan müşteri silinmez, pasif yapılır.");
            a("A client has one contract at a time. When it is renewed, add the new contract with its start date: the old one ends the day before, and earlier months keep their old number.",
                "Klijent ima jedan ugovor u isto vrijeme. Kad se ugovor obnovi, dodajte novi s datumom početka: stari završava dan ranije, a raniji mjeseci zadržavaju stari broj.",
                "Klijent ima jedan ugovor istodobno. Kad se ugovor obnovi, dodajte novi s datumom početka: stari završava dan ranije, a raniji mjeseci zadržavaju stari broj.",
                "Klijent ima jedan ugovor u isto vreme. Kada se ugovor obnovi, dodajte novi sa datumom početka: stari se završava dan ranije, a raniji meseci zadržavaju stari broj.",
                "Ein Kunde hat jeweils einen Vertrag. Bei einer Verlängerung fügen Sie den neuen Vertrag mit Startdatum hinzu: Der alte endet am Vortag, frühere Monate behalten ihre alte Nummer.",
                "Bir müşterinin aynı anda tek sözleşmesi olur. Yenilendiğinde yeni sözleşmeyi başlangıç tarihiyle ekleyin: eskisi bir gün önce biter, önceki aylar eski numarasını korur.");
            a("Client folder", "Folder klijenta", "Mapa klijenta", "Fascikla klijenta", "Kundenordner", "Müşteri klasörü");
            a("Give a client its own folder and its documents are saved there instead of the general export folders.",
                "Dajte klijentu vlastiti folder i njegovi dokumenti se spremaju tamo umjesto u opće foldere za izvoz.",
                "Dajte klijentu vlastitu mapu i njegovi se dokumenti spremaju ondje umjesto u opće mape za izvoz.",
                "Dajte klijentu sopstvenu fasciklu i njegovi dokumenti se čuvaju tamo umesto u opštim fasciklama za izvoz.",
                "Geben Sie einem Kunden einen eigenen Ordner, und seine Dokumente werden dort statt in den allgemeinen Exportordnern gespeichert.",
                "Bir müşteriye kendi klasörünü verirseniz belgeleri genel dışa aktarma klasörleri yerine orada saklanır.");
            a("Your own details come from My business, and the client's details and contract number from Settings, Clients. See Clients and documents.",
                "Vaši podaci dolaze iz Moje firme, a podaci klijenta i broj ugovora iz Postavki, Klijenti. Pogledajte Klijenti i dokumenti.",
                "Vaši podaci dolaze iz Moje tvrtke, a podaci klijenta i broj ugovora iz Postavki, Klijenti. Pogledajte Klijenti i dokumenti.",
                "Vaši podaci dolaze iz Moje firme, a podaci klijenta i broj ugovora iz Podešavanja, Klijenti. Pogledajte Klijenti i dokumenti.",
                "Ihre eigenen Angaben stammen aus Mein Unternehmen, die Angaben des Kunden und die Vertragsnummer aus Einstellungen, Kunden. Siehe Kunden und Dokumente.",
                "Kendi bilgileriniz İşletmem bölümünden, müşterinin bilgileri ve sözleşme numarası Ayarlar, Müşteriler bölümünden gelir. Müşteriler ve belgeler bölümüne bakın.");

            // Reports: rates and amounts
            a("Your company, the client and the contract number come from Settings (My business and Clients). Files are saved to the client's folder, or to your export folders.",
                "Vaša firma, klijent i broj ugovora dolaze iz Postavki (Moja firma i Klijenti). Datoteke se spremaju u folder klijenta ili u vaše foldere za izvoz.",
                "Vaša tvrtka, klijent i broj ugovora dolaze iz Postavki (Moja tvrtka i Klijenti). Datoteke se spremaju u mapu klijenta ili u vaše mape za izvoz.",
                "Vaša firma, klijent i broj ugovora dolaze iz Podešavanja (Moja firma i Klijenti). Datoteke se čuvaju u fascikli klijenta ili u vašim fasciklama za izvoz.",
                "Ihr Unternehmen, der Kunde und die Vertragsnummer stammen aus den Einstellungen (Mein Unternehmen und Kunden). Die Dateien werden im Ordner des Kunden oder in Ihren Exportordnern gespeichert.",
                "Şirketiniz, müşteri ve sözleşme numarası Ayarlar'dan (İşletmem ve Müşteriler) gelir. Dosyalar müşterinin klasörüne veya dışa aktarma klasörlerinize kaydedilir.");
            a("Net amount for the month: {0} {1}", "Neto iznos za mjesec: {0} {1}", "Neto iznos za mjesec: {0} {1}", "Neto iznos za mesec: {0} {1}",
                "Nettobetrag für den Monat: {0} {1}", "Ay için net tutar: {0} {1}");
            a("Rate from contract {0}", "Cijena iz ugovora {0}", "Cijena iz ugovora {0}", "Cena iz ugovora {0}", "Satz aus Vertrag {0}", "Ücret {0} sözleşmesinden");
            a("No contract is valid in this month", "Nijedan ugovor ne važi u ovom mjesecu", "Nijedan ugovor ne vrijedi u ovom mjesecu", "Nijedan ugovor ne važi u ovom mesecu",
                "In diesem Monat ist kein Vertrag gültig", "Bu ayda geçerli sözleşme yok");

            // Documents from template files, tag colours
            a("Documents", "Dokumenti", "Dokumenti", "Dokumenti", "Dokumente", "Belgeler");
            a("Your own Word or Excel files with {{placeholders}} that are filled in for a month, for example the invoice sheet of your accountant.",
                "Vaše Word ili Excel datoteke s oznakama {{...}} koje se popunjavaju za mjesec, na primjer obrazac računa vašeg računovođe.",
                "Vaše Word ili Excel datoteke s oznakama {{...}} koje se popunjavaju za mjesec, na primjer obrazac računa vašeg računovođe.",
                "Vaše Word ili Excel datoteke sa oznakama {{...}} koje se popunjavaju za mesec, na primer obrazac računa vašeg računovođe.",
                "Ihre eigenen Word- oder Excel-Dateien mit Platzhaltern {{...}}, die für einen Monat ausgefüllt werden, z. B. die Rechnungsvorlage Ihres Buchhalters.",
                "Bir ay için doldurulan {{...}} yer tutucularına sahip kendi Word veya Excel dosyalarınız; örneğin muhasebecinizin fatura tablosu.");
            a("Show placeholders", "Prikaži oznake", "Prikaži oznake", "Prikaži oznake", "Platzhalter anzeigen", "Yer tutucuları göster");
            a("Add template file", "Dodaj datoteku predloška", "Dodaj datoteku predloška", "Dodaj datoteku šablona", "Vorlagendatei hinzufügen", "Şablon dosyası ekle");
            a("Template name", "Naziv predloška", "Naziv predloška", "Naziv šablona", "Name der Vorlage", "Şablon adı");
            a("File name of the result ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})",
                "Naziv rezultirajuće datoteke ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})", "Naziv rezultirajuće datoteke ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})",
                "Naziv rezultujuće datoteke ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})", "Dateiname des Ergebnisses ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})",
                "Sonuç dosyasının adı ({Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber})");
            a("Offer this template in Reports", "Nudi ovaj predložak u Izvještajima", "Nudi ovaj predložak u Izvješćima", "Nudi ovaj šablon u Izveštajima",
                "Diese Vorlage in den Berichten anbieten", "Bu şablonu Raporlar'da sun");
            a("Save template", "Sačuvaj predložak", "Spremi predložak", "Sačuvaj šablon", "Vorlage speichern", "Şablonu kaydet");
            a("Delete template", "Obriši predložak", "Obriši predložak", "Obriši šablon", "Vorlage löschen", "Şablonu sil");
            a("Tag colour", "Boja oznake", "Boja oznake", "Boja oznake", "Markierungsfarbe", "Etiket rengi");
            a("No colour", "Bez boje", "Bez boje", "Bez boje", "Keine Farbe", "Renk yok");
            a("Teal", "Tirkizna", "Tirkizna", "Tirkizna", "Petrol", "Turkuaz");
            a("Blue", "Plava", "Plava", "Plava", "Blau", "Mavi");
            a("Purple", "Ljubičasta", "Ljubičasta", "Ljubičasta", "Lila", "Mor");
            a("Pink", "Roza", "Roza", "Roze", "Rosa", "Pembe");
            a("Red", "Crvena", "Crvena", "Crvena", "Rot", "Kırmızı");
            a("Orange", "Narandžasta", "Narančasta", "Narandžasta", "Orange", "Turuncu");
            a("Amber", "Jantarna", "Jantarna", "Žuto-narandžasta", "Bernstein", "Kehribar");
            a("Green", "Zelena", "Zelena", "Zelena", "Grün", "Yeşil");
            a("Grey", "Siva", "Siva", "Siva", "Grau", "Gri");

            a("Choose a Word or Excel template", "Odaberite Word ili Excel predložak", "Odaberite Word ili Excel predložak", "Izaberite Word ili Excel šablon",
                "Word- oder Excel-Vorlage auswählen", "Bir Word veya Excel şablonu seçin");
            a("Template added", "Predložak dodan", "Predložak dodan", "Šablon dodat", "Vorlage hinzugefügt", "Şablon eklendi");
            a("Template", "Predložak", "Predložak", "Šablon", "Vorlage", "Şablon");
            a("The template was added, but it contains no {{placeholders}} yet.",
                "Predložak je dodan, ali još ne sadrži nijednu oznaku {{...}}.", "Predložak je dodan, ali još ne sadrži nijednu oznaku {{...}}.",
                "Šablon je dodat, ali još ne sadrži nijednu oznaku {{...}}.", "Die Vorlage wurde hinzugefügt, enthält aber noch keine Platzhalter {{...}}.",
                "Şablon eklendi ancak henüz {{...}} yer tutucusu içermiyor.");
            a("The template was added. It uses {0} placeholders.", "Predložak je dodan. Koristi oznaka: {0}.", "Predložak je dodan. Koristi oznaka: {0}.",
                "Šablon je dodat. Koristi oznaka: {0}.", "Die Vorlage wurde hinzugefügt. Sie verwendet {0} Platzhalter.", "Şablon eklendi. {0} yer tutucu kullanıyor.");
            a("The template was added, but these placeholders are not known and will stay visible: {0}",
                "Predložak je dodan, ali ove oznake nisu poznate i ostat će vidljive: {0}", "Predložak je dodan, ali ove oznake nisu poznate i ostat će vidljive: {0}",
                "Šablon je dodat, ali ove oznake nisu poznate i ostaće vidljive: {0}", "Die Vorlage wurde hinzugefügt, aber diese Platzhalter sind unbekannt und bleiben sichtbar: {0}",
                "Şablon eklendi ancak bu yer tutucular bilinmiyor ve görünür kalacak: {0}");
            a("Could not add the template: {0}", "Predložak nije moguće dodati: {0}", "Predložak nije moguće dodati: {0}", "Šablon nije moguće dodati: {0}",
                "Die Vorlage konnte nicht hinzugefügt werden: {0}", "Şablon eklenemedi: {0}");
            a("Delete the template \"{0}\"? Documents already made from it stay where they are.",
                "Obrisati predložak \"{0}\"? Dokumenti već napravljeni iz njega ostaju gdje jesu.", "Obrisati predložak \"{0}\"? Dokumenti već izrađeni iz njega ostaju gdje jesu.",
                "Obrisati šablon \"{0}\"? Dokumenti već napravljeni iz njega ostaju gde jesu.", "Die Vorlage \"{0}\" löschen? Bereits erstellte Dokumente bleiben erhalten.",
                "\"{0}\" şablonu silinsin mi? Bununla oluşturulmuş belgeler olduğu yerde kalır.");
            a("Placeholders", "Oznake", "Oznake", "Oznake", "Platzhalter", "Yer tutucular");
            a("Write a placeholder in your Word or Excel file like {{Client.LegalName}}. A table row (or sheet row) with {{Task.Date}}, {{Task.Description}} or {{Task.Hours}} is repeated for every entry.",
                "Napišite oznaku u svojoj Word ili Excel datoteci, na primjer {{Client.LegalName}}. Red tabele (ili lista) s {{Task.Date}}, {{Task.Description}} ili {{Task.Hours}} ponavlja se za svaki unos.",
                "Napišite oznaku u svojoj Word ili Excel datoteci, na primjer {{Client.LegalName}}. Redak tablice (ili lista) s {{Task.Date}}, {{Task.Description}} ili {{Task.Hours}} ponavlja se za svaki unos.",
                "Napišite oznaku u svojoj Word ili Excel datoteci, na primer {{Client.LegalName}}. Red tabele (ili lista) sa {{Task.Date}}, {{Task.Description}} ili {{Task.Hours}} ponavlja se za svaki unos.",
                "Schreiben Sie einen Platzhalter in Ihre Word- oder Excel-Datei, z. B. {{Client.LegalName}}. Eine Tabellenzeile (oder Tabellenblattzeile) mit {{Task.Date}}, {{Task.Description}} oder {{Task.Hours}} wird für jeden Eintrag wiederholt.",
                "Word veya Excel dosyanıza {{Client.LegalName}} gibi bir yer tutucu yazın. {{Task.Date}}, {{Task.Description}} veya {{Task.Hours}} içeren bir tablo satırı (veya sayfa satırı) her kayıt için tekrarlanır.");
            a("A template must be a Word (.docx) or Excel (.xlsx) file.", "Predložak mora biti Word (.docx) ili Excel (.xlsx) datoteka.", "Predložak mora biti Word (.docx) ili Excel (.xlsx) datoteka.",
                "Šablon mora biti Word (.docx) ili Excel (.xlsx) datoteka.", "Eine Vorlage muss eine Word-Datei (.docx) oder Excel-Datei (.xlsx) sein.", "Şablon bir Word (.docx) veya Excel (.xlsx) dosyası olmalı.");
            a("The template is larger than 10 MB.", "Predložak je veći od 10 MB.", "Predložak je veći od 10 MB.", "Šablon je veći od 10 MB.", "Die Vorlage ist größer als 10 MB.", "Şablon 10 MB'tan büyük.");
            a("A template needs a name.", "Predložak mora imati naziv.", "Predložak mora imati naziv.", "Šablon mora imati naziv.", "Eine Vorlage braucht einen Namen.", "Şablonun bir adı olmalı.");
            a("Template not found.", "Predložak nije pronađen.", "Predložak nije pronađen.", "Šablon nije pronađen.", "Vorlage nicht gefunden.", "Şablon bulunamadı.");

            a("Your template files for this client, filled in with the month and client selected above.",
                "Vaše datoteke predložaka za ovog klijenta, popunjene za mjesec i klijenta odabrane iznad.", "Vaše datoteke predložaka za ovog klijenta, popunjene za mjesec i klijenta odabrane iznad.",
                "Vaše datoteke šablona za ovog klijenta, popunjene za mesec i klijenta izabrane iznad.", "Ihre Vorlagendateien für diesen Kunden, ausgefüllt mit dem oben gewählten Monat und Kunden.",
                "Bu müşteri için şablon dosyalarınız; yukarıda seçilen ay ve müşteriyle doldurulur.");
            a("Generate selected documents", "Napravi odabrane dokumente", "Izradi odabrane dokumente", "Napravi izabrane dokumente", "Ausgewählte Dokumente erstellen", "Seçili belgeleri oluştur");
            a("Tick at least one document to generate.", "Označite barem jedan dokument za izradu.", "Označite barem jedan dokument za izradu.", "Označite bar jedan dokument za izradu.",
                "Wählen Sie mindestens ein Dokument aus.", "Oluşturmak için en az bir belge işaretleyin.");
            a("Enter an invoice number in the Invoice section; a selected document uses it.",
                "Unesite broj računa u odjeljku Račun; odabrani dokument ga koristi.", "Unesite broj računa u odjeljku Račun; odabrani dokument ga koristi.",
                "Unesite broj računa u odeljku Račun; izabrani dokument ga koristi.", "Geben Sie im Bereich Rechnung eine Rechnungsnummer ein; ein ausgewähltes Dokument verwendet sie.",
                "Fatura bölümüne bir fatura numarası girin; seçili bir belge onu kullanıyor.");
            a("Created:", "Napravljeno:", "Izrađeno:", "Napravljeno:", "Erstellt:", "Oluşturuldu:");
            a("These placeholders have no value and were left in the files: {0}", "Ove oznake nemaju vrijednost i ostavljene su u datotekama: {0}", "Ove oznake nemaju vrijednost i ostavljene su u datotekama: {0}",
                "Ove oznake nemaju vrednost i ostavljene su u datotekama: {0}", "Diese Platzhalter haben keinen Wert und blieben in den Dateien stehen: {0}", "Bu yer tutucuların değeri yok ve dosyalarda bırakıldı: {0}");
            a("Open it now?", "Otvoriti sada?", "Otvoriti sada?", "Otvoriti sada?", "Jetzt öffnen?", "Şimdi açılsın mı?");
            a("Documents created", "Dokumenti napravljeni", "Dokumenti izrađeni", "Dokumenti napravljeni", "Dokumente erstellt", "Belgeler oluşturuldu");
            a("Failed to create the documents: {0}", "Izrada dokumenata nije uspjela: {0}", "Izrada dokumenata nije uspjela: {0}", "Pravljenje dokumenata nije uspelo: {0}",
                "Die Dokumente konnten nicht erstellt werden: {0}", "Belgeler oluşturulamadı: {0}");

            // Help page: rates, templates, colours
            a("Rates", "Cijene", "Cijene", "Cene", "Sätze", "Ücretler");
            a("A contract can have an hourly rate. Reports then use it, with the client's currency, to show the amount of the month and to fill the invoice.",
                "Ugovor može imati cijenu po satu. Izvještaji je tada koriste, uz valutu klijenta, za prikaz iznosa za mjesec i za popunjavanje računa.",
                "Ugovor može imati cijenu po satu. Izvješća je tada koriste, uz valutu klijenta, za prikaz iznosa za mjesec i za popunjavanje računa.",
                "Ugovor može imati cenu po satu. Izveštaji je tada koriste, uz valutu klijenta, za prikaz iznosa za mesec i za popunjavanje računa.",
                "Ein Vertrag kann einen Stundensatz haben. Die Berichte verwenden ihn dann mit der Währung des Kunden, um den Monatsbetrag anzuzeigen und die Rechnung auszufüllen.",
                "Bir sözleşmenin saatlik ücreti olabilir. Raporlar bunu müşterinin para birimiyle birlikte ayın tutarını göstermek ve faturayı doldurmak için kullanır.");
            a("Your own templates", "Vaši predlošci", "Vaši predlošci", "Vaši šabloni", "Ihre eigenen Vorlagen", "Kendi şablonlarınız");
            a("Settings, Clients, Documents: add a Word or Excel file with placeholders such as {{Client.LegalName}}, {{TotalHours}} or {{InvoiceNumber}}. A table row or sheet row with {{Task.Date}}, {{Task.Description}} and {{Task.Hours}} is repeated for every entry. Reports then offers the file under Documents.",
                "Postavke, Klijenti, Dokumenti: dodajte Word ili Excel datoteku s oznakama kao što su {{Client.LegalName}}, {{TotalHours}} ili {{InvoiceNumber}}. Red tabele ili lista s {{Task.Date}}, {{Task.Description}} i {{Task.Hours}} ponavlja se za svaki unos. Izvještaji zatim nude datoteku pod Dokumenti.",
                "Postavke, Klijenti, Dokumenti: dodajte Word ili Excel datoteku s oznakama kao što su {{Client.LegalName}}, {{TotalHours}} ili {{InvoiceNumber}}. Redak tablice ili lista s {{Task.Date}}, {{Task.Description}} i {{Task.Hours}} ponavlja se za svaki unos. Izvješća zatim nude datoteku pod Dokumenti.",
                "Podešavanja, Klijenti, Dokumenti: dodajte Word ili Excel datoteku sa oznakama kao što su {{Client.LegalName}}, {{TotalHours}} ili {{InvoiceNumber}}. Red tabele ili lista sa {{Task.Date}}, {{Task.Description}} i {{Task.Hours}} ponavlja se za svaki unos. Izveštaji zatim nude datoteku pod Dokumenti.",
                "Einstellungen, Kunden, Dokumente: Fügen Sie eine Word- oder Excel-Datei mit Platzhaltern wie {{Client.LegalName}}, {{TotalHours}} oder {{InvoiceNumber}} hinzu. Eine Tabellen- oder Blattzeile mit {{Task.Date}}, {{Task.Description}} und {{Task.Hours}} wird für jeden Eintrag wiederholt. Die Berichte bieten die Datei dann unter Dokumente an.",
                "Ayarlar, Müşteriler, Belgeler: {{Client.LegalName}}, {{TotalHours}} veya {{InvoiceNumber}} gibi yer tutucular içeren bir Word veya Excel dosyası ekleyin. {{Task.Date}}, {{Task.Description}} ve {{Task.Hours}} içeren bir tablo veya sayfa satırı her kayıt için tekrarlanır. Raporlar dosyayı daha sonra Belgeler altında sunar.");
            a("Give a client a colour and a dot of that colour appears next to its entries in the week and list views.",
                "Dajte klijentu boju i tačka te boje pojavljuje se pored njegovih unosa u sedmičnom prikazu i prikazu liste.",
                "Dajte klijentu boju i točka te boje pojavljuje se pored njegovih unosa u tjednom prikazu i prikazu popisa.",
                "Dajte klijentu boju i tačka te boje pojavljuje se pored njegovih unosa u nedeljnom prikazu i prikazu liste.",
                "Geben Sie einem Kunden eine Farbe, und ein Punkt dieser Farbe erscheint neben seinen Einträgen in der Wochen- und Listenansicht.",
                "Bir müşteriye renk verin; o renkte bir nokta, hafta ve liste görünümlerinde kayıtlarının yanında görünür.");

            a("Contract number", "Broj ugovora", "Broj ugovora", "Broj ugovora", "Vertragsnummer", "Sözleşme numarası");
            a("File name of the result", "Naziv rezultirajuće datoteke", "Naziv rezultirajuće datoteke", "Naziv rezultujuće datoteke", "Dateiname des Ergebnisses", "Sonuç dosyasının adı");
            a("You can use {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber}", "Možete koristiti {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber}",
                "Možete koristiti {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber}", "Možete koristiti {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber}",
                "Sie können {Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber} verwenden", "{Name} {Client} {YYYY} {MM} {Month} {InvoiceNumber} kullanabilirsiniz");

            // Titles of the native file and folder dialogs
            a("Choose your signature image", "Odaberite sliku potpisa", "Odaberite sliku potpisa", "Izaberite sliku potpisa", "Unterschriftsbild auswählen", "İmza görselini seçin");
            a("Choose your stamp image", "Odaberite sliku pečata", "Odaberite sliku pečata", "Izaberite sliku pečata", "Stempelbild auswählen", "Kaşe görselini seçin");
            a("Select the folder for this client's documents", "Odaberite folder za dokumente ovog klijenta", "Odaberite mapu za dokumente ovog klijenta",
                "Izaberite fasciklu za dokumente ovog klijenta", "Ordner für die Dokumente dieses Kunden auswählen", "Bu müşterinin belgeleri için klasörü seçin");
        }
    }
}
