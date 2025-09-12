# CMPCodeDatabase

WinForms-based code database manager for patch workflows.

## Features
- Startup checks create `Files/`, `Files/Database/`, `Files/Tools/` if missing and guide setup.
- **ReloadDB** button clears UI and reloads all databases.
- Quick links: [Download Database](https://drive.google.com/drive/folders/1MoOYhItCwsTypEkn8a98TY3O32t8WnIe) and **Download Tools** [Apollo CLI](https://github.com/bucanero/apollo-lib/releases).
- Code preview highlighting synced with current dialog.
- Built in Apollo Patching Using Apollo CLI (Like ApolloGUI)

## Build
- .NET 8 (Windows Desktop)
- Open the solution in Visual Studio or run `dotnet build`.

## First run
1. Launch app. If required folders are missing, the setup dialog appears.
2. Click **Open Database Page** and **Open Tools Page** to download zips.
3. Unzip contents into:
   - Database → `Files/Database`
   - Tools → `Files/Tools`
4. Click **ReloadDB** to load the database.

## Settings
Settings are stored at: `%AppData%/CMPCodeDatabase/settings.json`

Key fields:
- `DatabaseDownloadUrl` (default: your Drive folder)
- `ToolsDownloadUrl` (default: Apollo CLI tools releases)

## GitHub Desktop (quick start)
1. **File → New repository** in GitHub Desktop, choose this folder.
2. Review added files, commit: `chore: initial import`.
3. **Publish repository** to GitHub.
4. (Optional) Add a release with packaged binaries.

## License
GPL-3.0. See `LICENSE`.
