# Best practice checklist (quick)

## Repo hygiene
- Use semantic versioning and a changelog
- Use consistent formatting (EditorConfig) and analyzers
- Enforce code review, even if lightweight

## Security
- Store OAuth tokens in Credential Manager or DPAPI
- Never log secrets
- Code-sign the MSI and add-in assemblies for trust

## Reliability
- No silent failures
- Always show clear recovery actions
- Fail safe: block send when a link conversion is failed

## Testing
- Unit test name cleaning, path mapping, and HTML builder
- Integration test Dropbox link creation in team space
- Manual test matrix: New, Reply, Forward, HTML and Plain Text

## Deployment
- MSI installer (WiX)
- Keep a simple internal release process
