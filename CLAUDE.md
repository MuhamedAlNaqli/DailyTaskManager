# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a modern cross-platform desktop application for managing daily tasks built with Avalonia UI and PostgreSQL database support. The application follows MVVM architecture with dependency injection and modern .NET practices.

## Architecture

The solution consists of three main projects:

- **DailyTaskManager.Avalonia** (.NET 8): Cross-platform UI application with Avalonia framework
- **DailyTaskManager.Core** (.NET Standard 2.0): Business logic, models, and interfaces
- **DailyTaskManager.Infrastructure** (.NET 8): Data access, services, and external integrations
- **DailyTaskManager.UI.Tests** (.NET 8): FlaUI-based UI automation tests

### Key Technologies
- **Avalonia UI 11.x** - Cross-platform .NET UI framework
- **Entity Framework Core** with PostgreSQL (Npgsql)
- **MVVM pattern** with CommunityToolkit.Mvvm (ObservableObject, RelayCommand)
- **Microsoft Extensions** - Dependency Injection, Logging, Configuration
- **Google OAuth 2.0** authentication integration
- **ClosedXML** for Excel export functionality
- **FlaUI** for automated UI testing

### Project Structure
- **Models**: `DailyTask`, `User`, and `Setting` entities with EF Core mappings
- **Services**: Authentication, database operations, OAuth, export services
- **ViewModels**: MVVM view models using CommunityToolkit.Mvvm attributes
- **Views**: Avalonia UserControls and Windows (.axaml files)
- **Infrastructure**: Database context, service implementations, configurations

## Common Commands

### Build Solution
**Preferred Method (Visual Studio 2022):**
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" DailyTaskManager.Avalonia.sln -p:Configuration=Debug -verbosity:minimal
```

**Alternative Methods using .NET CLI:**
```bash
dotnet build DailyTaskManager.Avalonia.sln --configuration Debug
```
or
```bash
dotnet build DailyTaskManager.Avalonia.sln --configuration Release
```

### Restore NuGet Packages
```bash
dotnet restore DailyTaskManager.Avalonia.sln
```

### Run Application
**Using dotnet run:**
```bash
cd src/DailyTaskManager.Avalonia
dotnet run
```

**Using built executable:**
```bash
src\DailyTaskManager.Avalonia\bin\Debug\net8.0\DailyTaskManager.Avalonia.exe
```

## Database Configuration

The application uses PostgreSQL with Entity Framework Core. Connection strings are configured through `appsettings.json` and environment variables. The database context is `DailyTaskManagerDbContext` located in `src/DailyTaskManager.Infrastructure/Data/DailyTaskManagerDbContext.cs`.

### Entity Mappings
- `DailyTask` maps to `public.daily_tasks` table
- `User` maps to `public.users` table with OAuth support
- `Setting` maps to `public.settings` table
- Uses EF Core fluent configuration and PostgreSQL-specific features

### Authentication
The application supports dual authentication:
- **Local Authentication**: Username/password stored in PostgreSQL
- **Google OAuth 2.0**: Integration with Google sign-in
- User management with proper security practices

## Avalonia Migration Highlights

### Modern Architecture Features
The application was completely migrated from WPF to Avalonia with significant improvements:

**Core Architectural Changes:**
- **Dependency Injection**: Full DI container using Microsoft.Extensions.DependencyInjection
- **Modern MVVM**: CommunityToolkit.Mvvm with source generators ([ObservableProperty], [RelayCommand])
- **Cross-platform Support**: Runs on Windows, macOS, and Linux
- **Avalonia UI 11.x**: Latest Avalonia features and performance improvements

**Authentication System Enhancement:**
- `LoginWindow.axaml` - Modern login interface with dual authentication
- `LoginViewModel.cs` - Handles both local and OAuth authentication
- `MainWindowViewModel.cs` - User session management and navigation
- OAuth integration with Google sign-in support

**Task Management Improvements:**
- `AddTaskDialog.axaml` - Modal dialog for task creation
- `EditTaskDialog.axaml` - Dedicated task editing interface
- `DailyTasksView.axaml` - Main task management view with filtering
- Enhanced validation and error handling

**UI/UX Enhancements:**
- Responsive sidebar navigation with proper alignment
- Theme switching (Light/Dark mode) with persistent preferences
- Improved form layouts and input validation
- Better loading states and user feedback

## UI Style Guide (Avalonia)

### Button Styling Standards

**Primary Action Buttons (Add, Save, etc.):**
```xml
<Button Content="Add Task" 
        Background="#FF4C566A" 
        BorderBrush="Transparent" 
        Foreground="White" 
        CornerRadius="6" 
        Padding="16,8" 
        FontWeight="Medium" 
        MinHeight="36"/>
```

**Secondary Action Buttons (Cancel, Export, etc.):**
```xml
<Button Content="Cancel" 
        Background="White" 
        BorderBrush="#FFD1D5DB" 
        Foreground="#FF374151" 
        CornerRadius="6" 
        Padding="16,8" 
        MinHeight="36"/>
```

**Navigation Buttons (Sidebar):**
```xml
<ToggleButton Classes="nav" Command="{Binding NavigateToTasksCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="📋" Margin="0,0,8,0"/>
        <TextBlock Text="Tasks"/>
    </StackPanel>
</ToggleButton>
```

**Critical Rules:**
- **USE consistent** `CornerRadius="6"` for modern rounded corners
- **DEFINE explicit** foreground colors for text contrast
- **MAINTAIN** consistent padding (`16,8` for normal buttons)
- **INCLUDE** `MinHeight="36"` for touch-friendly interfaces
- **USE** semantic color schemes (`#FF4C566A` for primary actions)

### Layout Standards (Avalonia)

**Main Window Grid Layout:**
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="240"/>  <!-- Fixed sidebar -->
        <ColumnDefinition Width="*"/>    <!-- Main content area -->
    </Grid.ColumnDefinitions>
</Grid>
```

**Responsive Content Layouts:**
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- Header/filters -->
        <RowDefinition Height="*"/>     <!-- Scrollable content -->
    </Grid.RowDefinitions>
</Grid>
```

**Form Dialog Layout:**
```xml
<Border Background="White" CornerRadius="12" Padding="32">
    <StackPanel Spacing="16">
        <!-- Form fields with consistent spacing -->
        <TextBox Watermark="Task description" Padding="12,10" FontSize="14"/>
        <Grid><!-- Date/Hours input grid --></Grid>
        
        <!-- Action buttons aligned right -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
            <Button Content="Cancel"/><!-- Secondary style -->
            <Button Content="Save"/><!-- Primary style -->
        </StackPanel>
    </StackPanel>
</Border>
```

### Namespace Conventions
- **ViewModels**: `DailyTaskManager.Avalonia.ViewModels`
- **Views**: `DailyTaskManager.Avalonia.Views`
- **Services**: `DailyTaskManager.Avalonia.Services`
- **Core Models**: `DailyTaskManager.Core.Models`
- **Core Interfaces**: `DailyTaskManager.Core.Interfaces`
- **Infrastructure Services**: `DailyTaskManager.Infrastructure.Services`
- **Data Access**: `DailyTaskManager.Infrastructure.Data`

### MVVM Pattern Requirements
- **No business logic in code-behind** - only event handling and UI interactions
- **Use RelayCommand** for all ICommand implementations
- **Implement INotifyPropertyChanged** for all ViewModels
- **Use event-driven communication** between ViewModels and Views
- **Async database operations** with proper error handling

## Development Notes

### Application Architecture
- **Main Window**: Navigation shell with sidebar for Tasks/Settings views
- **Authentication Flow**: LoginWindow → MainWindow with proper session management
- **Dependency Injection**: Services registered in `Program.cs` and accessed via DI container
- **Theme Support**: Light/Dark mode switching with persistent preferences
- **Cross-platform**: Tested on Windows, designed for macOS/Linux compatibility

### Data Access Patterns
- **Async/await**: All database operations use async patterns
- **Entity Framework Core**: Modern ORM with PostgreSQL provider
- **Repository Pattern**: Service layer abstracts data access logic
- **Connection Management**: Automatic connection pooling and lifecycle management

### UI Patterns
- **MVVM Binding**: Two-way data binding with proper change notifications
- **Dialog Management**: Modal dialogs with proper parent window relationships
- **Validation**: Input validation with user-friendly error messages
- **Loading States**: Progress indicators and disabled states during operations

### Best Practices
- **Error Handling**: Comprehensive try-catch with logging
- **Resource Management**: Proper disposal of resources and database connections
- **Performance**: Lazy loading and efficient data queries
- **Security**: OAuth integration and secure credential handling

## **MANDATORY BUILD VERIFICATION**

**🚨 CRITICAL REQUIREMENT: Always build the project before finishing everything just to make sure everything works properly.**

Before completing any task or claiming work is finished, you **MUST** perform the following verification steps:

### 1. Clean and Restore Solution
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "D:\Internal Projects\DailyTaskManager\DailyTaskManager.Avalonia.sln" -t:Clean
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "D:\Internal Projects\DailyTaskManager\DailyTaskManager.Avalonia.sln" -t:Restore -verbosity:minimal
```

### 2. Build Solution (Zero Tolerance for Errors)
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "D:\Internal Projects\DailyTaskManager\DailyTaskManager.Avalonia.sln" -p:Configuration=Debug -verbosity:minimal
```

**Build Success Criteria:**
- ✅ **ZERO build errors** (warnings are acceptable)
- ✅ All projects compile successfully
- ✅ No missing references or assembly issues

### 3. If Build Fails
- 🔴 **DO NOT** claim task completion
- 🔴 **DO NOT** move to next steps
- ✅ **FIX** all compilation errors immediately
- ✅ **RETRY** build verification process
- ✅ **DOCUMENT** what was broken and how it was fixed

### 4. Package Restore Issues
If you encounter package/assembly issues:
```bash
dotnet restore --force
dotnet nuget locals all --clear
```

## UI Testing Workflow for Rapid Iteration

The project includes comprehensive FlaUI-based UI tests for **rapid iteration development**. This enables a continuous feedback loop: **Change → Test → Fix → Repeat**.

### UI Test Architecture

**Test Project:** `tests\DailyTaskManager.UI.Tests\DailyTaskManager.UI.Tests.csproj`
- **Framework:** MSTest + FlaUI for Avalonia UI automation
- **Target:** `net8.0-windows` (required for Accessibility API)
- **Key Dependencies:** FlaUI.Core, FlaUI.UIA3, Windows Desktop App framework

### Core Test Infrastructure

**AppLauncher.cs** - Handles application lifecycle:
```csharp
// Launch app and get main window
var launcher = new AppLauncher();
var mainWindow = await launcher.LaunchAsync();

// Automatic login detection and handling
if (mainWindow.Title.Contains("Login"))
{
    await LoginAsync("admin", "admin123");
}
```

**BaseUITest.cs** - Provides common functionality:
- Automatic application startup/cleanup
- Login integration for authenticated tests
- Window management and navigation
- Screenshot capture for debugging

### Rapid Iteration Workflow

#### 1. **Make Code Changes**
- Modify ViewModels, Views, or business logic
- Save changes to source files

#### 2. **Run UI Tests** (Immediate Feedback)
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "D:\Internal Projects\DailyTaskManager\tests\DailyTaskManager.UI.Tests\DailyTaskManager.UI.Tests.csproj" -p:Configuration=Debug -verbosity:minimal
dotnet test "tests\DailyTaskManager.UI.Tests\bin\Debug\net8.0-windows\DailyTaskManager.UI.Tests.dll" --logger:console
```

#### 3. **Analyze Test Results**
- ✅ **All tests pass**: Changes work correctly, continue development
- ❌ **Tests fail**: Identify specific issues from test output
- 📸 **Screenshot debugging**: Use `TakeScreenshot()` for visual verification

#### 4. **Fix Issues and Iterate**
- Address failing tests immediately
- Re-run specific test methods for quick validation
- Continue rapid iteration cycle until stable

### Test Categories and Usage

#### **TaskManagementTests.cs** - Core functionality testing
```csharp
[TestMethod]
public async Task CanAddNewTask() // Test task creation workflow
[TestMethod]
public async Task CanEditExistingTask() // Test task modification
[TestMethod]
public async Task DateFiltersWorkCorrectly() // Test date filtering (key issue area)
```

#### **ValidationTests.cs** - Input validation and error handling
```csharp
[TestMethod]
public async Task RequiredFieldsShowValidation() // Test form validation
[TestMethod]
public async Task InvalidDataShowsErrors() // Test error states
```

#### **ExportTests.cs** - Export functionality verification
```csharp
[TestMethod]
public async Task CanExportTasksToExcel() // Test Excel export workflow
```

### FlaUI Best Practices for Avalonia

#### Element Interaction Patterns
```csharp
// Text input (Avalonia-specific)
var textBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TaskNameTextBox"));
textBox.Click(); // Focus first
await Task.Delay(200); // Allow focus to settle
textBox.Text = "New Task Name"; // Direct assignment (not .Enter())

// Button clicking
var saveButton = mainWindow.FindFirstDescendant(cf => cf.ByName("Save"));
saveButton.Click();

// Checkbox interaction
var checkbox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("IsCompletedCheckBox"));
var isChecked = checkbox.AsCheckBox().IsChecked; // Read state
checkbox.AsCheckBox().IsChecked = true; // Set state
```

#### Window and Dialog Handling
```csharp
// Wait for dialogs to appear
var dialog = await mainWindow.WaitForDialog("Add Task", timeout: 5000);

// Handle modal dialogs
var addTaskDialog = Application.GetAllTopLevelWindows(Automation)
    .FirstOrDefault(w => w.Title.Contains("Add Task"));
```

### Debugging UI Tests

#### Enable Verbose Logging
```csharp
// In test methods
Console.WriteLine($"🔍 Looking for element: {elementName}");
launcher.PrintUITree(); // Print entire UI structure
launcher.TakeScreenshot($"debug_{DateTime.Now:HHmmss}.png");
```

#### Common Issues and Solutions

**Problem:** Element not found
```csharp
// Solution: Wait for element availability
var element = await Retry.WhileNull(
    () => mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("ElementId")),
    timeout: TimeSpan.FromSeconds(10)
);
```

**Problem:** Timing issues with UI updates
```csharp
// Solution: Add strategic delays
await Task.Delay(500); // Allow UI to settle after actions
element.WaitUntilEnabled(timeout: 5000); // Wait for element state
```

### Integration with Development Process

#### Before Committing Changes
1. ✅ **Build verification** (mandatory)
2. ✅ **Run full UI test suite**
3. ✅ **Verify critical user workflows**
4. ✅ **Check for regressions in existing functionality**

#### During Feature Development
1. 🔄 **Write failing tests first** (TDD approach)
2. 🔄 **Implement feature incrementally**
3. 🔄 **Run tests after each small change**
4. 🔄 **Refactor with test coverage safety net**

#### Bug Fixing Workflow
1. 🐛 **Reproduce bug with UI test**
2. 🐛 **Create specific test case for the bug**
3. 🐛 **Fix the underlying issue**
4. 🐛 **Verify test passes consistently**
5. 🐛 **Run regression tests**

This workflow ensures **continuous quality** and **rapid feedback** during development, preventing the accumulation of bugs and reducing debugging time.

## **MANDATORY UI TESTING FOR ALL UI CHANGES**

**🚨 CRITICAL REQUIREMENT: Every UI integration and feature MUST have a corresponding UI test that validates the changes.**

### UI Testing Mandate

For **EVERY** UI change, integration, or new feature, you **MUST**:

#### 1. **Code → Test → Code Flow**
- ✅ **NEVER** claim UI work is complete without corresponding tests
- ✅ **ALWAYS** follow: Write Code → Write Test → Verify → Iterate
- ✅ **VALIDATE** all UI changes through automated tests

#### 2. **Mandatory Test Coverage Areas**

**Login Window Tests:**
- Window dimensions and layout (no scrolling required)
- Text box padding and spacing validation
- All interactive elements accessibility (buttons, fields, links)
- Card layout as window structure verification
- Cross-browser/platform rendering consistency

**Main Window Sidebar Tests:**
- Icon alignment (left) and text alignment (right) validation  
- Logout button positioning (bottom of sidebar)
- Light/Dark mode button positioning (bottom, above logout)
- User welcome text positioning and truncation
- Navigation link functionality and layout consistency

**General UI Component Tests:**
- Button sizing, padding, and interaction states
- Form field validation and error display
- Modal dialog positioning and behavior
- Responsive layout behavior at different window sizes
- Theme switching functionality

#### 3. **UI Test Implementation Requirements**

**Test File Structure:**
```csharp
[TestClass]
public class LoginWindowLayoutTests : BaseUITest
{
    [TestMethod]
    public async Task LoginWindow_Should_DisplayAllElementsWithoutScrolling()
    
    [TestMethod]  
    public async Task LoginWindow_TextBoxes_Should_HaveProperPadding()
    
    [TestMethod]
    public async Task LoginWindow_Should_UseCardAsWindowLayout()
}

[TestClass]
public class MainWindowSidebarTests : BaseUITest  
{
    [TestMethod]
    public async Task Sidebar_Icons_Should_BeLeftAligned()
    
    [TestMethod]
    public async Task Sidebar_LogoutButton_Should_BeAtBottom()
    
    [TestMethod]
    public async Task Sidebar_UserWelcome_Should_ShowInCorrectPosition()
}
```

#### 4. **Visual Validation Through Automation**

**Screenshot Verification:**
```csharp
// Capture screenshots for visual regression testing
AppLauncher.TakeScreenshot($"login-window-layout-{TestContext.TestName}.png");

// Validate window dimensions
Assert.AreEqual(400, loginWindow.BoundingRectangle.Width, "Login window width incorrect");
Assert.AreEqual(520, loginWindow.BoundingRectangle.Height, "Login window height incorrect");

// Validate element positioning
var usernameBox = FindElement("UsernameTextBox");
Assert.IsTrue(usernameBox.BoundingRectangle.X > 10, "Username box padding insufficient");
```

#### 5. **Test Execution Requirements**

**Before Any UI Commit:**
```bash
# Build tests
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" "tests\DailyTaskManager.UI.Tests\DailyTaskManager.UI.Tests.csproj" -p:Configuration=Debug -verbosity:minimal

# Run UI layout tests specifically  
dotnet test "tests\DailyTaskManager.UI.Tests\bin\Debug\net8.0-windows\DailyTaskManager.UI.Tests.dll" --filter "TestCategory=UILayout" --logger:console

# Run all UI tests
dotnet test "tests\DailyTaskManager.UI.Tests\bin\Debug\net8.0-windows\DailyTaskManager.UI.Tests.dll" --logger:console
```

#### 6. **Failure Response Protocol**

**If UI Tests Fail:**
- 🔴 **DO NOT** commit or claim work complete
- 🔴 **DO NOT** ignore test failures as "flaky"
- ✅ **INVESTIGATE** root cause immediately
- ✅ **FIX** both code and tests until green
- ✅ **DOCUMENT** what was wrong and how it was resolved

#### 7. **Test Documentation Requirements**

Each UI test **MUST** include:
- **Clear test name** describing what UI aspect is being validated
- **Detailed assertions** for layout, positioning, and behavior
- **Screenshot capture** for visual verification  
- **Comments** explaining expected UI behavior
- **Timeout handling** for reliable test execution

This ensures that UI changes are **validated programmatically** and **regressions are caught immediately** rather than discovered by users.