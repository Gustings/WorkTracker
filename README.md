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

## Setup & Local Testing
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Build and run.

*Note: Code-signing certificate generation, clean builds, and setup packaging are fully automated via `build_and_sign.ps1` in the project root.*
