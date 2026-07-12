import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/network/dio_client.dart';
import '../core/notifications/push_service.dart';
import '../design_system/theme/app_theme.dart';
import '../features/auth/presentation/auth_controller.dart';
import 'router/app_router.dart';

class GoalApp extends ConsumerWidget {
  const GoalApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // When a token refresh fails, sign the user out so the router redirects to login.
    ref.listen<bool>(authSignedOutProvider, (_, signedOut) {
      if (signedOut) {
        ref.read(authControllerProvider.notifier).markSignedOut();
        ref.read(authSignedOutProvider.notifier).state = false;
      }
    });

    // Once authenticated, register this device for push (no-op if Firebase isn't set up).
    ref.listen<AuthState>(authControllerProvider, (prev, next) {
      if (next.status == AuthStatus.authenticated && prev?.status != AuthStatus.authenticated) {
        ref.read(pushServiceProvider).registerDevice();
      }
    });

    final router = ref.watch(routerProvider);
    return MaterialApp.router(
      title: 'Goal',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: router,
      supportedLocales: const [Locale('pt', 'BR'), Locale('en')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
    );
  }
}
