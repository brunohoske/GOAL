import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app/app.dart';
import 'features/onboarding/presentation/onboarding_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final prefs = await SharedPreferences.getInstance();
  final onboardingDone = prefs.getBool(onboardingPrefKey) ?? false;

  runApp(ProviderScope(
    overrides: [
      onboardingDoneProvider.overrideWith((_) => onboardingDone),
    ],
    child: const GoalApp(),
  ));
}
