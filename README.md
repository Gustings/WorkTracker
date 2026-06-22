# Work Tracker

Work Tracker is a premium, modern Windows desktop utility designed to track worked hours, calendar meetings, and calculate daily overtime/flextime balances. It features a sleek borderless UI styled after modern dashboard aesthetics.

## Key Features
- **Interactive Timeline**: Visual block-by-block breakdown of your day. Edit categories (Work, Meeting, Break, Offline Work) and descriptions, or delete entries directly from the hover edit menu.
- **Flextime & Overtime Calculator**: Automatically computes weekday overtime based on specific clock-time segments (e.g. 1.5x after 16:00, 2.0x after 21:00 and weekends).
- **Norwegian Public Holidays**: Programmatic Gregorian Easter-based calculations that automatically import public holidays on startup, adjusting weekly targets while preserving vacation days.
- **Calendar Integrations**: Imports meetings and time-off periods from Google Calendar (ICS feeds) with automatic duplicates/overlapping interval merging.
- **Diagnostics & System Logs**: Integrated event logging and simulation testing controls (e.g., simulating inactivity or screen unlock events).
- **Tray Summary Popup**: Quick-access pop-up window in the system tray displaying current daily worked hours, saving targets, and flex balances.
- **Automatic GitHub Updates**: Natively checks for newer setup versions from your GitHub repository, downloads them in the background, and silently installs updates.

## Technical Architecture
- **Language/Framework**: C# / .NET 9.0 (WPF)
- **Database**: SQLite (EF Core)
- **installer**: Inno Setup (with digital code-signing automated via PowerShell)

## Installation Guide

To install and run Work Tracker on your PC:
1. Navigate to the **[Releases](https://github.com/Gustings/WorkTracker/releases)** section of the GitHub repository.
2. Download the latest installer executable (`WorkTrackerSetup-1.5.0.exe`).
3. Run the downloaded installer and follow the setup wizard. You can select option preferences to:
   - Create a desktop shortcut.
   - Run Work Tracker automatically on Windows boot (recommended).
4. Click **Finish**. Work Tracker will launch immediately and minimize into your Windows system tray.

Once installed, the application will automatically check for updates on startup, notifying you if a newer release is available and offering to download and silently install it.

## Setup & Local Testing
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Build and run.

*Note: Code-signing certificate generation, clean builds, and setup packaging are fully automated via `build_and_sign.ps1` in the project root.*
