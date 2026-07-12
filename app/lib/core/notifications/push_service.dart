import 'package:dio/dio.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/goals/data/goals_repository.dart';
import '../../features/notifications/data/notifications_repository.dart';
import '../network/dio_client.dart';

/// FCM integration with graceful degradation: when Firebase isn't configured
/// (no google-services.json yet), everything silently no-ops and the app keeps
/// working — notifications then arrive only in the in-app center on refresh.
class PushService {
  PushService(this._ref);
  final Ref _ref;

  final _local = FlutterLocalNotificationsPlugin();
  bool _initialized = false;
  bool _available = false;
  String? _token;

  Dio get _dio => _ref.read(dioProvider);

  Future<void> _init() async {
    if (_initialized) return;
    _initialized = true;

    try {
      await Firebase.initializeApp();
      _available = true;
    } catch (e) {
      debugPrint('Push desativado: Firebase não configurado ($e). '
          'Veja app/SETUP.md para adicionar o google-services.json.');
      return;
    }

    await _local.initialize(const InitializationSettings(
      android: AndroidInitializationSettings('@mipmap/ic_launcher'),
    ));

    // Foreground pushes: Android doesn't show them automatically, so mirror
    // them as a local notification and refresh the in-app data.
    FirebaseMessaging.onMessage.listen((message) {
      final n = message.notification;
      if (n != null) {
        _local.show(
          n.hashCode,
          n.title,
          n.body,
          const NotificationDetails(
            android: AndroidNotificationDetails(
              'goal_default',
              'Goal',
              channelDescription: 'Avisos de revisão, aprovação e bloqueio',
              importance: Importance.high,
              priority: Priority.high,
            ),
          ),
        );
      }
      _refreshFromPush(message.data);
    });

    FirebaseMessaging.instance.onTokenRefresh.listen(_sendToken);
  }

  void _refreshFromPush(Map<String, dynamic> data) {
    _ref.invalidate(notificationsProvider);
    final goalId = data['goalId'] as String?;
    if (goalId != null) {
      _ref.invalidate(blockingStateProvider(goalId));
      _ref.invalidate(reviewQueueProvider(goalId));
    }
  }

  /// Call after login: requests permission, grabs the token and registers it.
  Future<void> registerDevice() async {
    await _init();
    if (!_available) return;

    try {
      await FirebaseMessaging.instance.requestPermission();
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) await _sendToken(token);
    } catch (e) {
      debugPrint('Falha ao registrar push: $e');
    }
  }

  Future<void> _sendToken(String token) async {
    _token = token;
    await _dio.post('/devices', data: {'fcmToken': token, 'platform': 0});
  }

  /// Call on logout so this device stops receiving pushes.
  Future<void> unregisterDevice() async {
    final token = _token;
    if (token == null) return;
    try {
      await _dio.post('/devices/unregister', data: {'fcmToken': token});
    } catch (_) {
      // best effort — token will also be deactivated when FCM reports it dead
    }
  }
}

final pushServiceProvider = Provider<PushService>((ref) => PushService(ref));
