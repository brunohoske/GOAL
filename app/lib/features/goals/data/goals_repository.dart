import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';
import '../../blocking/domain/blocking_state.dart';
import '../../tasks/domain/task_models.dart';
import '../domain/goal_models.dart';

class GoalsRepository {
  GoalsRepository(this._dio);
  final Dio _dio;

  Future<List<GoalSummary>> listGoals() async {
    final r = await _dio.get('/goals');
    return (r.data as List).map((e) => GoalSummary.fromJson(e)).toList();
  }

  Future<GoalDetail> getGoal(String goalId) async {
    final r = await _dio.get('/goals/$goalId');
    return GoalDetail.fromJson(r.data);
  }

  Future<String> createGoal(Map<String, dynamic> body) async {
    final r = await _dio.post('/goals', data: body);
    return r.data as String;
  }

  Future<BlockingState> getBlockingState(String goalId) async {
    final r = await _dio.get('/goals/$goalId/blocking-state');
    return BlockingState.fromJson(r.data);
  }

  Future<String> createTask(String goalId, Map<String, dynamic> body) async {
    final r = await _dio.post('/goals/$goalId/tasks', data: body);
    return r.data as String;
  }

  Future<String> createInvite(String goalId, String email) async {
    final r = await _dio.post('/goals/$goalId/invites', data: {'email': email});
    return r.data as String;
  }

  /// Joins a goal by its shareable code; returns the goal id.
  Future<String> joinByCode(String code) async {
    final r = await _dio.post('/goals/join', data: {'code': code.trim().toUpperCase()});
    return r.data as String;
  }

  /// Admin-only: edits the mutable fields (title, sprint duration, XP target/table).
  Future<void> updateGoal(String goalId, Map<String, dynamic> body) =>
      _dio.patch('/goals/$goalId', data: body);

  /// Admin-only: archives the goal (members are released automatically).
  Future<void> deleteGoal(String goalId) => _dio.delete('/goals/$goalId');

  Future<String> assignTask(String sprintId, String taskDefinitionId, {String? targetMemberId}) async {
    final r = await _dio.post('/sprints/$sprintId/assignments', data: {
      'taskDefinitionId': taskDefinitionId,
      'targetMemberId': targetMemberId,
      'dueAt': null,
    });
    return r.data as String;
  }

  Future<String> submitCompletion(String assignmentId, String text,
      {List<Map<String, dynamic>> attachments = const [], List<Map<String, dynamic>> checklist = const []}) async {
    final r = await _dio.post('/assignments/$assignmentId/completions',
        data: {'textContent': text, 'attachments': attachments, 'checklist': checklist});
    return r.data as String;
  }

  Future<Map<String, dynamic>> vote(String completionId, int decision, {String? comment}) async {
    final r = await _dio.post('/completions/$completionId/votes', data: {'decision': decision, 'comment': comment});
    return r.data as Map<String, dynamic>;
  }

  // --- Lists used by the goal-detail tabs ---

  Future<List<TaskDef>> listTasks(String goalId) async {
    final r = await _dio.get('/goals/$goalId/tasks');
    return (r.data as List).map((e) => TaskDef.fromJson(e)).toList();
  }

  Future<List<Assignment>> listAssignments(String sprintId) async {
    final r = await _dio.get('/sprints/$sprintId/assignments');
    return (r.data as List).map((e) => Assignment.fromJson(e)).toList();
  }

  Future<List<Member>> listMembers(String goalId) async {
    final r = await _dio.get('/goals/$goalId/members');
    return (r.data as List).map((e) => Member.fromJson(e)).toList();
  }

  Future<List<ReviewItem>> listReviewQueue(String goalId) async {
    final r = await _dio.get('/goals/$goalId/review-queue');
    return (r.data as List).map((e) => ReviewItem.fromJson(e)).toList();
  }
}

final goalsRepositoryProvider = Provider<GoalsRepository>(
  (ref) => GoalsRepository(ref.read(dioProvider)),
);

/// List of the user's goals (auto-refreshable via ref.invalidate).
final goalsListProvider = FutureProvider<List<GoalSummary>>(
  (ref) => ref.read(goalsRepositoryProvider).listGoals(),
);

final goalDetailProvider = FutureProvider.family<GoalDetail, String>(
  (ref, goalId) => ref.read(goalsRepositoryProvider).getGoal(goalId),
);

final blockingStateProvider = FutureProvider.family<BlockingState, String>(
  (ref, goalId) => ref.read(goalsRepositoryProvider).getBlockingState(goalId),
);

final tasksProvider = FutureProvider.family<List<TaskDef>, String>(
  (ref, goalId) => ref.read(goalsRepositoryProvider).listTasks(goalId),
);

final assignmentsProvider = FutureProvider.family<List<Assignment>, String>(
  (ref, sprintId) => ref.read(goalsRepositoryProvider).listAssignments(sprintId),
);

final membersProvider = FutureProvider.family<List<Member>, String>(
  (ref, goalId) => ref.read(goalsRepositoryProvider).listMembers(goalId),
);

final reviewQueueProvider = FutureProvider.family<List<ReviewItem>, String>(
  (ref, goalId) => ref.read(goalsRepositoryProvider).listReviewQueue(goalId),
);
