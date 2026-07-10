# GhostTouchUI

GhostTouchUI is a Windows desktop utility designed to help keep some games from logging the user out during inactivity by sending small synthetic mouse movements after a period of idle time. It is also my first experiment with a system tray application.

This project is free to use, modify, and include in other work under the MIT License.

## Repository Layout

```text
.
|-- GhostMouse.sln
|-- README.md
|-- .gitignore
`-- src/
    |-- GhostMouse/       # Console proof-of-concept
    `-- GhostTouchUi/     # WPF desktop application
```

## Requirements

- Windows
- .NET 8 SDK
- Visual Studio 2022 or another IDE with WPF/.NET desktop workload support

## Getting Started

Restore and build the solution from the repository root:

```powershell
dotnet restore .\GhostMouse.sln
dotnet build .\GhostMouse.sln
```

Run the WPF application:

```powershell
dotnet run --project .\src\GhostTouchUi\GhostTouchUi.csproj
```

Run the console proof-of-concept:

```powershell
dotnet run --project .\src\GhostMouse\GhostMouse.csproj
```

## Projects

- `GhostTouchUi`: Main WPF application. It monitors keyboard and mouse activity, shows a small activity log, and toggles ghosting from the UI/tray.
- `GhostMouse`: Minimal console app that demonstrates inactivity detection and synthetic mouse movement.

## Notes

This repository is prepared for source control, but it has not been initialized as a git repository. Local IDE state, build artifacts, and user-specific files are intentionally ignored.

## License

Licensed under the [MIT License](./LICENSE).
