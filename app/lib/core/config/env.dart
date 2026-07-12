/// Build-time configuration. Override with --dart-define=API_BASE_URL=...
abstract final class Env {
  /// Defaults to the production backend so release builds work anywhere without
  /// extra flags. For local development against a backend on your machine, pass
  /// --dart-define=API_BASE_URL=http://localhost:5080/api/v1 (physical device or
  /// emulator via `adb reverse tcp:5080 tcp:5080`), or http://10.0.2.2:5080/api/v1
  /// for an emulator without adb reverse.
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://goal.cloud.hoskes.com/api/v1',
  );
}
