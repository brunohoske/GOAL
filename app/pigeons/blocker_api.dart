import 'package:pigeon/pigeon.dart';

// Pigeon contract for the native app-blocking module.
// Run: dart run pigeon --input pigeons/blocker_api.dart
// (configuration below generates Dart + Kotlin glue).

@ConfigurePigeon(PigeonOptions(
  dartOut: 'lib/core/platform/blocker_api.g.dart',
  kotlinOut: 'android/app/src/main/kotlin/com/goal/app/blocker/BlockerApi.g.kt',
  kotlinOptions: KotlinOptions(package: 'com.goal.app.blocker'),
))

/// The blocking policy the app pushes down to the native side. Mirrors the backend
/// blocking-state for one goal: which apps to block and the current/target progress
/// (used only to render the overlay message).
class BlockPolicy {
  BlockPolicy({
    required this.enabled,
    required this.packages,
    required this.targetPct,
    required this.currentPct,
    required this.xpRemaining,
    required this.goalTitle,
  });

  bool enabled;
  List<String> packages;
  int targetPct;
  int currentPct;
  int xpRemaining;
  String goalTitle;
}

@HostApi()
abstract class AppBlockerHost {
  /// Apply (and persist) the blocking policy. Replaces any previous policy.
  void applyPolicy(BlockPolicy policy);

  /// Clear blocking entirely (member is unblocked).
  void clearPolicy();

  bool hasAccessibilityPermission();
  bool hasOverlayPermission();
  bool hasUsageAccessPermission();

  void openAccessibilitySettings();
  void openOverlaySettings();
  void openUsageAccessSettings();
}

@FlutterApi()
abstract class AppBlockerEvents {
  /// Fired when a blocked app was opened and the overlay was shown (telemetry).
  void onBlockedAppOpened(String packageName);

  /// Fired when the user changed a relevant system permission.
  void onPermissionChanged();
}
