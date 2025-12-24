# Developer Handoff Package
Bridgewater Dropbox Linker for Outlook Classic
Version: v0.2
Date: 12/24/2025

## 1) Scope and deliverables
### Deliverable A: Outlook Classic add-in (VSTO)
- Outlook COM add-in (VSTO), .NET Framework 4.8
- Ribbon button: "Dropbox link" (required)
- Optional task pane drop zone (recommended, but can ship in v1 if time allows)
- Insert-at-cursor HTML block in compose window
- Send-time guardrails:
  - Warn on non-Dropbox attachments >= 10 MB
  - Block send if any link conversion is Failed

### Deliverable B: Dropbox integration
- OAuth 2.0 authorization code flow with PKCE
- Refresh token stored securely (Credential Manager or DPAPI)
- Local Dropbox folder discovery via info.json (Windows)
- Team space support via Path Root (root namespace id)
- Shared link creation with expiration (7 days)

### Deliverable C: MSI installer
- MSI packaging (WiX preferred)
- Manual update by installing newer MSI over old

### Deliverable D: Diagnostics
- Rolling logs
- Clear user-facing error prompts

## 2) User stories
### US-1 Insert Dropbox link block from Ribbon
As a user, when I click the "Dropbox link" button and pick a Dropbox file, I want a clean "Dropbox link" block inserted at my cursor, and I do not want the file attached.

Acceptance criteria:
- The block is inserted where the cursor is
- No attachment remains for that file
- Link opens the file
- Expiration defaults to 7 days

### US-2 Multiple file insert
As a user, when I pick multiple files, I want one block per file, inserted in a stable order.

Acceptance criteria:
- N files -> N blocks
- One blank line between blocks

### US-3 Send warning for large attachments
As a user, if I attempt to send an email with an attachment >= 10 MB, I want Outlook to warn me.

Acceptance criteria:
- Warning appears on send
- User can cancel send or send anyway

### US-4 Block send on conversion failure
As a user, if link creation failed, I want sending blocked until I fix it.

Acceptance criteria:
- Send is canceled
- The prompt offers Retry, Re-authenticate, Remove failed block, Cancel

## 3) Functional requirements (detailed)
### FR-1 Dropbox root discovery
- Read Dropbox desktop config info.json and get the business Dropbox folder path
- Support SmartSync without forcing download of the file

### FR-2 Local path validation
- Only allow files inside Dropbox root to be processed
- If file is outside Dropbox root, show a clear message and do not proceed

### FR-3 Dropbox path mapping
- Convert local path to Dropbox path (relative mapping)
- Normalize path separators and ensure Dropbox path begins with "/"

### FR-4 Team space Path Root
- Use Dropbox root namespace id and set the API Path Root appropriately

### FR-5 Create shared link
- Create shared link for the Dropbox path
- Configure access: anyone with the link
- Configure expiration: now + 7 days
- Reuse an existing link if desired, but v1 may create a new one for simplicity

### FR-6 Insert link block
- Insert a neutral "Dropbox link" HTML block at cursor
- Include: label, cleaned display name, file size, and a single primary hyperlink
- No remote images

### FR-7 Logging
- Log link creation attempts and outcomes
- Log authentication events (no secrets)
- Log send warnings and user decisions

## 4) UX copy and formatting
- Label text: Dropbox link
- Clickable text: Open
- Keep the block minimal and professional

## 5) Templates
Provide at least one template for the email body:
- Block label: Dropbox link
- File line: {DisplayName} ({Size})
- Action: Open

## 6) Edge cases
- Offline network or rate limiting: block send, allow retry
- SmartSync online-only file: link creation should still work using Dropbox path
- Plain text compose: convert to HTML or insert plain text fallback
- Cursor not available: insert at end of body and log the issue

## 7) Suggested milestones
### Milestone 0: Spike
- Confirm cursor insertion works in New, Reply, Forward
- Confirm Dropbox root discovery works on target machine
- Confirm shared link creation in Team Space works

### Milestone 1: MVP
- Ribbon flow
- OAuth and token storage
- Shared link creation with 7-day expiration
- HTML block insertion at cursor
- Logging
- Send warnings and send blocking for failures

### Milestone 2: Polish
- Task pane drop zone
- Multi-file selection and insertion improvements
- MSI packaging

## 8) Definition of done checklist
- Installs cleanly via MSI
- Works in Outlook Classic compose windows (New, Reply, Forward)
- Converts selected Dropbox files into link blocks at cursor
- Never attaches those files
- Warns on send for large non-Dropbox attachments
- Blocks send when conversion failures exist
- Produces usable logs
