# Flutter app — first-time setup

The `app/` folder contains all the hand-written source (`lib/`, the Pigeon contract, the native
Kotlin blocker, the AndroidManifest, and Android resources). It does **not** include the
generated Gradle wrapper / build files, because those are environment- and Flutter-version
specific. Generate them once with `flutter create`, which fills in the gaps without touching
your source.

## Steps

1. **Install Flutter** (stable channel) and run `flutter doctor` until Android tooling is green.

2. **Generate the Android/Gradle scaffolding into this folder** (it will NOT overwrite `lib/`):
   ```bash
   cd app
   flutter create . --org com.goal --platforms=android --project-name goal_app
   ```
   This creates `android/build.gradle`, `android/app/build.gradle`, `gradle/`, `settings.gradle`,
   `local.properties`, etc.

3. **Re-apply our custom Android bits if `flutter create` overwrote them.** Check these files —
   they are intentionally customized and must keep our versions:
   - `android/app/src/main/AndroidManifest.xml` (permissions, accessibility service, boot receiver, deep links)
   - `android/app/src/main/kotlin/com/goal/app/MainActivity.kt` (registers the Pigeon host)
   - `android/app/src/main/res/values/strings.xml`, `res/xml/accessibility_service_config.xml`
   - Make sure `android/app/build.gradle` has `minSdkVersion 24` (Pigeon/overlay APIs) and the
     `applicationId "com.goal.app"`.

4. **Get packages & generate Pigeon glue:**
   ```bash
   flutter pub get
   dart run pigeon --input pigeons/blocker_api.dart
   ```
   This generates `lib/core/platform/blocker_api.g.dart` and
   `android/app/src/main/kotlin/com/goal/app/blocker/BlockerApi.g.kt`.

5. **Run** (start the backend first — see root README). The app defaults to
   `http://localhost:5080/api/v1`, which reaches your PC through an adb tunnel that
   works on **both** physical devices (USB) and emulators:
   ```bash
   adb reverse tcp:5080 tcp:5080   # redo after unplugging/rebooting the device
   flutter run
   ```
   Emulator-only alternative (no adb reverse needed):
   ```bash
   flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/api/v1
   ```

## Notes
- `minSdkVersion` must be **24+**.
- The native blocker needs 3 special permissions granted at runtime via the in-app
  "Permissões de bloqueio" screen (Accessibility, Overlay, Usage Access).

## Enabling push notifications (Firebase / FCM)

Push is **optional** — without it the app still works and notifications appear in the in-app
"Alertas" tab. The FCM init is guarded: with no Firebase config it logs a warning and no-ops.

To enable real pushes:

1. Create a project at https://console.firebase.google.com and add an **Android app** with
   package name `com.goal.app`.
2. Download `google-services.json` into `android/app/`.
3. Wire the Google Services plugin:
   - `android/settings.gradle.kts` → inside `plugins { }` add
     `id("com.google.gms.google-services") version "4.4.2" apply false`
   - `android/app/build.gradle.kts` → inside `plugins { }` add
     `id("com.google.gms.google-services")`
4. **Backend:** in Firebase Console → Project settings → Service accounts, generate an
   **admin SDK private key** JSON. Save it (e.g. `src/Goal.Api/firebase-credentials.json`,
   never commit it) and point `appsettings.json` → `Firebase:CredentialsPath` at it.
5. Rebuild both. The app registers its device token at `/devices` after login; the backend
   pushes on review requests, approvals/rejections and blocking reminders.
