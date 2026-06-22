# Project Status: Work Tracker

- **Project Name**: Work Tracker
- **Current Version**: v1.4.3 (Latest Installer: `WorkTrackerSetup-1.4.3.exe`)
- **Current Goal**: Create a Windows application that tracks work hours, monitors active application usage to build a daily timeline, and calculates overtime/flexitime based on specific multipliers (1.5x for 16:00-21:00, 2.0x for 21:00-08:00 and weekends, with a standard 7.5-hour day).
- **Currently Working On**: Active maintenance, updates, and feature additions based on user feedback.
- **What Has Been Done**:
  - Initialized project tracking structure and finalized MVVM C#/.NET 9.0 WPF setup with local SQLite storage.
  - Implemented Win32 Active App Foreground and Idle monitoring services.
  - Developed full MVVM Views & ViewModels with premium dark UI styling, week/calendar views, and system tray integration.
  - Added public holidays calculation (Gregorian Easter-based) for Norway and vacation/sick day tracking.
  - Implemented ICS calendar synchronization with overlapping interval merging and meeting de-duplication.
  - Subscribed to Windows session lock/unlock events and created a customized PC locked idle-return prompt dialog.
  - Embedded a system tray summary popup window showing weekly and lifetime flextime statistics.
  - Refactored weekday overtime rules to sum clock-time based multipliers (1.5x after 16:00, 2x after 21:00) regardless of daily hours.
  - Added a modal timeline block edit dialog with a Pencil (`✏️`) hover icon and a delete option to erase work sessions.
  - Consolidated multiple popup views (Rules, Calendar, Holidays, Logs) into a single, cohesive borderless **Settings** window with sidebar navigation.
  - Expanded and compiled a custom uncompressed 12-resolution `.ico` container with correct alpha channels and transparency AND masks, resolving taskbar display bugs.
  - Created a C# `UpdateService` to fetch metadata from the GitHub Releases API, compare versions, and download installers.
  - Restructured settings navigation to add dedicated **Update** (with real-time progress logging) and **About App** (with credits and repository link) tabs.
  - Added auto-relaunch support (`Check: WizardSilent` in Inno Setup) to restart the application automatically after a silent update completes.
  - Configured and automated code-signing and setup compilation via `build_and_sign.ps1` using a local certificate.
  - Successfully published `v1.4.1`, `v1.4.2`, and `v1.4.3` releases and installers to the GitHub repository.

---

## ⏭ Next Step: Gather User Feedback

**Goal**: Monitor local testing of the auto-updater to ensure the application restarts seamlessly after updating, and gather feedback on the new Update and About App settings views.
