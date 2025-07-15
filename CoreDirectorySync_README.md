# CoreDirectorySync Implementation

## Overview
The CoreDirectorySync class has been successfully implemented and integrated into your ReleaseNotesUpdater application. Here's what it does:

## Key Features

### 1. **File Synchronization**
- Copies files from `OutputDir/` to `CoreDirectory/core/`
- Maintains the same directory structure
- Handles both full releases and SDK-only releases

### 2. **Backup System**
- Creates timestamped backup folders in `ApplicationRoot/backups/YYYY-MM-DD_HH-mm-ss/`
- Backs up existing files before replacing them
- Organized by context (root, release-notes, version folders)
- Handles duplicate backups with timestamp suffixes

### 3. **Intelligent Sync Logic**
- **New Runtime Releases**: Copies all files when runtime directory doesn't exist
- **SDK-Only Releases**: Only adds new files when runtime directory already exists
- **File Replacement**: Replaces existing files with backup creation

## File Mapping Examples

Based on your configuration with RuntimeIds ["9.0.7", "8.0.18"]:

```
OutputDir/README.md → CoreDirectory/core/README.md
OutputDir/releases.md → CoreDirectory/core/releases.md
OutputDir/release-notes/README.md → CoreDirectory/core/release-notes/README.md
OutputDir/release-notes/9.0/README.md → CoreDirectory/core/release-notes/9.0/README.md
OutputDir/release-notes/9.0/cve.md → CoreDirectory/core/release-notes/9.0/cve.md
OutputDir/release-notes/9.0/install-*.md → CoreDirectory/core/release-notes/9.0/install-*.md
OutputDir/release-notes/9.0/9.0.7/ → CoreDirectory/core/release-notes/9.0/9.0.7/
OutputDir/release-notes/8.0/8.0.18/ → CoreDirectory/core/release-notes/8.0/8.0.18/
```

## Backup Structure

Backups are now organized in the application root directory as follows:
```
C:\ReleaseNotesGen\backups\2025-07-15_14-30-45/
├── root/
│   ├── README.md
│   └── releases.md
├── release-notes/
│   └── README.md
├── release-notes/9.0/
│   ├── README.md
│   ├── cve.md
│   └── install-linux.md
└── release-notes/9.0/9.0.7/
    └── [runtime files]
```

## Integration

The CoreDirectorySync is now integrated into your Program.cs and will run automatically after all other file updaters complete. It:

1. Loads runtime IDs from your configuration
2. Creates backups of existing files in the application root directory
3. Syncs files from OutputDir to CoreDirectory/core
4. Logs all operations to your log file
5. Provides console output for monitoring

## Next Steps

1. **Test the Implementation**: Run your application to see the sync in action
2. **Verify Backup Creation**: Check that backups are created in `C:\ReleaseNotesGen\backups\`
3. **Test SDK-Only Scenario**: Create a scenario where a runtime directory already exists
4. **Monitor Logs**: Check the log file for detailed operation records

## Usage

The CoreDirectorySync will run automatically when you execute your application. No additional configuration is needed - it uses your existing appsettings.json configuration.

## Backup Location

**Important**: Backups are stored in your application's root directory (`C:\ReleaseNotesGen\backups\`) instead of the CoreDirectory. This keeps your backups separate from the target directory and makes them easier to manage.

## Error Handling

- Comprehensive error handling with detailed logging
- Backup failures won't stop the sync process
- Individual file failures are logged but don't stop the entire operation
- All exceptions are logged with timestamps
