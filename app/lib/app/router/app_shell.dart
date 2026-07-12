import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/notifications/data/notifications_repository.dart';

/// Bottom-nav shell for the main tabs.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.child});

  final Widget child;

  static const _paths = ['/home', '/blocking', '/notifications', '/profile'];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final location = GoRouterState.of(context).uri.path;
    var index = _paths.indexWhere((p) => p != '/home' && location.startsWith(p));
    if (index < 0) index = 0;

    final unread = ref.watch(unreadCountProvider).valueOrNull ?? 0;

    return Scaffold(
      body: child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (i) => context.go(_paths[i]),
        destinations: [
          const NavigationDestination(
              icon: Icon(Icons.flag_outlined), selectedIcon: Icon(Icons.flag), label: 'GOALs'),
          const NavigationDestination(
              icon: Icon(Icons.lock_outline), selectedIcon: Icon(Icons.lock), label: 'Bloqueio'),
          NavigationDestination(
            icon: Badge(
              isLabelVisible: unread > 0,
              label: Text('$unread'),
              child: const Icon(Icons.notifications_outlined),
            ),
            selectedIcon: Badge(
              isLabelVisible: unread > 0,
              label: Text('$unread'),
              child: const Icon(Icons.notifications),
            ),
            label: 'Alertas',
          ),
          const NavigationDestination(
              icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Perfil'),
        ],
      ),
    );
  }
}
