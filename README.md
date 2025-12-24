# Bridgewater Dropbox Linker for Outlook Classic

An Outlook Classic add-in that converts Dropbox files into clean, professional link blocks inserted at the cursor position.

## Overview

This add-in streamlines the process of sharing Dropbox files via email by:

- Converting Dropbox files to shareable links with a single click
- Inserting clean, branded "Dropbox link" blocks at the cursor
- Automatically setting 7-day link expiration
- Warning on large attachments (≥10 MB)
- Blocking send when link creation fails

## Project Structure

```
BridgewaterDropboxLinker/
├── src/
│   ├── Bridgewater.DropboxLinker.Core/     # Core business logic (platform-agnostic)
│   │   ├── Contracts/                       # Interfaces and DTOs
│   │   ├── Dropbox/                         # Dropbox folder detection and path mapping
│   │   ├── Html/                            # HTML block generation
│   │   ├── Logging/                         # File-based logging
│   │   └── Utilities/                       # Helper utilities
│   │
│   └── Bridgewater.DropboxLinker.Outlook/  # Outlook VSTO add-in
│       ├── Ribbon/                          # Ribbon XML customization
│       └── Services/                        # Outlook-specific services
│
├── tests/
│   └── Bridgewater.DropboxLinker.Core.Tests/
│
├── docs/                                    # Product documentation
├── settings/                                # Configuration templates
└── .github/workflows/                       # CI/CD pipelines
```

## Prerequisites

- **Development Machine**
  - Windows 10/11
  - Visual Studio 2022 with:
    - .NET desktop development workload
    - Office/SharePoint development workload
  - .NET Framework 4.8 SDK

- **Target Environment**
  - Windows 11
  - Microsoft 365 Apps (Outlook Classic, 64-bit)
  - Dropbox for Windows (Enterprise, Team Space, SmartSync)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/BridgewaterDropboxLinker.git
cd BridgewaterDropboxLinker
```

### 2. Create the VSTO Project

The `Bridgewater.DropboxLinker.Outlook` project is a placeholder. To create a production VSTO add-in:

1. In Visual Studio, create a new project: **Outlook VSTO Add-in (C#, .NET Framework)**
2. Name it `Bridgewater.DropboxLinker.Outlook`
3. Target .NET Framework 4.8
4. Add a project reference to `Bridgewater.DropboxLinker.Core`
5. Copy the code from the placeholder project

### 3. Configure Dropbox API

1. Create a Dropbox app at [https://www.dropbox.com/developers/apps](https://www.dropbox.com/developers/apps)
2. Configure OAuth 2.0 with PKCE
3. Request scopes: `files.content.read`, `sharing.write`
4. Store credentials securely (Windows Credential Manager recommended)

### 4. Build and Test

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Configuration

Copy `settings/settings.example.json` to `settings/settings.json` and customize:

```json
{
  "largeAttachmentThresholdBytes": 10485760,
  "defaultExpirationDays": 7,
  "blockLabel": "Dropbox link",
  "primaryActionText": "Open",
  "insertBlankLineBetweenBlocks": true,
  "enableTaskPaneDropZone": true,
  "debugLogPaths": false
}
```

## Key Features

### Ribbon Button

Click **"Dropbox link"** in the compose window to:
1. Select one or more Dropbox files
2. Create shared links with 7-day expiration
3. Insert formatted blocks at the cursor

### Send Guard

Prevents accidental sends by:
- Warning when attachments exceed 10 MB
- Blocking send when link creation has failed

### Logging

Rolling log files in `%LOCALAPPDATA%\Bridgewater\DropboxLinker\Logs\`:
- `dropboxlinker-YYYYMMDD.log`

## Development Guidelines

### Code Style

- Follow the `.editorconfig` rules
- Use nullable reference types
- Document public APIs with XML comments
- Keep Core library platform-agnostic

### Testing

- Unit test all Core utilities
- Integration test Dropbox API in Team Space
- Manual test matrix: New, Reply, Forward (HTML and Plain Text)

### Security

- Store OAuth tokens in Windows Credential Manager
- Never log secrets or tokens
- Code-sign MSI and assemblies

## Deployment

### MSI Packaging (WiX)

1. Install WiX Toolset v4+
2. Build the installer project
3. Code-sign the MSI

### Installation

```bash
msiexec /i BridgewaterDropboxLinker.msi /qn
```

## Documentation

| Document | Description |
|----------|-------------|
| [00-one-pager.md](docs/00-one-pager.md) | Executive summary |
| [01-product-spec-v0.2.md](docs/01-product-spec-v0.2.md) | Product requirements |
| [02-developer-handoff-v0.2.md](docs/02-developer-handoff-v0.2.md) | Technical specification |
| [03-ux-html-block.md](docs/03-ux-html-block.md) | UX design for link blocks |
| [04-templates.md](docs/04-templates.md) | Email templates |
| [05-best-practices.md](docs/05-best-practices.md) | Development best practices |

## Roadmap

### v0.2 (Current)
- ✅ Core utilities and contracts
- ✅ Unit test coverage
- ⏳ VSTO add-in implementation

### v1.0
- Ribbon flow with file picker
- OAuth 2.0 + PKCE authentication
- Shared link creation with expiration
- Send guard implementation
- MSI installer

### v1.1
- Task pane drop zone
- Link reuse optimization
- Telemetry (opt-in)

## License

Proprietary - Bridgewater Studio

## Support

For issues or questions, contact the development team.
