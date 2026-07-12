# Goal — Gamified Social-Accountability App

Friend groups create collective objectives ("Goals"), break them into documented tasks, approve
each other's work by vote, and **block social apps** until they hit their sprint XP target.
Unfinished XP becomes **debt** that accumulates and raises the bar next sprint.

## Architecture

```
Flutter app (Android-first)  ──REST──►  ASP.NET Core API (.NET 10)  ──►  PostgreSQL
        │                                       │
   Pigeon channel                          Hangfire jobs (close sprint, nag notifications)
        │                                       │
  Kotlin blocker                              FCM push
 (AccessibilityService + Overlay)
```

The **backend is the source of truth** for XP, debt and blocking state. The app only reflects
state and *enforces* blocking via the native module.

---

## Backend (`/src`, `/tests`)

- `.NET 10`, Clean Architecture: `Goal.Domain` (pure rules + 19 unit tests), `Goal.Application`
  (CQRS via MediatR), `Goal.Infrastructure` (EF Core + Postgres, JWT, FCM, Hangfire),
  `Goal.Api` (controllers, Swagger, Hangfire dashboard at `/hangfire`).

### Run it
```bash
docker compose up -d                 # Postgres on host port 5433 (5432 is taken by local PG)
dotnet run --project src/Goal.Api    # API on http://localhost:5080, applies migrations on boot
```
Swagger: http://localhost:5080/swagger

### Test
```bash
dotnet test tests/Goal.Domain.UnitTests   # gamification engine (blocking/XP/voting/debt)
```

The full loop is verified end-to-end (see `scratchpad/smoke2.ps1` in the session temp dir):
register → create goal → invite → assign → complete with docs → vote ≥60% → XP credited →
blocking recalculated.

---

## Flutter app (`/app`)

Feature-first, Riverpod, go_router, Dio (+ auth/refresh interceptor). Minimalist/elegant design
system in `lib/design_system` (warm white + indigo/teal, stylized **GOAL** wordmark).

### Setup
```bash
cd app
flutter pub get
dart run pigeon --input pigeons/blocker_api.dart   # generates the native blocking glue
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/api/v1
```
> The Android emulator reaches the host machine at `10.0.2.2`. On a physical device, use your
> machine's LAN IP.

### Screens
Auth (login/register) · Home (goal list) · Create-goal wizard (where all config is set) ·
Goal detail (blocking card + config) · Create task (admin) · Blocking hub · Blocking permissions.

---

## Native blocking module (`/app/android/.../blocker`)

- **`BlockerAccessibilityService`** — detects the foreground app (`TYPE_WINDOW_STATE_CHANGED`).
- **`BlockOverlay`** — draws a full-screen "you're blocked" overlay (`SYSTEM_ALERT_WINDOW`)
  and sends the user home.
- **`AppBlockerHostImpl`** — the Pigeon host: applies/clears policy, checks/opens the 3 special
  permissions (Accessibility, Overlay, Usage Access).
- **`BlockPolicyStore`** — persists policy across reboot; **`BootReceiver`** re-arms it.

The app pushes the backend's blocking-state into the native policy via `blockingSyncProvider`
(see `lib/features/blocking`). The native side enforces it independently of the backend.

> **Play Store compliance:** AccessibilityService + Overlay require a prominent disclosure and a
> digital-wellbeing justification. The accessibility config description and an explicit opt-in
> permissions screen are included for this reason.

### iOS (future)
The `AppBlocker` facade isolates platform code. An iOS port implements the same Pigeon contract
using FamilyControls / DeviceActivity (Screen Time). All UI/state/data is reused as-is.

---

## Environment notes
- Installed SDK: **.NET 10** (the solution targets `net10.0`).
- A local `postgresql-x64-18` service occupies port 5432, so Docker Postgres maps **5433**.
- Flutter SDK must be installed to build the app.
