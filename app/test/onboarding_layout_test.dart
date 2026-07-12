import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:goal_app/design_system/theme/app_theme.dart';
import 'package:goal_app/features/onboarding/presentation/onboarding_screen.dart';

void main() {
  testWidgets('onboarding lays out and taps work', (tester) async {
    await tester.pumpWidget(ProviderScope(
      child: MaterialApp(
        theme: AppTheme.light(),
        home: const OnboardingScreen(),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('GOALs em grupo'), findsOneWidget);
    expect(find.text('Pular'), findsOneWidget);

    await tester.tap(find.text('Próximo'));
    await tester.pumpAndSettle();
    expect(find.text('Provas, não promessas'), findsOneWidget);
  });
}
