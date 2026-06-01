# Signing SlideShowScreenSaver.exe

## Prerequisites (once per session)

1. Launch **SimplySign Desktop** from the Start menu
2. Log in with **saul@ucalgary.ca** — your phone (SimplySign mobile app) will be needed for the TOTP code

## Steps

1. Build in **Release** configuration
2. Open a **PowerShell** prompt at the solution root (not Git Bash — it breaks the signing flags)
3. Run:
   ```powershell
   .\Sign.ps1
   ```
4. Approve the signing request on your phone when prompted
5. Rename `SlideShowScreenSaver.exe` → `SlideShowScreenSaver.scr` — the signature stays valid after rename

## If Sign.ps1 fails with "No certificates were found"

SimplySign Desktop is not running or not logged in. Start it and log in first.

If `SimplySignDesktop.exe` is missing, reinstall from:
`D:\@Timelapse\CodeSigning(Certum)\SimplySignDesktop-9.4.2.86-win-64-bit.exe`

## Reference

Full signing guide: `D:\@Timelapse\CodeSigning(Certum)\SIGNING-GUIDE.md`
