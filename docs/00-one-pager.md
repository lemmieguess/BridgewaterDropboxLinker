# Bridgewater Dropbox Linker for Outlook Classic
One page brief (v0.2)
Date: 12/24/2025

## What we are building
A Windows Outlook Classic add-in that turns Dropbox files into clean "Dropbox link" blocks inserted at the current cursor position in the email body, without attaching the file.

## Why (measurable)
- Saves about 2 minutes per email
- Prevents forgetting to paste the link
- Standardizes the client experience with a professional, consistent link block

## Hard requirements
- Outlook Classic only (Microsoft 365 Apps, 64-bit), Windows 11
- Dropbox Enterprise, Team Space, SmartSync
- Trigger immediately when the user adds a Dropbox file through the add-in flow
- Remove attachments always for files handled by the add-in flow
- Only trigger for files inside the local Dropbox folder
- Insert exactly at cursor
- Block text label: Dropbox link
- Multiple files: insert one block per file
- Shared link access: anyone with the link
- Expiration: 7 days
- No password
- If Dropbox API fails: block send and offer recovery options
- Send guard: warn for any non-Dropbox attachment >= 10 MB

## Primary UX
- Ribbon button: "Dropbox link" -> select one or more Dropbox files -> insert blocks at cursor
- Optional: Task pane drop zone -> drop files -> insert blocks at cursor

## Architecture
- C# VSTO COM add-in for Outlook Classic (.NET Framework 4.8)
- Dropbox API via Dropbox .NET SDK
- OAuth 2.0 with PKCE, refresh token stored securely (Windows Credential Manager or DPAPI)
- Local rolling logs for diagnostics

## Definition of done (v1)
- Selecting a Dropbox file inserts a "Dropbox link" block at cursor and does not attach the file
- Links default to expire in 7 days
- Multiple selected files insert multiple blocks, one per file
- If Dropbox auth or API fails, send is blocked until resolved
- Large non-Dropbox attachments (>= 10 MB) trigger a warning on send
