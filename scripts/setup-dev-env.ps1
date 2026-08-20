param()

Write-Host "Configuring repository-local git hooks path to .githooks"
& git config core.hooksPath .githooks

if (Test-Path -Path .githooks/pre-commit) {
    Write-Host "Making .githooks/pre-commit executable (Windows)"
    # On Windows, Git for Windows will respect the executable bit in the index; ensure the script has CRLF preserved
    # There's no chmod on Windows reliably available; leave as-is.
} else {
    Write-Host "No .githooks/pre-commit found"
}

Write-Host "Done. Run 'git status' to verify and commit hooks if necessary."