# Design: multiple clients and documents (consultant edition)

Status: **phase 1 implemented** (business profile, clients, contracts, signature and stamp, migration); phases 2 to 4 are still to do. See section 8.

## 1. Why

The app started as one consultant's tool for one client. Every month the consultant produces the same documents for
that client from the same daily entries:

| Document | Format | What it contains |
| --- | --- | --- |
| Hours overview ("Pregled utrošenih sati") | Excel, sent as is | The tasks of the month with their hours and the total |
| Work order ("Radni nalog") | Word, printed to PDF | The consultant's company, the client's company, the month, the tasks with hours, the total, and two signatures |
| Invoice | Excel template from the accountant | Filled in with the billed amount and the bill number (done outside the app today) |

The goal of this step is to let **one consultant work for several clients at once**, each with its own details,
rates and documents, without changing how the daily entry flow feels. A later step (section 9) adds employees and
company sign-in; this design only makes sure we do not block it.

### Goals

- A consultant can have any number of clients. Each client has its own legal details, contract data, rate and
  documents.
- Each client can have several document templates (Excel, Word, PDF) and the monthly export produces the right set
  for the chosen client and month.
- Hours stay the source of truth; rates and amounts are calculated from them and optional.
- Existing installations keep working: today's setup becomes the first client, with no data loss.

### Non-goals (for this step)

- Accounts for many users, company sign-in, approval workflows, country-specific absence codes. See section 9.
- Replacing the accountant's invoice process. The app only gains the ability to fill an invoice template.
- A visual template designer.

## 2. What the code does today

Findings from reading the current code, which shape the design:

1. **`Client` is only a name.** `clients(Id, UserId, Name, CreatedAt)`. Entries (`daily_tasks.ClientId`) and task
   templates can point to it. Reports already filter by client.
2. **All document data is a single global block.** `PdfSettings` holds the consultant's company, the client's
   company (name, ID, VAT number, address), the document title, the contract prefix and the decision number. It is
   stored as keys in the `settings` table, which has a unique `Key` and **no user column**. So there is exactly one
   issuer and one client for the whole database.
3. **The layouts are written in code.** `ExportService` draws the PDF with QuestPDF, the Word file with OpenXML and
   the Excel file with ClosedXML. File names are hard-coded in `ReportsViewModel` (`RadniNalog_...`,
   `Pregled utrošenih sati - MA - ...`).
4. **Export folders are global** (PDF, Word, Excel), which is why two clients would end up in one folder.
5. **`PdfSettings` has real company data as default values in source code.** It works for one user, but it must not
   remain a default once other people use the app.
6. **The app talks to PostgreSQL directly.** Fine for one person with their own database, not for many users
   (section 9).

## 3. Concepts

```
User (the consultant)
 ├─ BusinessProfile            "who I am": company name, ID, VAT, address, phone, email, decision number,
 │                             logo, signature image, stamp image
 └─ Client (many)              "who I work for"
     ├─ Legal details          legal name, ID, VAT, address, notes
     ├─ Contract (one at a time)  contract number, hourly rate, valid from/to; a renewal adds a new row
     ├─ Currency               BAM by default; EUR, USD and others selectable per client
     ├─ Export folder          optional, per client
     ├─ DocumentTemplate (many)
     └─ Entries (daily_tasks)  already linked through ClientId
```

- **BusinessProfile** exists once per user. It replaces the "company" half of `PdfSettings`. The decision number
  ("Broj rješenja") is printed under the consultant's own company in the work order, so it belongs here (section 10).
- **Client** takes over the "client" half of `PdfSettings`.
- **Contract**: a client has one contract at a time, but contracts are renewed every half year or year with a new
  number. A contract is therefore a dated row, and a document for a given month uses the contract that was valid in
  that month. Old months keep reproducing with their old number.
- **DocumentTemplate** describes one document that can be produced for the client: what kind it is, which format,
  how the file is named, and which layout or template file it uses.
- A **generation** is "template + client + month → file". Nothing about a generation needs to be stored in the first
  version, only the file on disk.

## 4. Data model

New or changed tables (PostgreSQL, same naming style as the existing tables):

```
business_profiles
  Id, UserId (unique), CompanyName, CompanyId, VatNumber, AddressLine1, AddressLine2, Phone, Email,
  DecisionNumber, LogoPath, SignatureImagePath, StampImagePath

clients                      (existing, extended)
  Id, UserId, Name,                              -- display name, as today
  LegalName, CompanyId, VatNumber, AddressLine1, AddressLine2, Notes,
  Currency (ISO code, default 'BAM'), ExportFolder (nullable), IsActive, CreatedAt

client_contracts
  Id, ClientId, ContractNumber (e.g. '078-01'), HourlyRate (decimal 10,2, nullable),
  ValidFrom (date), ValidTo (date, nullable)     -- no rate = hours only, no amounts

document_templates
  Id, ClientId, Name, Kind (HoursOverview | WorkOrder | Invoice | Custom),
  Format (xlsx | docx | pdf), Source (BuiltIn | File), BuiltInLayout (nullable), TemplatePath (nullable),
  FileNamePattern ('{Kind} - {Client} - {YYYY}-{MM}'), SortOrder, IsActive
```

Rules:

- `daily_tasks` does not change. An entry belongs to at most one client, as now.
- The contract (number and rate) that applies to an entry is the one valid on the entry's date. Renewing a contract
  never rewrites past months. Contract periods of one client must not overlap.
- **No rounding.** Billing is the exact sum of the entries; totals are never rounded in the documents.
- Amount = hours × rate in the client's currency. The currency is formatted with its own culture (for example
  `1.234,50 KM`, `€1,234.50`). Only BAM is needed now; the model already allows any currency.
- Deleting a client with entries is not allowed; it is deactivated instead (entries keep their history).
  Today's `ON DELETE SET NULL` stays for safety but the UI no longer offers a hard delete once entries exist.

## 5. Document generation

Two stages, so the most valuable part ships early.

### Stage A: built-in layouts, driven by the client

The current Excel, Word and PDF layouts stay, but read their data from `BusinessProfile` + `Client` instead of
`PdfSettings`, and the file name comes from the template's pattern. A new client gets the same three documents
(hours overview, work order as Word and PDF) by one click, "Add the standard documents", and can switch any of them
off. This covers the consultant's two monthly documents and removes every hard-coded value.

**Signature and stamp.** The business profile accepts a signature image and a stamp image (PNG with a transparent
background works best). The built-in work order places them in the consultant's signature area, so the exported
Word file and the PDF come out ready to print, with signature and stamp already in position, instead of the
consultant adding them by hand each month. Each is a per-document choice (`IncludeSignatures` already exists), so
a document can be exported unsigned. The client's signature area stays empty.

### Stage B: the user's own template files

For anything the built-in layouts do not cover (the accountant's invoice sheet, a client's own timesheet), the user
points the template at an `.xlsx` or `.docx` file that contains **placeholders**, for example:

| Placeholder | Value |
| --- | --- |
| `{{Month}}`, `{{Year}}`, `{{PeriodFrom}}`, `{{PeriodTo}}` | The period |
| `{{Me.CompanyName}}`, `{{Me.CompanyId}}`, `{{Me.Address}}` ... | Business profile |
| `{{Client.LegalName}}`, `{{Client.VatNumber}}`, `{{Contract.Prefix}}` ... | Client |
| `{{TotalHours}}`, `{{Rate}}`, `{{Amount}}`, `{{Currency}}` | Totals |
| `{{Signature}}`, `{{Stamp}}` | The images from the business profile |
| `{{InvoiceNumber}}` | Asked for when generating |
| A table row containing `{{Task.Date}}`, `{{Task.Description}}`, `{{Task.Hours}}` | Repeated once per entry |

Word and Excel are filled by editing the user's file (OpenXML and ClosedXML are already in the project), so the
result looks exactly like their template. PDF output is produced by converting the filled Word file; see the open
question about PDF conversion in section 10.

A placeholder that has no value is left visible and reported ("2 placeholders were not filled"), so a wrong template
is noticed instead of silently producing a bad document.

## 6. User interface

| Where | Change |
| --- | --- |
| **Settings → My business** | The consultant's own company details (was the first half of the PDF card), decision number, and upload of the signature and stamp images with a preview. |
| **Settings → Clients** (new page, or a new section) | List of clients; edit one client: legal details, currency, export folder, its **contracts** (number, rate, valid from/to; "Renew contract" starts a new one), and its **Documents** list with add, edit, reorder and "Add the standard documents". |
| **Reports** | The client chooser already exists. It becomes required for document generation. The export area shows the documents of that client as checkboxes plus one **Generate** button and a result list with "Open file" and "Open folder". |
| **Daily Tasks, week and list views** | Unchanged. A client colour or short tag on each entry is optional and can come later. |
| **Add and edit entry** | Unchanged apart from remembering the last used client, so most entries need no extra click. |
| **Help page** | A short section "Clients and documents". |

The existing three export-path settings stay as the fallback when a client has no folder of its own.
All new text goes through the existing localisation catalog.

## 7. Migration of existing installations

Runs once at start-up in `FixSchemaIssuesAsync`, idempotent like the existing steps:

1. Create the new tables and add the new `clients` columns.
2. Read the current `PdfSettings` value from `settings`. Create the user's `BusinessProfile` from the company half,
   including the decision number. Values missing from the stored JSON fall back to the code defaults, exactly as the
   documents do today (for example the stored value has no decision number or client VAT number, so the code
   defaults are what the documents print now). The migration copies what is **printed**, not what is stored.
3. If entries already point to clients, fill those clients' details from the client half **only when there is exactly
   one client or the name matches**. Otherwise create a client named from `PdfSettings.ClientName` and leave the
   existing clients with empty legal details for the user to complete. The stored contract prefix becomes the first
   contract, valid from the start of the earliest entry.
4. Create the standard document templates for each client that has details (Stage A layouts).
5. Keep the old `settings` keys untouched for one release so a downgrade or a bug cannot lose data. They are removed in
   the release after.
6. The migration only adds tables, columns and rows and never changes or deletes existing data, so it does not make
   an automatic backup. A backup before updating is still a good habit (Settings, Backup and import).

Tests: a database with the current schema and realistic data is migrated and the generated work order must be
**identical** (same text, totals and layout) to the one generated before the migration.

## 8. Phases

| Phase | Result | Release |
| --- | --- | --- |
| 1 (done) | Business profile (with signature and stamp images) and extended clients with contracts, client management UI, migration. Documents still use the built-in layouts, now fed by the client and the contract, with signature and stamp placed. Per-client export folder and file names. | 4.0.0 |
| 2 | Currencies, rates, amounts in Reports and in the documents. | 4.1.0 |
| 3 | Placeholder templates for Word and Excel (accountant's invoice sheet included), invoice number prompt. | 4.2.0 |
| 4 | PDF from templates, per-client colours and tags, polish, Help section. | 4.3.0 |

Each phase ends with the usual full build, tests and real-UI verification. Phase 1 is the largest because it moves
the data model; phases 2 to 4 build on it without schema surprises.

### Notes from the phase 1 implementation

- The contract number on a document is printed as `number/yy`, where `yy` is the year of the **period** being
  documented. Before, it was the year of the day the document was made, which gave a wrong year when December was
  exported in January.
- The migration copies what the documents printed (stored values, then the old code defaults), so existing
  documents come out unchanged. The old `PdfSettings` value is kept in the settings table for one more release.
- A rate typed as `30,5` or `30.5` is read as 30.50 in every interface language.
- The per-client export folder applies to the work orders and the invoice of that client; the hours overview (Excel)
  follows the same rule.

## 9. Direction for the next product step (not designed here)

Written down so today's choices do not close doors; each item needs its own design document.

- **Many users means a server.** The desktop app opens PostgreSQL directly with a connection string. That cannot be
  handed to strangers or to a company. The multi-user product needs a small web API in front of the database that
  the desktop app (and later a web page) call over HTTPS. The `IDatabaseService`, `ISettingsService` and
  `IAuthenticationService` interfaces already separate the UI from the data, so an HTTP implementation of them can
  replace the direct one. The same API would host the stronger dictation model the user plans.
- **Ownership.** Everything new in this design carries a `UserId`. When companies arrive, an `Organization` is added
  above it, and consultants simply belong to a personal organization. The global `settings` table is the one piece
  that is not per user today and is replaced by per-user and per-organization settings.
- **Company sign-in.** Corporate sign-in is a standard rather than something to build: Microsoft Entra ID (the
  company's Active Directory in the cloud) through OpenID Connect. The app opens the company's own sign-in page. The
  **company's policy decides** what happens there: single sign-on, multi-factor authentication, allowed devices, all
  enforced by Entra ID (Conditional Access) before the app receives a token. The app does not implement MFA; it
  checks the signed-in user, the tenant and the company's roles. The existing OAuth sign-in code is the starting point.
  SharePoint is a possible later integration (store generated documents in a library) through Microsoft Graph with
  the same sign-in.
- **Employee timesheets.** A different rule set: absence types with official codes (annual leave, religious holidays,
  parental leave and so on), approval by a manager, locked periods. It reuses entries, clients (the company or its
  projects), templates and the sign-in above. The consultant model should not be bent into it; it becomes a second
  "profile" of the same app. To be specified when the code list and rules are available.

## 10. Decisions and remaining questions

Decided with the product owner:

| # | Question | Decision |
| --- | --- | --- |
| 1 | Where do the contract fields belong? | The contract number and rate belong to the client. The **decision number** is printed under the consultant's own company ("Broj rješenja"), so it belongs to the business profile. The stored settings contain no decision number; the documents print the code default `16-17-320/24`. To be confirmed by the owner. |
| 2 | Several contracts per client? | No: one contract at a time. Contracts are renewed every half year or year with a new number, so the contract is a dated row (section 4). |
| 3 | PDF conversion | The built-in PDF layout is kept. The layout may later differ per client; the template's `BuiltInLayout` field allows a layout variant per client. Word to PDF conversion stays a later option. |
| 4 | Currencies | BAM now; EUR, USD and others selectable per client later (section 4). |
| 5 | Rounding | None. Billing is the exact sum of the entries. |
| 6 | Signatures | Signature **and stamp** images in the business profile, placed automatically in the exported Word file and PDF (section 5). |
| 7 | Sensitive data | Postponed until the database moves to a server (section 9). |

Still open:

1. **Image placement**: where exactly on the work order do the signature and stamp go (the current layout has the
   consultant's signature on one side and the client's on the other)? We take the current signature line as the
   anchor and let the stamp overlap its edge, unless told otherwise.
2. **Contract number format**: the stored value is `078-01` while the code default is `081-01`. Is `078-01` the
   current contract, and what is the next one called when the contract is renewed (the app only needs to keep the
   text, not generate it)?
3. **Contract gaps**: should the app warn when a month has entries but no valid contract?
4. **Client VAT number**: the stored settings have none, so the document prints the code default. Please confirm the
   printed value is correct.

## 11. Risks

| Risk | Mitigation |
| --- | --- |
| Migration changes today's documents | Golden-file test of the work order before and after, backup before the first run, old keys kept for one release |
| Template placeholders broken by Word splitting text into runs | Merge runs before replacing, tested with templates edited in Word; unfilled placeholders are reported |
| Scope creep into the employee product | Section 1 non-goals; the employee step gets its own document and phase plan |
| One huge release | Four phases, each releasable on its own |
