- Allways use these credentials for login, Username: Muhamed, Password: SelmaSumejaAjna123!*
- Allways run UI tests that are specific for the implemented feature to verify that everything is implemented as it should be
- All project documentation is centralized in the docs/ folder at project root
- Documentation includes: README.md, deployment instructions, OAuth setup, UI tests guide, versioning roadmap, and installer documentation

## MANDATORY TESTING REQUIREMENT (CRITICAL)
- EVERY feature implementation MUST include complete end-to-end testing
- For UI features: MUST login with provided credentials, navigate to the feature, and verify ALL functionality works
- Testing is NOT complete until ALL steps are verified through actual application interaction
- Never declare a feature "complete" without full manual verification through the actual UI
- Document every test step performed and results observed
- This applies to ALL UI implementations - no exceptions
- Feature development is INCOMPLETE until actual UI testing is performed and verified

## Settings Page Requirements (STRICT) - ✅ COMPLETED
- ✅ Database connection string displayed in Application Settings section with parsed details (Host, Port, Database Name, Username)
- ✅ Individual export paths for PDF, Word, and Excel documents (replaced single download location)
- ✅ Browse buttons open native folder selection dialog for each document type
- ✅ Export services (Excel, PDF, CSV, Word) use their respective configured export paths
- ✅ Settings persist to database with proper fallback logic (database → appsettings.json → Desktop default)
- ✅ Enhanced Application Settings display shows comprehensive appsettings.json content
- ✅ Removed Add/Save buttons from Database Settings section (no longer needed)
- ✅ UI tests created for all settings functionality verification

## Export Settings Implementation:
- 📄 PDF Export Location: Individual path for PDF documents
- 📝 Word Export Location: Individual path for Word documents
- 📊 Excel Export Location: Individual path for Excel files (also used for CSV)
- All settings have Browse buttons with native folder picker integration