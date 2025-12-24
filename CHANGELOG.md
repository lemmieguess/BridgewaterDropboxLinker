# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Complete attachment size checking in SendGuard
- Unit tests for SendGuard, LinkConversionTracker, and Configuration classes
- New test project: Bridgewater.DropboxLinker.Outlook.Tests
- GitHub Actions CI workflow for automated builds and code analysis
- Pending/in-progress conversion detection in send guard

### Changed
- SendGuard now properly inspects Outlook attachments for size warnings
- Improved error messages for multiple failed conversions

## [0.2.0] - 2024-12-24

### Added
- **OAuth 2.0 Authentication**
  - PKCE flow implementation (`DropboxAuthService`)
  - Secure token storage using Windows Credential Manager (`SecureTokenStorage`)
  - Automatic token refresh with refresh tokens
  - Browser-based authentication callback handling

- **Dropbox Integration**
  - Shared link creation via Dropbox API (`DropboxLinkService`)
  - 7-day expiration support
  - Team Space path root support
  - Automatic reuse of existing shared links

- **VSTO Add-in**
  - Complete add-in entry point (`ThisAddIn.cs`)
  - Ribbon button handler (`BridgewaterRibbon.cs`)
  - File picker with Dropbox root validation
  - HTML insertion at cursor position
  - Configuration management (`Configuration.cs`)

- **Send Guard Features**
  - Full attachment size checking implementation
  - Large attachment warnings (≥10 MB)
  - Send blocking for failed conversions
  - Failure recovery dialog with Retry/Re-authenticate/Remove options
  - Conversion state tracking per email (`LinkConversionTracker`)

- **Core Library**
  - Dropbox folder locator for Windows (`DropboxFolderLocator`)
  - Path mapper with validation (`DropboxPathMapper`)
  - HTML link block builder with email-safe styling (`LinkBlockBuilder`)
  - Plain text link block builder
  - File name cleaner for display formatting (`FileNameCleaner`)
  - Byte size formatter (`ByteSizeFormatter`)
  - Rolling file logger (`FileLogger`)

- **Testing**
  - Unit tests for all core utilities
  - Integration tests for secure token storage
  - Mock implementations for service testing

- **Documentation**
  - Developer handoff package
  - Product specification v0.2
  - UX specifications for link blocks
  - Best practices documentation

- **Infrastructure**
  - GitHub Actions CI workflow
  - EditorConfig for code style consistency

## [0.1.0] - 2024-12-20

### Added
- Initial concept and requirements gathering
- One-pager brief

[Unreleased]: https://github.com/lemmieguess/BridgewaterDropboxLinker/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/lemmieguess/BridgewaterDropboxLinker/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/lemmieguess/BridgewaterDropboxLinker/releases/tag/v0.1.0
