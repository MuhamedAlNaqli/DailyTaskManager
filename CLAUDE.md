# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a WPF desktop application for managing daily tasks with PostgreSQL database support. The application follows MVVM architecture and uses Entity Framework 6 for data access.

## Architecture

The solution consists of two main projects:

- **DailyTaskManager.App** (.NET Framework 4.8): WPF presentation layer with Material Design UI
- **DailyTaskmanager.Core** (.NET Framework 4.8): Business logic and data access layer

### Key Technologies
- WPF with Material Design themes
- Entity Framework 6 with PostgreSQL (Npgsql)
- MVVM pattern with INotifyPropertyChanged
- ClosedXML for Excel export functionality

### Project Structure
- **Models**: `DailyTask` and `Setting` entities with PostgreSQL table mappings
- **Services**: `DatabaseService` for DailyTask CRUD operations, `SettingsService` for Settings CRUD operations
- **ViewModels**: MVVM view models with `RelayCommand` implementation
- **Views**: WPF UserControls (`DailyTasksView`, `SettingsView`)
- **Export**: Excel export functionality using ClosedXML

## Common Commands

### Build Solution
**Preferred Method (Visual Studio 2022):**
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" DailyTaskManager.sln -p:Configuration=Debug -verbosity:minimal
```

**Alternative Methods:**
```bash
msbuild DailyTaskManager.sln /p:Configuration=Debug
```
or
```bash
msbuild DailyTaskManager.sln /p:Configuration=Release
```

**Note:** If using .NET CLI, use Visual Studio Developer Command Prompt or VS MSBuild to avoid .NET Framework compatibility issues.

### Restore NuGet Packages
```bash
nuget restore DailyTaskManager.sln
```

### Run Application
Build and run the executable from:
```
DailyTaskManager.App\bin\Debug\DailyTaskManager.exe
```
or
```
DailyTaskManager.App\bin\Release\DailyTaskManager.exe
```

## Database Configuration

The application uses PostgreSQL with Entity Framework 6. Connection strings are configured in App.config files. The database context is `DailyTaskManagerDbContext` located in `DailyTaskmanager.Core\DbContext.cs`.

### Entity Mappings
- `DailyTask` maps to `public.daily_tasks` table
- Uses PostgreSQL-specific schema and column attributes

## Recent Refactoring Changes (MVVM Architecture Enhancement)

### Add Task Functionality Separation
The application was refactored to move add task functionality from inline form to dedicated window:

**New Components Added:**
- `AddTaskWindow.xaml` - Modal dialog for adding new daily tasks
- `AddTaskWindow.xaml.cs` - Code-behind with event handling and dialog management
- `AddTaskViewModel.cs` - Dedicated ViewModel with validation and save logic
- `TaskSavedEventArgs.cs` - Event args for task saved notifications

**Modified Components:**
- `DailyTasksView.xaml` - Removed inline add form, added "Add New" button
- `DailyTaskViewModel.cs` - Replaced SaveCommand/ClearCommand with AddNewTaskCommand
- Navigation pattern: Main view → AddTaskWindow dialog → Auto-refresh main view

**Key Benefits:**
- Clean separation of concerns following MVVM principles
- Modal dialog improves UX and focus
- Automatic data refresh after task creation
- Better validation and error handling
- Consistent Material Design styling

### Layout Improvements
- **DataGrid Height Fix**: Changed from StackPanel to Grid layout with proper row definitions for full-height responsiveness
- **Button Consistency**: Standardized button heights (MinHeight="36") and padding
- **Material Design Compliance**: All buttons use consistent styling patterns

## UI Style Guide

### Button Styling Standards

**Primary Action Buttons (Add New, Save, etc.):**
```xml
<Button Style="{StaticResource MaterialDesignOutlinedButton}"
        Background="{DynamicResource PrimaryHueMidBrush}"
        Padding="12,8"
        MinHeight="36">
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
        <materialDesign:PackIcon Kind="Plus" Width="20" Height="20" Margin="0,0,5,0"/>
        <TextBlock Text="Add New" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

**Secondary Action Buttons (Export, Cancel, etc.):**
```xml
<Button Style="{StaticResource MaterialDesignOutlinedButton}"
        Background="{DynamicResource SecondaryHueMidBrush}"
        Padding="12,8"
        MinHeight="36">
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
        <materialDesign:PackIcon Kind="FileExcelOutline" Width="20" Height="20" Margin="0,0,5,0"/>
        <TextBlock Text="Export to Excel" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

**Critical Rules:**
- **ALWAYS use** `MaterialDesignOutlinedButton` style for consistent text visibility
- **NEVER use** `MaterialDesignRaisedButton` with custom backgrounds (causes invisible text)
- **NEVER add** explicit `Foreground` properties - let Material Design theme handle colors
- **ALWAYS include** `MinHeight="36"` for consistent button sizing
- **ALWAYS use** StackPanel with Icon + TextBlock pattern for buttons with icons

### Layout Standards

**Responsive Grid Layouts:**
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- Fixed height content (buttons, filters) -->
        <RowDefinition Height="*"/>     <!-- Expandable content (DataGrid, main content) -->
    </Grid.RowDefinitions>
</Grid>
```

**Form Dialog Layout (Follow EditTaskWindow pattern):**
```xml
<materialDesign:Card Padding="20">
    <StackPanel>
        <!-- Form fields with consistent spacing -->
        <TextBox Margin="0 0 0 15" Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
        <Grid Margin="0 0 0 15"><!-- Date/Hours grid --></Grid>
        
        <!-- Action buttons aligned right -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <!-- Cancel button, Save button -->
        </StackPanel>
    </StackPanel>
</materialDesign:Card>
```

### Namespace Conventions
- All ViewModels: `DailyTaskManager.App.ViewModels`
- All Views: `DailyTaskManager.App.Views` 
- All Windows: `DailyTaskManager.App`
- Core Services: `DailyTaskManager.Core.Services`
- Models: `DailyTaskManager.Core.Models`

### MVVM Pattern Requirements
- **No business logic in code-behind** - only event handling and UI interactions
- **Use RelayCommand** for all ICommand implementations
- **Implement INotifyPropertyChanged** for all ViewModels
- **Use event-driven communication** between ViewModels and Views
- **Async database operations** with proper error handling

## Development Notes

- The main window serves as a navigation shell between DailyTask and Settings pages
- Uses Material Design themes for consistent UI styling
- Excel export saves files to user's Desktop with Serbian language headers
- All database operations are async using Entity Framework patterns
- The codebase follows standard C# naming conventions and MVVM patterns
- Modal dialogs should always set `Owner = Application.Current.MainWindow`
- Use `ShowDialog()` return values for proper dialog result handling