import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/platform/app_blocker.dart';
import '../../goals/data/goals_repository.dart';

final appBlockerProvider = Provider<AppBlocker>((_) => AppBlocker.create());

/// Watches a goal's blocking state and pushes it to the native enforcer whenever it changes.
/// This is a side-effecting provider: keep it alive (e.g. watched by the goal detail screen)
/// so the policy stays in sync while the goal is on screen.
final blockingSyncProvider = Provider.family<void, ({String goalId, String goalTitle})>((ref, args) {
  final blocker = ref.read(appBlockerProvider);

  ref.listen(blockingStateProvider(args.goalId), (prev, next) {
    next.whenData((state) async {
      try {
        await blocker.sync(state, args.goalTitle);
      } catch (e) {
        debugPrint('blocking sync failed: $e');
      }
    });
  });
});
