# Process Monitor API

A .NET 10 Web API that analyzes actions against guidelines using AI classification through HuggingFace models. The API stores analysis results in a SQLite database and provides endpoints for retrieving history and summaries.

## Features

- **AI-Powered Analysis**: Classify actions against guidelines using HuggingFace's BART-MNLI model
- **Smart Caching**: Automatically returns cached results for duplicate analyses
- **Persistent Storage**: SQLite database for storing analysis results with Entity Framework Core
- **History Tracking**: Retrieve past analyses ordered by creation date
- **Summary Statistics**: Get aggregated counts by classification result
- **Health Checks**: Liveness and readiness probes for monitoring
- **Metrics**: Prometheus metrics endpoint for observability
- **API Documentation**: Swagger/OpenAPI documentation
- **Resilience**: Built-in retry and circuit breaker patterns for external API calls
- **Comprehensive Testing**: Unit tests with xUnit, Moq, and FluentAssertions

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Setup Instructions](#setup-instructions)
- [Running the Application](#running-the-application)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [Database](#database)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Development](#development)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Required)
- [Visual Studio 2025 Preview](https://visualstudio.microsoft.com/vs/preview/) or [Visual Studio Code](https://code.visualstudio.com/) (Recommended)
- [HuggingFace API Token](https://huggingface.co/settings/tokens) (Required - free tier available)
- [Git](https://git-scm.com/) (For cloning the repository)

## Project Structure

```
ProcessMonitor/
??? Host/
?   ??? ProcessMonitorAPI/              # Web API Host Project
?       ??? Controllers/
?       ?   ??? AIController.cs         # API endpoints
?       ??? Program.cs                  # Application entry point
?       ??? Startup.cs                  # Service configuration & middleware
?       ??? appsettings.json            # Configuration settings
??? Domain/
?   ??? ProcessMonitorApi/              # Domain/Business Logic Project
?       ??? Contracts/                  # Data contracts & entities
?       ?   ??? Analysis.cs             # Analysis entity
?       ?   ??? ClassificationResponse.cs
?       ??? Mappers/                    # Object mapping
?       ?   ??? Interfaces/
?       ?   ?   ??? IAnalysisMapper.cs
?       ?   ??? Implementations/
?       ?       ??? AnalysisMapper.cs
?       ??? Models/                     # DTOs & models
?       ?   ??? AnalysisRequest.cs
?       ?   ??? AnalysisResponse.cs
?       ?   ??? AnalysesSummaryResponse.cs
?       ??? Operations/                 # Business operations
?       ?   ??? Interfaces/
?       ?   ?   ??? IAnalyzeOperation.cs
?       ?   ??? Implementations/
?       ?       ??? AnalyzeOperation.cs
?       ??? Repository/                 # Data access layer
?       ?   ??? AppDbContext.cs         # EF Core context
?       ?   ??? AnalysisConfiguration.cs # Entity configuration
?       ?   ??? ISQLiteRepository.cs
?       ?   ??? SQLiteRepository.cs
?       ??? Services/                   # External service integrations
?           ??? Interfaces/
?           ?   ??? IHuggingFaceClassificationService.cs
?           ??? Implementations/
?               ??? HuggingFaceClassificationService.cs
??? Tests/
?   ??? Domain.UnitTests/               # Unit Tests Project
?       ??? Mappers/
?       ?   ??? AnalysisMapperTests.cs  # Mapper unit tests (13 tests)
?       ??? Operations/
?           ??? AnalyzeOperationTests.cs # Operation unit tests (17 tests)
??? README.md
```

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd ProcessMonitor
```

### 2. Configure HuggingFace API Token

You have three options for configuring your HuggingFace API token:

#### Option A: User Secrets (Recommended for Development)

```bash
cd Host/ProcessMonitorAPI
dotnet user-secrets init
dotnet user-secrets set "HuggingFaceSettings:TokenValue" "your-huggingface-token-here"
```

#### Option B: Environment Variables (Recommended for Production)

**Windows (PowerShell):**
```powershell
$env:HuggingFaceSettings__TokenValue="your-huggingface-token-here"
```

**Linux/macOS:**
```bash
export HuggingFaceSettings__TokenValue="your-huggingface-token-here"
```

#### Option C: appsettings.json (Not Recommended - For Testing Only)

Update `Host/ProcessMonitorAPI/appsettings.json`:

```json
{
  "HuggingFaceSettings": {
    "Model": "facebook/bart-large-mnli",
    "TokenValue": "your-huggingface-token-here",
    "Endpoint": "https://router.huggingface.co/"
  }
}
```

?? **Important**: Never commit your actual API token to version control!

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Solution

```bash
dotnet build
```

The database will be automatically created on first run.

## Running the Application

### Development Mode

#### Using Command Line

From the solution root:

```bash
cd Host/ProcessMonitorAPI
dotnet run
```

Or directly:

```bash
dotnet run --project Host/ProcessMonitorAPI/Host.ProcessMonitorApi.csproj
```

#### Using Visual Studio

1. Open `ProcessMonitor.sln`
2. Set `Host.ProcessMonitorApi` as the startup project
3. Press `F5` or click the "Start" button

#### Using Visual Studio Code

1. Open the workspace folder
2. Press `F5` or use the "Run and Debug" panel
3. Select ".NET Core Launch (web)"

### Default URLs

The API will start on:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`

### Access Swagger Documentation

Once running, navigate to:
```
https://localhost:5001/swagger
```

### Production Mode

```bash
dotnet run --project Host/ProcessMonitorAPI/Host.ProcessMonitorApi.csproj --configuration Release
```

## API Endpoints

### POST /analyze

Analyzes an action against a guideline using AI classification. Returns cached result if the same action/guideline combination was previously analyzed.

**Request Body:**
```json
{
  "action": "User deleted a customer record without approval",
  "guideline": "All customer data deletions must be approved by a supervisor"
}
```

**Response:**
```json
{
  "action": "User deleted a customer record without approval",
  "guideline": "All customer data deletions must be approved by a supervisor",
  "result": "DEVIATES",
  "confidence": 0.95,
  "timeStamp": "2025-01-15T10:30:00Z"
}
```

**Classification Results:**
- `COMPLIES` - Action follows the guideline
- `DEVIATES` - Action contradicts the guideline
- `UNCLEAR` - Relationship is ambiguous or insufficient information

**Status Codes:**
- `200 OK` - Analysis completed successfully
- `400 Bad Request` - Invalid request (missing action or guideline)
- `500 Internal Server Error` - Server error

### GET /history

Retrieves all past analyses ordered by creation date (newest first).

**Response:**
```json
[
  {
    "action": "User deleted a customer record without approval",
    "guideline": "All customer data deletions must be approved by a supervisor",
    "result": "DEVIATES",
    "confidence": 0.95,
    "timeStamp": "2025-01-15T10:30:00Z"
  },
  {
    "action": "User requested supervisor approval before deletion",
    "guideline": "All customer data deletions must be approved by a supervisor",
    "result": "COMPLIES",
    "confidence": 0.98,
    "timeStamp": "2025-01-15T10:25:00Z"
  }
]
```

**Status Codes:**
- `200 OK` - Returns array of analyses (empty array if none exist)

### GET /summary

Returns a summary of all analyses including total count and breakdown by classification result.

**Response:**
```json
{
  "count": 150,
  "resultsCount": {
    "COMPLIES": 120,
    "DEVIATES": 25,
    "UNCLEAR": 5
  }
}
```

**Status Codes:**
- `200 OK` - Returns summary (count will be 0 if no analyses exist)

### Health Check Endpoints

- **Liveness**: `GET /health/live` - Returns 200 if the application is running
- **Readiness**: `GET /health/ready` - Returns 200 if the application is ready to serve requests
- **Metrics**: `GET /prometheus/metrics` - Prometheus metrics endpoint

## Testing

### Running Unit Tests

The project includes comprehensive unit tests covering mappers and operations.

**Run all tests:**
```bash
dotnet test
```

**Run tests with detailed output:**
```bash
dotnet test --verbosity normal
```

**Run tests with code coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Run specific test project:**
```bash
dotnet test Tests/Domain.UnitTests/Domain.UnitTests.csproj
```

### Test Coverage

- **AnalysisMapperTests** (15 tests)
  - MapToAnalysis validation and error handling
  - MapToAnalysisResponse transformations
  - Confidence rounding behavior
  - DateTime UTC conversion

- **AnalyzeOperationTests** (15 tests)
  - Input validation
  - Caching behavior
  - Classification and storage workflow
  - Error handling and logging
  - History retrieval
  - Summary aggregation

**Total: 30 unit tests**

### Manual API Testing

#### Using cURL

**Analyze an action:**
```bash
curl -X POST https://localhost:5001/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "action": "User accessed sensitive customer data",
    "guideline": "Access to sensitive data requires explicit authorization"
  }'
```

**Get history:**
```bash
curl https://localhost:5001/history
```

**Get summary:**
```bash
curl https://localhost:5001/summary
```

#### Using PowerShell

**Analyze an action:**
```powershell
$body = @{
    action = "User accessed sensitive customer data"
    guideline = "Access to sensitive data requires explicit authorization"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/analyze" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

**Get history:**
```powershell
Invoke-RestMethod -Uri "https://localhost:5001/history" -Method Get
```

**Get summary:**
```powershell
Invoke-RestMethod -Uri "https://localhost:5001/summary" -Method Get
```

#### Using Swagger UI

1. Navigate to `https://localhost:5001/swagger`
2. Expand the endpoint you want to test
3. Click "Try it out"
4. Enter the request body
5. Click "Execute"

## Database

### Overview

The application uses **SQLite** with **Entity Framework Core** for data persistence. The database file (`app.db`) is automatically created on first run.

**Default Location:** `Host/ProcessMonitorAPI/app.db`

### Database Schema

**Analysis Table:**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | INTEGER | Primary Key (auto-increment) |
| `Action` | TEXT | The action being analyzed |
| `Guideline` | TEXT | The guideline to evaluate against |
| `Result` | TEXT | Classification result (COMPLIES/DEVIATES/UNCLEAR) |
| `Confidence` | DECIMAL | Confidence score (0.00 - 1.00) |
| `CreatedAt` | DATETIME | Timestamp when analysis was created (UTC) |

### Database Management

#### View Database Contents

**Using SQLite CLI:**
```bash
# Install SQLite (if needed)
# Windows: Download from https://www.sqlite.org/download.html
# macOS: brew install sqlite
# Linux: sudo apt-get install sqlite3

# Open database
sqlite3 Host/ProcessMonitorAPI/app.db

# View all analyses
SELECT * FROM Analysis;

# View recent analyses
SELECT * FROM Analysis ORDER BY CreatedAt DESC LIMIT 10;

# Count by result
SELECT Result, COUNT(*) as Count FROM Analysis GROUP BY Result;

# Exit
.quit
```

#### Reset Database

To start fresh, simply delete the database file:

```bash
# Windows
del Host\ProcessMonitorAPI\app.db

# Linux/macOS
rm Host/ProcessMonitorAPI/app.db
```

The database will be recreated on next application startup.

#### Change Database Location

Update `ConnectionStrings:SqliteConnection` in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=C:\\path\\to\\your\\database.db"
  }
}
```

## Configuration

### appsettings.json Reference

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `HuggingFaceSettings:Model` | HuggingFace model to use | `facebook/bart-large-mnli` | No |
| `HuggingFaceSettings:TokenValue` | Your HuggingFace API token | - | Yes |
| `HuggingFaceSettings:Endpoint` | HuggingFace API endpoint | `https://router.huggingface.co/` | No |
| `ConnectionStrings:SqliteConnection` | SQLite connection string | `Data Source=app.db` | No |
| `Logging:LogLevel:Default` | Default log level | `Information` | No |
| `Logging:LogLevel:Microsoft.AspNetCore` | ASP.NET Core log level | `Warning` | No |

### Environment Variable Override

Override any configuration using environment variables with double underscores:

```bash
# Linux/macOS
export HuggingFaceSettings__TokenValue="your-token"
export ConnectionStrings__SqliteConnection="Data Source=/path/to/db.db"
export Logging__LogLevel__Default="Debug"

# Windows PowerShell
$env:HuggingFaceSettings__TokenValue="your-token"
$env:ConnectionStrings__SqliteConnection="Data Source=C:\path\to\db.db"
$env:Logging__LogLevel__Default="Debug"
```

### Logging Levels

Available log levels (from most to least verbose):
- `Trace` - Very detailed logs
- `Debug` - Debugging information
- `Information` - General informational messages (default)
- `Warning` - Warning messages
- `Error` - Error messages
- `Critical` - Critical failures

## Troubleshooting

### Issue: "Invalid request" error

**Symptoms:** API returns 400 Bad Request with "Invalid request" message

**Solutions:**
- Ensure both `action` and `guideline` fields are provided in the request body
- Verify fields are not empty or whitespace only
- Check Content-Type header is set to `application/json`

**Example:**
```json
{
  "action": "User performed an action",
  "guideline": "Guideline text here"
}
```

### Issue: API returns null or no classification response

**Symptoms:** 200 OK but null response or missing classification

**Solutions:**
- Verify your HuggingFace API token is valid
- Check token has not expired at [HuggingFace settings](https://huggingface.co/settings/tokens)
- Ensure network connectivity to `https://router.huggingface.co/`
- Check logs for detailed error messages

**Debug:**
```bash
# Check if token is set
dotnet user-secrets list

# View detailed logs
# Set log level to Debug in appsettings.json
```

### Issue: Database errors on startup

**Symptoms:** Application crashes or errors related to SQLite

**Solutions:**
- Ensure the application has write permissions in the directory
- Check if `app.db` file is not locked by another process
- Delete the database file to recreate it
- Verify SQLite is supported on your platform

**Windows Permission Fix:**
```powershell
# Run as administrator or ensure user has write permissions
icacls "Host\ProcessMonitorAPI" /grant Users:F
```

### Issue: Port already in use

**Symptoms:** "Address already in use" or port binding error

**Solutions:**

**Windows:**
```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill the process
taskkill /PID <process-id> /F
```

**Linux/macOS:**
```bash
# Find process using port 5001
lsof -i :5001

# Kill the process
kill -9 <process-id>
```

**Alternative:** Change the port in `Properties/launchSettings.json`

### Issue: HuggingFace API is slow or times out

**Symptoms:** Long response times or timeout errors

**Solutions:**
- HuggingFace free tier has rate limits and cold start delays
- Consider upgrading to paid tier for faster inference
- The model may need to "warm up" on first request (can take 30-60 seconds)
- Current timeout is set to 60 seconds (configurable in `Startup.cs`)

### Issue: Tests fail to run

**Symptoms:** `dotnet test` fails or tests don't execute

**Solutions:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Run tests with verbose output
dotnet test --verbosity detailed
```

### Issue: Swagger UI not loading

**Symptoms:** 404 or blank page at /swagger

**Solutions:**
- Ensure you're running in Development mode
- Check that the application has started successfully
- Navigate to the correct URL: `https://localhost:5001/swagger`
- Verify Swagger packages are installed

## Development

### Adding New Features

1. **Create feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Implement changes** in the appropriate layer:
   - Controllers ? `Host/ProcessMonitorAPI/Controllers/`
   - Business Logic ? `Domain/ProcessMonitorApi/Operations/`
   - Data Access ? `Domain/ProcessMonitorApi/Repository/`
   - External Services ? `Domain/ProcessMonitorApi/Services/`

3. **Add unit tests** in `Tests/Domain.UnitTests/`

4. **Build and test:**
   ```bash
   dotnet build
   dotnet test
   ```

5. **Commit and push:**
   ```bash
   git add .
   git commit -m "feat: description of your feature"
   git push origin feature/your-feature-name
   ```

### Code Style Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types (enabled in all projects)
- Use primary constructors where appropriate (.NET 10 feature)
- Add XML documentation comments for public APIs
- Keep methods small and focused
- Use dependency injection for all services

### Dependencies

**Host.ProcessMonitorApi:**
- Microsoft.AspNetCore.OpenApi (10.0.1)
- Microsoft.Extensions.Http.Resilience (10.1.0)
- prometheus-net.AspNetCore (8.2.1)
- Swashbuckle.AspNetCore.SwaggerGen (10.1.0)
- Swashbuckle.AspNetCore.SwaggerUI (10.1.0)

**ProcessMonitorApi:**
- HuggingFace.Net (1.0.0)
- Microsoft.EntityFrameworkCore (10.0.1)
- Microsoft.EntityFrameworkCore.Sqlite (10.0.1)
- Microsoft.Extensions.AI (10.1.1)
- Microsoft.Extensions.Configuration (10.0.1)

**Domain.UnitTests:**
- xunit (2.9.2)
- Moq (4.20.72)
- FluentAssertions (7.0.0)
- Microsoft.NET.Test.Sdk (17.12.0)
- coverlet.collector (6.0.2)

### Performance Considerations

- **Caching**: Duplicate action/guideline combinations are cached in the database
- **Resilience**: HTTP client includes automatic retries and circuit breakers
- **Timeout**: HuggingFace API calls have a 60-second timeout
- **Connection Pooling**: SQLite uses connection pooling via EF Core
- **Async/Await**: All I/O operations are asynchronous

### Security Recommendations

- ? Store API tokens in User Secrets or environment variables
- ? Use HTTPS in production
- ?? Implement authentication/authorization (not included in this version)
- ?? Consider rate limiting for the `/analyze` endpoint
- ?? Sanitize input data to prevent injection attacks
- ?? Use Azure Key Vault or similar for production secrets

## Architecture

### Design Patterns

- **Dependency Injection**: All services use constructor injection
- **Repository Pattern**: Data access abstracted through repository interfaces
- **Mapper Pattern**: Separate mapping logic from business logic
- **Operation Pattern**: Business operations encapsulated in dedicated classes

### Project Dependencies

```
Host.ProcessMonitorApi
  ??? ProcessMonitorApi (Domain)

Domain.UnitTests
  ??? ProcessMonitorApi (Domain)
```

### Clean Architecture Layers

1. **Presentation Layer** (`Host/ProcessMonitorAPI/`)
   - Controllers
   - Startup configuration
   - Middleware

2. **Domain Layer** (`Domain/ProcessMonitorApi/`)
   - Business logic (Operations)
   - Domain models
   - Interfaces
   - Data access

3. **Test Layer** (`Tests/Domain.UnitTests/`)
   - Unit tests
   - Test helpers

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'feat: add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Commit Message Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Adding or updating tests
- `refactor:` Code refactoring
- `perf:` Performance improvements
- `chore:` Build process or tooling changes

## License

[Specify your license here]

## Support

For issues, questions, or contributions:
- ?? [Open an issue](https://github.com/your-repo/issues)
- ?? [Start a discussion](https://github.com/your-repo/discussions)
- ?? Contact: your-email@example.com

## Acknowledgments

- [HuggingFace](https://huggingface.co/) for the BART-MNLI model
- [.NET Team](https://github.com/dotnet) for .NET 10
- All contributors to this project

## Changelog

### Version 1.0.0 (Current)
- ? Initial release
- ? AI-powered analysis using HuggingFace BART-MNLI model
- ? SQLite persistence with Entity Framework Core
- ? Smart caching for duplicate analyses
- ? History and summary endpoints
- ? Health checks and Prometheus metrics
- ? Swagger/OpenAPI documentation
- ? Comprehensive unit tests (30 tests)
- ? Resilience patterns (retry and circuit breaker)

---

**Built with ?? using .NET 10**
