import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/auth_controller.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/blocking/presentation/blocking_screen.dart';
import '../../features/completion/presentation/complete_task_screen.dart';
import '../../features/goals/presentation/create_goal_screen.dart';
import '../../features/goals/presentation/goal_detail_screen.dart';
import '../../features/goals/presentation/home_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/onboarding/presentation/onboarding_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import '../../features/tasks/domain/task_models.dart';
import '../../features/tasks/presentation/create_task_screen.dart';
import 'app_shell.dart';

/// App routes with onboarding + auth guards. Watching the auth controller re-runs the
/// redirect on login/logout, so navigation reacts to auth state automatically.
final routerProvider = Provider<GoRouter>((ref) {
  final auth = ref.watch(authControllerProvider);
  final onboardingDone = ref.watch(onboardingDoneProvider);

  return GoRouter(
    initialLocation: '/home',
    redirect: (context, state) {
      // First launch: show the onboarding (includes the blocking disclosure).
      if (!onboardingDone) {
        return state.matchedLocation == '/onboarding' ? null : '/onboarding';
      }
      if (state.matchedLocation == '/onboarding') return '/home';

      final loggingIn = state.matchedLocation == '/login' || state.matchedLocation == '/register';

      switch (auth.status) {
        case AuthStatus.unknown:
          return null; // splash handled by app while bootstrapping
        case AuthStatus.unauthenticated:
          return loggingIn ? null : '/login';
        case AuthStatus.authenticated:
          return loggingIn ? '/home' : null;
      }
    },
    routes: [
      GoRoute(path: '/onboarding', builder: (_, __) => const OnboardingScreen()),
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
      GoRoute(path: '/goals/create', builder: (_, __) => const CreateGoalScreen()),
      GoRoute(
        path: '/goals/:goalId',
        builder: (_, s) => GoalDetailScreen(goalId: s.pathParameters['goalId']!),
        routes: [
          GoRoute(
            path: 'tasks/create',
            builder: (_, s) {
              final extra = (s.extra as Map<String, dynamic>?) ?? const {};
              return CreateTaskScreen(
                goalId: s.pathParameters['goalId']!,
                isAdmin: extra['isAdmin'] as bool? ?? true,
                review: extra['review'] as TaskDef?,
              );
            },
          ),
          GoRoute(
            path: 'complete/:assignmentId',
            builder: (_, s) {
              final extra = (s.extra as Map<String, dynamic>?) ?? const {};
              return CompleteTaskScreen(
                goalId: s.pathParameters['goalId']!,
                assignmentId: s.pathParameters['assignmentId']!,
                taskTitle: extra['taskTitle'] as String? ?? 'Tarefa',
                requiresImage: extra['requiresImage'] as bool? ?? false,
                requiresAttachment: extra['requiresAttachment'] as bool? ?? false,
                hasChecklist: extra['hasChecklist'] as bool? ?? false,
                checklistItems: ((extra['checklistItems'] as List?) ?? const [])
                    .map((e) => ChecklistItem.fromJson(Map<String, dynamic>.from(e as Map)))
                    .toList(),
                estimatedXp: extra['estimatedXp'] as int? ?? 0,
                sprintId: extra['sprintId'] as String? ?? '',
              );
            },
          ),
        ],
      ),

      // Shell with bottom navigation for the main tabs.
      ShellRoute(
        builder: (_, __, child) => AppShell(child: child),
        routes: [
          GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          GoRoute(path: '/blocking', builder: (_, __) => const BlockingHubScreen()),
          GoRoute(path: '/notifications', builder: (_, __) => const NotificationsScreen()),
          GoRoute(path: '/profile', builder: (_, __) => const ProfileScreen()),
        ],
      ),
    ],
  );
});
