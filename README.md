# Bridgewater Dropbox Linker for Outlook Classic

[![CI](https://github.com/lemmieguess/BridgewaterDropboxLinker/actions/workflows/ci.yml/badge.svg)](https://github.com/lemmieguess/BridgewaterDropboxLinker/actions/workflows/ci.yml)

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
│   │   ├── Auth/                            # OAuth authentication & token storage
│   │   │   ├── DropboxAuthService.cs        # OAuth 2.0 PKCE flow
│   │   │   └── SecureTokenStorage.cs        # Windows Credential Manager integration
│   │   ├── Contracts/                       # Interfaces and DTOs
│   │   ├── Dropbox/                         # Dropbox integration
│   │   │   ├── DropboxFolderLocator.cs      # Finds Dropbox folder from info.json
│   │   │   ├── DropboxLinkService.cs        # Creates shared links via API
│   │   │   └── DropboxPathMapper.cs         # Maps local paths to Dropbox paths
│   │   ├── Html/                            # Email-safe HTML generation
│   │   │   └── LinkBlockBuilder.cs          # Builds table-based link blocks
│   │   ├── Logging/                         # Rolling file logger
│   │   └── Utilities/                       # Helper utilities
│   │       ├── ByteSizeFormatter.cs         # Human-readable file sizes
│   │       └── FileNameCleaner.cs           # Clean display names
│   │
│   └── Bridgewater.DropboxLinker.Outlook/  # Outlook VSTO add-in
│       ├── ThisAddIn.cs                     # VSTO entry point
│       ├── BridgewaterRibbon.cs             # Ribbon UI and file picker
│       ├── Configuration.cs                 # Settings management
│       ├── Ribbon/
│       │   └── BridgewaterRibbon.xml        # Ribbon XML customization
│       └── Services/
│           ├── SendGuard.cs                 # Send-time validation
│           ├── SendBlockedDialog.cs         # Failure recovery UI
│           └── LinkConversionTracker.cs     # Tracks conversion state per email
│
├── tests/
│   └── Bridgewater.DropboxLinker.Core.Tests/
│       ├── ByteSizeFormatterTests.cs
│       ├── DropboxLinkServiceTests.cs
│       ├── DropboxPathMapperTests.cs
│       ├── FileNameCleanerTests.cs
│       ├── LinkBlockBuilderTests.cs
│       └── SecureTokenStorageTests.cs
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
git clone https://github.com/lemmieguess/BridgewaterDropboxLinker.git
cd BridgewaterDropboxLinker
```

### 2. Set Up the VSTO Project

The `Bridgewater.DropboxLinker.Outlook` project contains complete production code. To enable VSTO deployment:

1. In Visual Studio 2022, create a new project: **Outlook VSTO Add-in (C#, .NET Framework)**
2. Name it `Bridgewater.DropboxLinker.Outlook.VSTO` (or replace the existing project)
3. Target .NET Framework 4.8
4. Add a project reference to `Bridgewater.DropboxLinker.Core`
5. Copy the code files from `src/Bridgewater.DropboxLinker.Outlook/`:
   - `ThisAddIn.cs`
   - `BridgewaterRibbon.cs`
   - `Configuration.cs`
   - `Ribbon/BridgewaterRibbon.xml`
   - `Services/SendGuard.cs`
   - `Services/SendBlockedDialog.cs`
   - `Services/LinkConversionTracker.cs`
6. The VSTO template will automatically generate deployment manifests

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

### 1. Create Dropbox App

1. Go to [Dropbox App Console](https://www.dropbox.com/developers/apps)
2. Click **Create app**
3. Choose **Scoped access** and **Full Dropbox**
4. Name your app (e.g., "Bridgewater Dropbox Linker")
5. Under **Permissions**, enable:
   - `files.content.read`
   - `sharing.write`
6. Copy the **App key** (you'll need this)

### 2. Configure the Add-in

Create a settings file at `%LOCALAPPDATA%\Bridgewater\DropboxLinker\settings.json`:

```json
{
  "DropboxAppKey": "YOUR_APP_KEY_HERE",
  "RootNamespaceId": null,
  "LargeAttachmentThresholdBytes": 10485760,
  "LinkExpirationDays": 7,
  "DebugLogging": false
}
```

**Note:** For Team Space accounts, you may need to set `RootNamespaceId` to your team's root namespace ID. Contact your Dropbox admin for this value.

### 3. Environment Variables (Development)

For development, you can use environment variables instead:

```bash
set DROPBOX_APP_KEY=your_app_key
set DROPBOX_ROOT_NAMESPACE_ID=your_namespace_id  # Optional
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

### v0.2 (Current - Implemented)
- ✅ Core utilities and contracts
- ✅ Unit test coverage (Core and Outlook services)
- ✅ OAuth 2.0 + PKCE authentication service
- ✅ Secure token storage (Windows Credential Manager)
- ✅ Dropbox folder locator
- ✅ Dropbox path mapper
- ✅ Shared link creation service
- ✅ HTML block builder (email-safe)
- ✅ VSTO add-in scaffolding (ThisAddIn, Ribbon)
- ✅ Send guard with full attachment size checking
- ✅ Failure recovery dialog
- ✅ Link conversion tracking
- ✅ GitHub Actions CI pipeline

### v1.0 (Ready for Integration Testing)
- ⏳ Create VSTO project in Visual Studio (use the code from this repo)
- ⏳ Register Dropbox app and obtain App Key
- ⏳ Integration testing with Team Space
- ⏳ MSI installer with WiX
- ⏳ Code signing

### v1.1
- Task pane drop zone
- Link reuse optimization
- Telemetry (opt-in)

## License

Proprietary - Bridgewater Studio

## Support

For issues or questions, contact the development team.
