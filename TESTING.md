# 🧪 Testing Guide for Daily Task Manager

## Overview

This project currently includes comprehensive UI tests that verify the application's functionality end-to-end. These tests require a desktop environment and cannot run in headless CI environments like GitHub Actions.

## 🖥️ UI Tests

### Location
- **Project**: `tests/DailyTaskManager.UI.Tests`
- **Framework**: Uses Windows UI Automation for testing Avalonia UI components
- **Coverage**: 55 tests covering login, main functionality, settings, exports, and date filtering

### Running UI Tests Locally

**Prerequisites:**
- Windows desktop environment (required for UI Automation)
- PostgreSQL database running (tests use real database)
- Display/monitor connected (tests interact with actual UI)

**Commands:**
```bash
# Run all UI tests
dotnet test tests/DailyTaskManager.UI.Tests --configuration Release

# Run specific test class
dotnet test tests/DailyTaskManager.UI.Tests --filter "FullyQualifiedName~LoginWindowLayoutTests"

# Run with detailed output
dotnet test tests/DailyTaskManager.UI.Tests --configuration Release --verbosity detailed

# Run and collect coverage
dotnet test tests/DailyTaskManager.UI.Tests --collect:"XPlat Code Coverage"
```

### Test Categories

1. **Login Tests**
   - Window layout and controls verification
   - Authentication flow testing
   - Error handling validation

2. **Main Window Tests**
   - Sidebar navigation
   - Task management functionality
   - UI responsiveness

3. **Settings Tests**
   - Database connection display
   - Export path configuration
   - Update system integration

4. **Export Tests**
   - PDF generation
   - Excel export functionality
   - File path handling

5. **Date Filtering Tests**
   - Date picker functionality
   - Range filtering
   - Calendar integration

## 🚀 CI/CD Testing Strategy

### GitHub Actions Behavior

**Current Setup:**
- ✅ **Build validation**: Code compiles successfully
- ⚠️  **UI tests skipped**: Cannot run in headless CI environment
- ✅ **Executable verification**: Confirms build outputs are correct

**Why UI Tests Are Skipped in CI:**
- GitHub Actions runners don't have desktop environments
- UI Automation requires actual windows and display
- Tests would fail with "no display" errors

### Manual Testing Before Release

**Required Steps:**
1. **Run full test suite locally:**
   ```bash
   dotnet test tests/DailyTaskManager.UI.Tests --configuration Release
   ```

2. **Verify all tests pass:**
   - Total tests: 55
   - Expected: All passed ✅
   - Any failures should be investigated and fixed

3. **Test key scenarios manually:**
   - Login with valid credentials
   - Navigate through all main features
   - Test export functionality
   - Verify settings page functionality
   - Test update check button

## 📋 Test Development Guidelines

### Adding New Tests

1. **Follow existing patterns:**
   ```csharp
   [Test]
   public async Task Should_VerifySpecificBehavior()
   {
       // Arrange
       await LaunchApplicationAsync();

       // Act
       var element = await FindElementAsync(/* criteria */);
       await element.ClickAsync();

       // Assert
       Assert.IsTrue(condition, "Error message");
   }
   ```

2. **Use meaningful test names:**
   - `Should_DisplayLoginWindow_WhenApplicationStarts`
   - `Should_SaveTask_WhenValidDataProvided`
   - `Should_ShowError_WhenInvalidCredentials`

3. **Include proper cleanup:**
   ```csharp
   [TearDown]
   public async Task TearDown()
   {
       await CleanupApplicationAsync();
   }
   ```

### Test Configuration

**Database Setup:**
- Tests use real PostgreSQL database
- Connection string from `appsettings.json`
- Ensure database is running before testing

**Login Credentials:**
- Username: `Muhamed`
- Password: `SelmaSumejaAnja123!*`
- Configured in `CLAUDE.md`

## 🔧 Troubleshooting Tests

### Common Issues

1. **"Could not find main window"**
   - Ensure application builds successfully
   - Check database connection
   - Verify no other instances running

2. **UI Automation timeouts**
   - Increase timeout values in `BaseUITest.cs`
   - Check system performance
   - Close other applications during testing

3. **Database connection errors**
   - Verify PostgreSQL is running
   - Check connection string in `appsettings.json`
   - Ensure database schema is up to date

### Performance Tips

- **Run tests on dedicated machine** when possible
- **Close unnecessary applications** to improve performance
- **Use SSD storage** for faster database operations
- **Ensure sufficient RAM** (minimum 8GB recommended)

## 🎯 Future Testing Improvements

### Planned Enhancements

1. **Unit Tests Addition:**
   - Create separate unit test project
   - Test business logic without UI dependencies
   - Enable unit tests to run in CI

2. **Integration Tests:**
   - Database layer testing
   - Service integration testing
   - API endpoint testing (if applicable)

3. **Test Data Management:**
   - Automated test data setup/teardown
   - Test database isolation
   - Seed data generation

### Recommended Test Structure

```
tests/
├── DailyTaskManager.Unit.Tests/           # New - for CI
│   ├── Services/
│   ├── ViewModels/
│   └── Utilities/
├── DailyTaskManager.Integration.Tests/    # New - for CI
│   ├── Database/
│   ├── ExportServices/
│   └── Configuration/
└── DailyTaskManager.UI.Tests/            # Existing - local only
    ├── LoginTests/
    ├── MainWindowTests/
    ├── SettingsTests/
    └── ExportTests/
```

---

## 📞 Support

If you encounter issues with tests:

1. **Check this documentation first**
2. **Verify environment setup** (database, display, permissions)
3. **Run individual test classes** to isolate issues
4. **Check logs** in the test output for detailed error information

**Remember**: Always run the full UI test suite locally before creating releases to ensure quality! 🚀