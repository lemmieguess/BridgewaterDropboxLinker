# Bridgewater Dropbox Linker for Outlook Classic
Product spec (v0.2)
Date: 12/24/2025

## 1) Problem statement
Bridgewater frequently sends large client deliverables that are too large for email attachments. The current workflow is manual and error-prone: create a Dropbox link, paste it, format it, and ensure you did not accidentally attach the file.

## 2) Goals
### Goals (v1)
- Create a Dropbox shared link (anyone with the link, expires in 7 days) for selected Dropbox files
- Insert a clean, neutral "Dropbox link" block at the current cursor position
- Ensure the file is not attached (for files handled by the add-in flow)
- Provide send-time guardrails:
  - Warn if there is a non-Dropbox attachment >= 10 MB
  - Block send if any Dropbox-link insertion is in a failed state

### Non-goals (v1)
- Word or Excel integration
- Dropbox Transfer links
- Uploading non-Dropbox files into Dropbox automatically
- Passwords, disable download, or view-only controls (future version)

## 3) Target environment
- Outlook Classic, Microsoft 365 Apps, 64-bit
- Windows 11
- Dropbox Enterprise, Team Space
- SmartSync

## 4) UX requirements
### Entry points
- Ribbon button (required): Dropbox link
- Task pane drop zone (recommended): Drop files here

### Insert location
- Insert the link block at the current cursor position in the email body

### Multiple files
- Insert one block per file, separated by a blank line

### Block style
- Neutral and clean
- Label text: Dropbox link
- Inline CSS only
- Table-based HTML layout (email safe)
- No remote images by default

## 5) Sharing requirements
- Audience: anyone with the link
- Expiration: 7 days
- Password: off

## 6) Failure handling
If Dropbox API fails:
- Mark the item as Failed
- Block Send with a modal prompt
- Provide recovery options:
  - Retry
  - Re-authenticate
  - Remove the failed block and continue editing (send remains blocked until resolved)
  - Cancel

## 7) Logging
- Local rolling logs under %LOCALAPPDATA%\Bridgewater\DropboxLinker\logs\
- Log API outcomes and error codes
- Avoid storing full local file paths unless debug mode is enabled

## 8) Outlook constraint and design choice
Outlook attachments typically do not preserve a reliable source file path through the object model. Therefore v1 should not depend on converting normal attachments into Dropbox links by inferring attachment paths. Instead, v1 uses a deterministic "Dropbox link" flow (Ribbon and optional drop zone).

## 9) Acceptance criteria
- Selecting a Dropbox file inserts a "Dropbox link" block at cursor and does not attach the file
- Link works for recipients without Dropbox login, assuming Dropbox policy allows public links
- Links default to expire in 7 days
- Multiple selected files insert multiple blocks, one per file
- If Dropbox auth fails, send is blocked until resolved
- If user attempts to send an email with a non-Dropbox attachment >= 10 MB, they get a warning
