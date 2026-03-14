# Payroll Management System

A web-based payroll management system built with ASP.NET Core Razor Pages (.NET 8). It enables employees to submit weekly timesheets and administrators to review, approve, and generate payroll reports.

## Features

- Employee registration, authentication, and profile management
- Weekly timesheet submission and editing
- Timesheet review and approval workflow for administrators
- Payroll generation for weeks with submitted timesheets
- Payroll run reports and history
- Role-based access (Admin/User)
- Entity Framework Core with SQL Server/SQLite support

## Technologies

- ASP.NET Core Razor Pages (.NET 8)
- Entity Framework Core
- Microsoft Identity for authentication
- Bootstrap for UI styling

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQLite

### Setup

1. Clone the repository:
```sh
git clone <your-repo-url>
   cd PayrollManagementSystem
```

2. Update `appsettings.json` with your database connection string.

3. Run database migrations:
```sh
dotnet ef database update
```

4. Start the application:
```sh
dotnet run
```

5. Open your browser and navigate to `https://localhost:5001` (or the port shown in the terminal).

### Default Roles

- **Admin**: Can review timesheets, generate payroll, and view reports.
- **User**: Can submit and edit their own timesheets.

## Project Structure

- `Pages/`: Razor Pages for user and admin workflows
- `Areas/Identity/`: Authentication and user management
- `Models/`: Entity models
- `Services/`: Business logic and data access
- `ViewModels/`: Data transfer objects for UI
- `Data/`: Database context

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes
4. Push to your branch and open a pull request

## License

This project is licensed under the MIT License.
