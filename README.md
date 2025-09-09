# Daily Task Manager

A modern, cross-platform desktop application for managing daily tasks, built with Avalonia UI and .NET 8. Features dual authentication (local and Google OAuth), real-time task management, and comprehensive export capabilities.

## ✨ Features

### 🔐 Dual Authentication System
- **Local Authentication**: Traditional username/password with secure storage
- **Google OAuth 2.0**: Sign in with your Google account
- Persistent login sessions with "Remember Me" functionality

### 📋 Task Management
- Create, edit, and delete daily tasks
- Real-time task completion tracking
- Advanced filtering by date ranges and completion status
- Bulk operations and task search functionality

### 🎨 Modern UI/UX
- **Cross-platform**: Runs on Windows, macOS, and Linux
- **Responsive Design**: Adaptive layouts for different screen sizes
- **Theme Support**: Light and Dark mode with automatic system detection
- **Accessibility**: Full keyboard navigation and screen reader support

### 📊 Export & Reporting
- Export tasks to Excel (.xlsx) format
- Customizable date ranges and filters
- Serbian language support for exported headers
- Automatic file organization

### 🔧 Technical Excellence
- Modern MVVM architecture with dependency injection
- Comprehensive UI automation testing with FlaUI
- PostgreSQL database with Entity Framework Core
- Robust error handling and logging

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- [Git](https://git-scm.com/downloads)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/DailyTaskManager.git
   cd DailyTaskManager
   ```

2. **Setup Database**
   ```bash
   # Create PostgreSQL database
   createdb daily_task_manager
   
   # Update connection string in appsettings.json
   ```

3. **Install Dependencies**
   ```bash
   dotnet restore DailyTaskManager.Avalonia.sln
   ```

4. **Build the Application**
   ```bash
   dotnet build DailyTaskManager.Avalonia.sln --configuration Release
   ```

5. **Run the Application**
   ```bash
   cd src/DailyTaskManager.Avalonia
   dotnet run
   ```

## 🏗️ Project Structure

```
DailyTaskManager/
├── src/
│   ├── DailyTaskManager.Avalonia/          # UI Layer (Avalonia UI)
│   │   ├── Views/                          # XAML Views and Windows
│   │   ├── ViewModels/                     # MVVM ViewModels
│   │   └── Services/                       # UI-specific services
│   ├── DailyTaskManager.Core/              # Business Logic (.NET Standard 2.0)
│   │   ├── Models/                         # Domain entities
│   │   ├── Interfaces/                     # Service contracts
│   │   └── DTOs/                          # Data transfer objects
│   └── DailyTaskManager.Infrastructure/    # Data & External Services (.NET 8)
│       ├── Data/                          # EF Core DbContext
│       ├── Services/                      # Service implementations
│       └── Migrations/                    # Database migrations
├── tests/
│   ├── DailyTaskManager.UI.Tests/         # FlaUI UI automation tests
│   └── DailyTaskManager.Tests/            # Unit tests
└── docs/                                  # Documentation
```

## 🛠️ Development

### Development Setup

1. **Install development tools**
   ```bash
   # Install Entity Framework tools
   dotnet tool install --global dotnet-ef
   
   # Install development certificates (Windows)
   dotnet dev-certs https --trust
   ```

2. **Database Migrations**
   ```bash
   # Add new migration
   dotnet ef migrations add MigrationName --project src/DailyTaskManager.Infrastructure
   
   # Update database
   dotnet ef database update --project src/DailyTaskManager.Infrastructure
   ```

3. **Running Tests**
   ```bash
   # Build test projects
   dotnet build tests/DailyTaskManager.UI.Tests/DailyTaskManager.UI.Tests.csproj
   
   # Run UI automation tests
   dotnet test tests/DailyTaskManager.UI.Tests/ --logger:console
   
   # Run unit tests
   dotnet test tests/DailyTaskManager.Tests/ --logger:console
   ```

### Architecture Overview

The application follows **Clean Architecture** principles:

- **Presentation Layer** (`DailyTaskManager.Avalonia`): Avalonia UI with MVVM pattern
- **Application Layer** (`DailyTaskManager.Core`): Business logic and interfaces
- **Infrastructure Layer** (`DailyTaskManager.Infrastructure`): Data access and external services

#### Key Design Patterns
- **MVVM**: Separation of UI and business logic
- **Dependency Injection**: Service registration and resolution
- **Repository Pattern**: Abstract data access layer  
- **Command Pattern**: User actions through RelayCommands

## 🧪 Testing Strategy

### UI Automation Testing
Uses **FlaUI** for comprehensive UI testing:

```bash
# Run specific test category
dotnet test --filter "TestCategory=UILayout" --logger:console

# Run login-specific tests
dotnet test --filter "FullyQualifiedName~LoginWindow" --logger:console
```

### Test Categories
- **UILayout**: Window sizing, element positioning, responsive behavior
- **Authentication**: Login/logout workflows, OAuth integration
- **TaskManagement**: CRUD operations, filtering, validation
- **Export**: Excel generation and file operations

## 🔧 Configuration

### Application Settings

Configure the application via `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=daily_task_manager;Username=your_user;Password=your_password"
  },
  "GoogleOAuth": {
    "ClientId": "your-google-client-id.googleusercontent.com",
    "ClientSecret": "your-google-client-secret"
  },
  "AppSettings": {
    "Version": "2.0.0",
    "DefaultTheme": "Light"
  }
}
```

### Environment Variables

For production deployments:

```bash
# Database connection
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=daily_task_manager;..."

# OAuth credentials
export GoogleOAuth__ClientId="your-production-client-id"
export GoogleOAuth__ClientSecret="your-production-client-secret"
```

## 📦 Deployment

### Windows

```bash
# Publish self-contained executable
dotnet publish src/DailyTaskManager.Avalonia -c Release -r win-x64 --self-contained true

# Output: src/DailyTaskManager.Avalonia/bin/Release/net8.0/win-x64/publish/
```

### macOS

```bash
# Publish for macOS
dotnet publish src/DailyTaskManager.Avalonia -c Release -r osx-x64 --self-contained true

# Create macOS app bundle (requires additional tooling)
```

### Linux

```bash
# Publish for Linux
dotnet publish src/DailyTaskManager.Avalonia -c Release -r linux-x64 --self-contained true

# Create .deb package (requires additional tooling)
```

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Follow coding standards** (see `CLAUDE.md` for detailed guidelines)
4. **Write comprehensive tests** for UI changes
5. **Ensure all tests pass** (`dotnet test`)
6. **Commit your changes** (`git commit -m 'Add amazing feature'`)
7. **Push to the branch** (`git push origin feature/amazing-feature`)
8. **Open a Pull Request**

### Code Standards

- **UI Changes**: Must include FlaUI automation tests
- **Database Changes**: Require proper migrations
- **Documentation**: Update relevant docs for new features
- **Performance**: Profile and optimize resource-intensive operations

See [`CLAUDE.md`](./CLAUDE.md) for detailed development guidelines and architecture documentation.

## 📋 Prerequisites for Development

- **Visual Studio 2022** or **JetBrains Rider** (recommended IDEs)
- **PostgreSQL** with development database
- **Google Cloud Console** account (for OAuth setup)
- **FlaUI Test Environment** (Windows required for UI tests)

## 🐛 Troubleshooting

### Common Issues

**Build Errors:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

**Database Connection:**
```bash
# Test PostgreSQL connection
psql -h localhost -U your_user -d daily_task_manager -c "SELECT version();"
```

**UI Test Failures:**
- Ensure application is not running during tests
- Check display scaling settings (100% recommended)
- Verify Windows Accessibility features are enabled

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [FlaUI](https://github.com/FlaUI/FlaUI) - UI automation library
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Modern ORM
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) - MVVM helpers

---

**Built with ❤️ using Avalonia UI and .NET 8**