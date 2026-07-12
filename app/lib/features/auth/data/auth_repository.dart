import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/dio_client.dart';
import '../../../core/storage/secure_storage.dart';

class AuthTokens {
  const AuthTokens({required this.accessToken, required this.refreshToken, required this.userId, required this.displayName});
  final String accessToken;
  final String refreshToken;
  final String userId;
  final String displayName;

  factory AuthTokens.fromJson(Map<String, dynamic> j) => AuthTokens(
        accessToken: j['accessToken'],
        refreshToken: j['refreshToken'],
        userId: j['userId'],
        displayName: j['displayName'],
      );
}

class AuthRepository {
  AuthRepository(this._dio, this._storage);
  final Dio _dio;
  final TokenStorage _storage;

  Future<AuthTokens> register(String email, String displayName, String password) async {
    final r = await _dio.post('/auth/register', data: {
      'email': email,
      'displayName': displayName,
      'password': password,
    });
    return _persist(AuthTokens.fromJson(r.data));
  }

  Future<AuthTokens> login(String email, String password) async {
    final r = await _dio.post('/auth/login', data: {'email': email, 'password': password});
    return _persist(AuthTokens.fromJson(r.data));
  }

  Future<void> logout() => _storage.clear();

  Future<bool> hasSession() async => (await _storage.accessToken) != null;

  /// Current user's profile (id, email, displayName, avatarUrl).
  Future<Map<String, dynamic>> me() async {
    final r = await _dio.get('/auth/me');
    return r.data as Map<String, dynamic>;
  }

  Future<void> changePassword(String current, String newPassword) => _dio.post(
        '/auth/change-password',
        data: {'currentPassword': current, 'newPassword': newPassword},
      );

  Future<AuthTokens> _persist(AuthTokens tokens) async {
    await _storage.save(access: tokens.accessToken, refresh: tokens.refreshToken);
    return tokens;
  }
}

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.read(dioProvider), ref.read(tokenStorageProvider)),
);
