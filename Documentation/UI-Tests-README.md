# FlaUI UI Automation Tests

This project contains UI automation tests for the Daily Task Manager Avalonia application using FlaUI.

## Setup Complete ✅

### What's Implemented
- **FlaUI Test Infrastructure**: Complete setup with FlaUI.Core 5.0.0 and FlaUI.UIA3 5.0.0
- **Base Test Classes**: 
  - `AppLauncher`: Handles launching and managing the application
  - `BaseUITest`: Provides common test functionality and setup/teardown
- **Test Classes**:
  - `SmokeTests`: Basic application launch and login tests
  - `SimplifiedDateFilteringTests`: Date filter exploration and interaction tests

### Project Structure
```
tests/DailyTaskManager.UI.Tests/
├── Infrastructure/
│   ├── AppLauncher.cs          # Application lifecycle management
│   └── BaseUITest.cs           # Base test functionality
├── SmokeTests.cs               # Basic smoke tests
├── SimplifiedDateFilteringTests.cs # Date filtering UI tests
└── README.md                   # This file
```

### Available Tests
1. `Application_Should_Launch_Successfully` - Verifies app launches
2. `Login_Should_Work_Successfully` - Tests login functionality
3. `UI_Elements_Should_Be_Present_After_Login` - Enumerates UI elements
4. `CanInteractWithDateFilters_Basic` - Explores date filtering UI
5. `VerifyApplicationResponsiveness` - Tests UI stability

## Usage

### Running Tests
```bash
# Navigate to test directory
cd "tests/DailyTaskManager.UI.Tests"

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "Application_Should_Launch_Successfully"

# List all tests
dotnet test --list-tests
```

### Key Features
- **Automatic Application Management**: Tests launch fresh instances and clean up automatically
- **Login Automation**: Automated login with admin/admin123 credentials  
- **UI Discovery**: Comprehensive UI element enumeration and debugging
- **Error Handling**: Robust error handling and logging
- **Screenshot Placeholders**: Infrastructure ready for screenshot capture

### Date Filtering Testing
The `SimplifiedDateFilteringTests` class specifically targets the date filtering functionality:
- Searches for date-related UI elements
- Attempts to interact with "Test Date Filter" buttons
- Provides detailed logging of UI structure
- Validates application responsiveness

## Integration with Development

This FlaUI setup provides:
1. **Automated Verification** of the date filtering fix implementation
2. **UI Structure Analysis** for understanding Avalonia control hierarchy
3. **Regression Testing** capabilities for future changes
4. **Debugging Support** through comprehensive UI element enumeration

## Running Tests

### Option 1: PowerShell Script (Recommended)
```bash
# Run all tests with build
.\run-ui-tests.ps1

# Run specific test class
.\run-ui-tests.ps1 -TestFilter "TaskManagementTests"

# Run without building first (faster for repeated runs)
.\run-ui-tests.ps1 -BuildFirst:$false

# Run with verbose output
.\run-ui-tests.ps1 -Verbose
```

### Option 2: Batch Script
```bash
# Run all tests with Debug configuration
.\run-ui-tests.bat

# Run with Release configuration
.\run-ui-tests.bat Release
```

### Option 3: Direct dotnet command
```bash
# Build Avalonia app first
cd "..\..\src\DailyTaskManager.Avalonia"
dotnet build -c Debug

# Return to test directory and run tests
cd "..\..\tests\DailyTaskManager.UI.Tests"
dotnet test -c Debug --settings runsettings.xml
```

### Test Categories

1. **SmokeTests** - Basic application functionality
   - Application launch
   - Login functionality
   - UI element enumeration

2. **TaskManagementTests** - Core task operations
   - Add task functionality
   - Task list display
   - Edit task operations
   - Search/filter functionality
   - Task status changes

3. **ValidationTests** - Data validation and error handling
   - Invalid login credentials
   - Empty required fields
   - Date field validation
   - Database connection errors
   - Long text field limits
   - Concurrent operation handling

4. **ExportTests** - Export functionality
   - Export button availability
   - File creation verification
   - Filtered data export
   - Empty list handling
   - Progress indication
   - Error handling

5. **SimplifiedDateFilteringTests** - Date filtering (existing)
   - Date filter UI interaction
   - Application responsiveness

## Integration with Development

This comprehensive UI test suite provides:

1. **Automated Verification** of all major application features
2. **Regression Testing** for future changes
3. **UI Structure Analysis** for debugging Avalonia controls
4. **Error Handling Validation** for robust application behavior
5. **Export Functionality Testing** for data integrity
6. **Performance Testing** through responsiveness checks

## CI/CD Integration

To integrate with your build pipeline:

1. **Azure DevOps**: Use the TRX and HTML loggers for test reporting
2. **GitHub Actions**: Use the runsettings.xml for consistent test execution
3. **Jenkins**: Use the batch script for Windows agents

## Next Steps

You can now:
1. **Run comprehensive UI tests** across all application features
2. **Add more specific test scenarios** as features evolve
3. **Integrate with CI/CD** for automated regression testing
4. **Monitor test results** through detailed logging and reporting
5. **Debug UI issues** using the comprehensive element discovery tools

The complete UI testing infrastructure is now in place! 🎯