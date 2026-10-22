# TLMS Mobile App and API

Branch: `integrate-tasks-chat-attendance`. Please test before it is merged to `main`.

## Projects

| Project | What it is |
|---|---|
| `TechnoLegendsManagementSystem` | MAUI app (Android and Windows) |
| `TechnoLegendsManagementSystem.Shared` | All pages, layouts, models and services |
| `TechnoLegendsMobile.Api` | Web API. The only thing that talks to the database |

Supabase handles sign-in. Everything else goes app → API → Supabase PostgreSQL. The API reads your identity from your login token and rejects anyone who hasn't passed two-factor.

## Features and how they work

**Sign-in with two-factor.** Email and password, then a 6-digit code from an authenticator app. First sign-in shows a QR code to scan. Without the code, the API refuses every request.

**Home and Profile.** Show your real name, title, department and role. Home lists your unread channels and your next tasks. Profile shows your task counts and most urgent task. Log Out clears your session so the next user never sees your data.

**Tasks.** The list has search, filters (My Team, High Priority, Assigned to me) and To Do / In Progress / Done tabs. Open a task to see its checklist, subtasks, people and discussion, and to post comments. **+ New** assigns a task to one or more people, with a project, priority, due date and story points. New tasks start in To Do.

**Chat.** Department channels and direct messages. History and sending go through the API. Live delivery uses SignalR, so new messages appear without refreshing. Opening a chat marks it read, which clears its unread badge. If the live connection drops, a banner shows and missed messages are fetched when it reconnects. Start a DM with the pencil button.

**Meetings and attendance.** Any member schedules a meeting by pasting a Teams link, a start time and a duration. Check-in unlocks 15 minutes before the start. Tapping **Check In** saves the time on the server and opens the link. Joining within 10 minutes of the start is **On time**, later is **Late**, and no check-in by the end is **Missed**. The Attendance screen shows your stats, and tapping a past meeting lists who attended.

**Notifications.** The bell on every screen shows an unread count, refreshed every 30 seconds and when you move between screens. A notification is created when someone assigns you a task, comments on a task you're on, or schedules a meeting. Tap one to mark it read and jump to it.

**Error handling.** Forms validate before sending and keep your input on error. Network failures, timeouts and server errors show plain-language messages, and a screen error shows a "Try again" card instead of crashing.

## Setup

1. **Clone to a short path** (long paths break the build):
```
   git clone [REPO URL] C:\dev\TLMS
   git checkout integrate-tasks-chat-attendance
```
2. **Install:** Visual Studio 2022 with the **.NET MAUI** workload, the **.NET 10 SDK**, and an Android emulator (or a phone). NuGet packages restore automatically when you open the solution.
3. **Database:** the shared Supabase project already has the tables. For a fresh project, run `database/schema.sql`, then `database/seed.sql`, in the Supabase SQL Editor.
4. **API secrets:** right-click `TechnoLegendsMobile.Api` → **Manage User Secrets** and paste the connection string you were sent privately. It is not in the repo.
5. **API address:** open `MauiProgram.cs` in the MAUI project and set `apiBase` for how you run the app:
   - Android emulator: `http://10.0.2.2:5080`
   - Windows: `http://localhost:5080`
   - Real phone: `http://<your-PC-IP>:5080`. The repo currently has **[your name]'s** IP, so **change it to yours**. Run `ipconfig` and use your IPv4 address. The phone must be on the same Wi-Fi, and Windows Firewall must allow port 5080.

## Create your own test user

Each tester needs their own account, because the two-factor code is tied to one phone.

1. In Supabase: **Authentication → Users → Add user → Create new user**. Enter an email and password, tick **Auto Confirm User**, and create it.
2. In the **SQL Editor**, run this with your own details:
```sql
   update profiles
   set full_name = 'Your Name', title = 'Developer', department = '#development'
   where id in (select id from auth.users where email = 'YOUR-EMAIL');
```
3. Sign in to the app. On your first sign-in, scan the QR code with Google Authenticator, Microsoft Authenticator or Authy, and enter the code.

To test chat, notifications and tasks you need **two** accounts signed in at once (for example Windows and the Android emulator).

## Run: MAUI app and API together

1. Right-click the **solution** → **Configure Startup Projects** → **Multiple startup projects**.
2. Set `TechnoLegendsMobile.Api` and `TechnoLegendsManagementSystem` (the MAUI app) both to **Start**. Move the **API above the app** so it starts first.
3. Press **F5**. Select the **http** profile for the API.

If only "Windows Machine" shows (Visual Studio hides the device list in this mode), start them separately:

1. Set `TechnoLegendsMobile.Api` as the startup project, pick the **http** profile and press **Ctrl+F5**. Leave its console window open.
2. Set the MAUI project as the startup project, pick your emulator or phone and press **F5**.

## Quick test (two accounts)

- [ ] Sign in with the code. Home and Profile show your name.
- [ ] Create a task assigned to the other account. Their bell shows 1.
- [ ] Send a chat message and a direct message. They arrive live.
- [ ] Schedule a meeting starting in about 10 minutes, then check in.
- [ ] Stop the API and tap around. A friendly error appears, not a crash.

Found a bug? Comment on the pull request with the screen and the error text.
