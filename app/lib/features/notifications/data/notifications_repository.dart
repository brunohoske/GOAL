import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';

class AppNotification {
  const AppNotification({
    required this.id,
    this.goalId,
    required this.type,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdAt,
  });

  final String id;
  final String? goalId;
  final int type;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;

  factory AppNotification.fromJson(Map<String, dynamic> j) => AppNotification(
        id: j['id'],
        goalId: j['goalId'],
        type: j['type'] as int,
        title: j['title'],
        body: j['body'],
        isRead: j['isRead'],
        createdAt: DateTime.parse(j['createdAt']).toLocal(),
      );
}

class NotificationsRepository {
  NotificationsRepository(this._dio);
  final Dio _dio;

  Future<List<AppNotification>> list() async {
    final r = await _dio.get('/notifications');
    return (r.data as List).map((e) => AppNotification.fromJson(e)).toList();
  }

  Future<void> markRead(String id) => _dio.post('/notifications/$id/read');

  Future<void> markAllRead() => _dio.post('/notifications/read-all');
}

final notificationsRepositoryProvider =
    Provider<NotificationsRepository>((ref) => NotificationsRepository(ref.read(dioProvider)));

final notificationsProvider = FutureProvider<List<AppNotification>>(
  (ref) => ref.read(notificationsRepositoryProvider).list(),
);

/// Unread count for the bottom-nav badge (derived from the same list).
final unreadCountProvider = FutureProvider<int>((ref) async {
  final items = await ref.watch(notificationsProvider.future);
  return items.where((n) => !n.isRead).length;
});
