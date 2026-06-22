# Project Status: Work Tracker

- **Project Name**: Work Tracker
- **Current Goal**: Create a Windows application that tracks work hours, monitors active application usage to build a daily timeline, and calculates overtime/flexitime based on specific multipliers (1.5x for 16:00-21:00, 2.0x for 21:00-08:00 and weekends, with a standard 7.5-hour day).
- **Currently Working On**: Active development — adding features based on user feedback.
- **What Has Been Done**:
  - Initialized project tracking structure.
  - Formulated baseline questions to refine design, technology, and overtime calculation rules.
  - Received user clarifications: Tech stack is C#/.NET WPF, Scenario A for overtime calculations (37.5 hours standard per week, all weekend hours count as overtime, time off days reduce target, active/idle tracking prompts on return, local SQLite storage, category rules for apps).
  - Authored and approved the implementation plan.
  - Implemented C#/.NET 9.0 WPF project, SQLite DB context, and entities.
  - Created Win32 Active App and Idle tracking services.
  - Implemented overtime & flexitime math (with daily/weekly target reduction and deficit offset prioritizations).
  - Developed full MVVM Views & ViewModels with premium dark UI styling and system tray integration.
  - Set up xUnit test project and successfully verified all overtime calculation rules (6/6 passed).
  - Created a walkthrough report.
  - Fixed minimum window size, exit crash, blank Week/Calendar views, 20-min rounding rule.
  - Added custom app icon (Okta-style gradient design).
  - Added Week View combined time-off/overtime popup with weekly savings box.
  - Added Calendar View unlinked time-off edit button (pencil icon).
  - Fixed Calendar View and Week View titles.
  - Removed daily timeline from Dashboard; restored single Top Applications card.
  - Fixed Calendar View edit button binding.
  - Replaced app icon with user-provided gradient calendar image.
  - Implemented Time Off flex balance deduction: vacation logs (TimeOffLog) now reduce the lifetime flex balance; sick days do not.
  - Calendar View cells now show: 🟢 overtime gained, 🟣 vacation used, 🔵 sick day badges.
  - Added Sick Day Hours field to the Week View manual popup (reduces work target, does NOT deduct flex).
  - Added "This Week" button to Dashboard and Week View date scrollers.
  - Replaced app icon with the gradient 3D calendar icon (calendar_date_icon_231425.webp → app_icon.png).
  - Attempted Outlook COM automation for calendar sync.
  - Implemented Outlook and ICS calendar synchronization via shareable URLs with localized Jet date filter fixes.
  - Implemented calendar deletion sync, all-day/multi-day regular meeting exclusions, and time-off event expansion.
  - Resolved event de-duplication on the daily timeline and daily calendar events schedule.
  - Aligned weekly view daily worked hours calculations (rounding, future cutoff, and overlap merging).
  - Implemented daily merging of overlapping work/meeting logs to prevent double-counting.
  - Restructured weekday overtime calculations to sum daily segments worked above 8.0 hours rather than capping by weekly totals.
  - Removed standard weekday deficit subtractions from the lifetime balance; only explicitly logged time-off now deducts from the flex balance.
  - Prioritized unlinked time-off consumption for spent flex hours and updated the unlinked hours counter in the UI.
  - Added a system tray left-click summary popup window showing current weekly and lifetime stats.
  - Removed all Outlook COM calendar integration and timers to prevent the application from prompting for Outlook 2016 setup/configuration on boot.
  - Programmatically generated `app_icon.ico` from PNG and embedded it into the executable properties (`<ApplicationIcon>`), resolving the missing window/taskbar/explorer icon issues.
  - Bumped release version to 1.0.2 and successfully built and signed the installer (archived at: [WorkTrackerSetup-1.0.2.exe](file:///c:/Users/augus/OneDrive/Dokumenter/AntiGravity/Work%20Tracker/Installers/Archive/WorkTrackerSetup-1.0.2.exe)).
  - Implemented Windows Session Lock/Unlock detection hooks (`Microsoft.Win32.SystemEvents.SessionSwitch`) to calculate PC lock durations. On unlock, if the duration exceeds the 5-minute idle threshold, it prompts the user with a customized PC Locked summary dialog rather than the standard inactive message.
  - Bumped release version to 1.1.0 and successfully built and signed the installer: [WorkTrackerSetup-1.1.0.exe](file:///c:/Users/augus/OneDrive/Dokumenter/AntiGravity/Work%20Tracker/Installers/WorkTrackerSetup-1.1.0.exe).

---

## ⏭ Next Step: Local User Verification & Feedback

**Goal**: Verify version 1.0.2 works seamlessly on the user's work PC (no Outlook setup wizard trigger, custom embedded icon loaded successfully) and gather feedback for potential future feature additions.
