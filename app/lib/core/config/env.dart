/// Build-time configuration. Override with --dart-define=API_BASE_URL=...
abstract final class Env {
  /// Default targets localhost, which reaches the host machine through the
  /// `adb reverse tcp:5080 tcp:5080` tunnel — works on BOTH physical devices
  /// (USB) and emulators. Alternative for emulators without adb reverse:
  /// --dart-define=API_BASE_URL=http://10.0.2.2:5080/api/v1
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5080/api/v1',
  );
}
