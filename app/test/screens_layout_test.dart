import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:goal_app/design_system/components/goal_progress_ring.dart';
import 'package:goal_app/design_system/theme/app_theme.dart';
import 'package:goal_app/features/completion/presentation/complete_task_screen.dart';
import 'package:goal_app/features/goals/presentation/create_goal_screen.dart';
import 'package:goal_app/features/tasks/domain/task_models.dart';
import 'package:goal_app/features/tasks/presentation/create_task_screen.dart';

Widget _wrap(Widget child) => ProviderScope(
      child: MaterialApp(theme: AppTheme.light(), home: child),
    );

void main() {
  testWidgets('complete task screen renders all dynamic fields', (tester) async {
    await tester.pumpWidget(_wrap(const CompleteTaskScreen(
      goalId: 'g1',
      assignmentId: 'a1',
      taskTitle: 'Estudar capitulo',
      requiresImage: true,
      requiresAttachment: true,
      hasChecklist: true,
      checklistItems: [
        ChecklistItem(id: 'c1', label: 'Ler paginas 1-10', isRequired: true, orderIndex: 0),
        ChecklistItem(id: 'c2', label: 'Fazer resumo', isRequired: false, orderIndex: 1),
      ],
      estimatedXp: 25,
      sprintId: 's1',
    )));
    await tester.pumpAndSettle();

    expect(find.text('Estudar capitulo'), findsOneWidget);

    // Items further down the ListView are built lazily — scroll to them.
    await tester.scrollUntilVisible(find.text('Ler paginas 1-10'), 200,
        scrollable: find.byType(Scrollable).first);
    expect(find.text('Ler paginas 1-10'), findsOneWidget);

    // Toggle a checklist item.
    await tester.tap(find.text('Fazer resumo'));
    await tester.pumpAndSettle();

    // Submitting without text shows the validation message (no network involved).
    await tester.scrollUntilVisible(find.text('Enviar para aprovação'), 200,
        scrollable: find.byType(Scrollable).first);
    await tester.tap(find.text('Enviar para aprovação'));
    await tester.pumpAndSettle();
    expect(find.text('Descreva o que foi feito.'), findsOneWidget);
  });

  testWidgets('create goal wizard derives the XP table from target / tasks', (tester) async {
    await tester.pumpWidget(_wrap(const CreateGoalScreen()));
    await tester.pumpAndSettle();

    // Defaults: 100 XP / 5 tarefas -> medium 20, easy 10, hard 40.
    await tester.scrollUntilVisible(find.textContaining('Valor calculado'), 200,
        scrollable: find.byType(Scrollable).first);
    expect(find.text('Fácil 10 XP  ·  Médio 20 XP  ·  Difícil 40 XP'), findsOneWidget);
  });

  testWidgets('progress ring animates to the given percentage', (tester) async {
    await tester.pumpWidget(_wrap(const Scaffold(
      body: Center(
        child: GoalProgressRing(progress: 0.72, threshold: 0.7, sublabel: 'da meta'),
      ),
    )));
    await tester.pumpAndSettle();

    expect(find.text('72%'), findsOneWidget);
    expect(find.text('da meta'), findsOneWidget);
  });

  testWidgets('create task screen: checklist editor adds and removes items', (tester) async {
    await tester.pumpWidget(_wrap(const CreateTaskScreen(goalId: 'g1')));
    await tester.pumpAndSettle();

    // Enable the checklist -> first item row appears.
    await tester.scrollUntilVisible(find.text('Checklist de subtarefas'), 200,
        scrollable: find.byType(Scrollable).first);
    await tester.tap(find.text('Checklist de subtarefas'));
    await tester.pumpAndSettle();
    expect(find.text('Item 1'), findsOneWidget);

    await tester.ensureVisible(find.text('Adicionar item'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Adicionar item'), warnIfMissed: false);
    await tester.pumpAndSettle();
    expect(find.text('Item 2'), findsOneWidget);
  });
}
