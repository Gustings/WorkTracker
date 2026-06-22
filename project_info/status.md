# Project Status: Work Tracker

- **Project Name**: Work Tracker
- **Current Version**: v1.5.0 (Latest Installer: `WorkTrackerSetup-1.5.0.exe`)
- **Current Goal**: Add dashboard analytics and daily worked vs target stacked bar charts, Focus Mode with smart idle alert suppressions (for full-screen PowerPoint, media players, Teams, Zoom), custom day-by-day work schedules, and startup auto-update checks.
- **Currently Working On**: Final compile verification, packaging, and manual testing of v1.5.0.
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
  - Implemented custom day-by-day work schedules stored in SQLite AppSettings, dynamically driving overtime calculations and target reductions.
  - Extended idle transition detection to capture foreground monitor resolution and full-screen window states.
  - Created Focus Mode logic to auto-suppress idle prompt popups (logging idle duration as "Offline Work") and auto-log meetings or work during full-screen PowerPoint, VLC, Teams, Zoom, Chrome, or Edge sessions.
  - Added a native horizontal daily worked vs target stacked bar chart on the dashboard using a custom `DoubleToGridLengthConverter`.
  - Added automatic check for updates on startup with a prompt dialog allowing the user to initiate the silent installer background process.
  - Successfully published releases up to `v1.5.0` to the GitHub repository.

---

## ⏭ Next Step: Release and Verify Version 1.5.0

**Goal**: Complete local testing of the newly built installer `WorkTrackerSetup-1.5.0.exe`, verify the dashboard chart, Focus Mode, Custom Schedules settings page, and auto-update startup prompt, then publish to GitHub.
