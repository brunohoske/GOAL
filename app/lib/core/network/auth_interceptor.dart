import 'package:dio/dio.dart';

import '../storage/secure_storage.dart';

/// Attaches the bearer token and, on a 401, transparently refreshes it once and retries.
/// Concurrent 401s queue behind a single in-flight refresh.
class AuthInterceptor extends Interceptor {
  AuthInterceptor({required this.dio, required this.storage, required this.onSignedOut});

  final Dio dio;
  final TokenStorage storage;
  final void Function() onSignedOut;

  Future<void>? _refreshing;

  @override
  Future<void> onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    if (!_isAuthEndpoint(options.path)) {
      final token = await storage.accessToken;
      if (token != null) options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final is401 = err.response?.statusCode == 401;
    final alreadyRetried = err.requestOptions.extra['retried'] == true;

    if (!is401 || alreadyRetried || _isAuthEndpoint(err.requestOptions.path)) {
      return handler.next(err);
    }

    try {
      await (_refreshing ??= _refreshToken());
      _refreshing = null;

      final token = await storage.accessToken;
      if (token == null) {
        onSignedOut();
        return handler.next(err);
      }

      final opts = err.requestOptions
        ..headers['Authorization'] = 'Bearer $token'
        ..extra['retried'] = true;
      final response = await dio.fetch(opts);
      return handler.resolve(response);
    } catch (_) {
      _refreshing = null;
      await storage.clear();
      onSignedOut();
      return handler.next(err);
    }
  }

  Future<void> _refreshToken() async {
    final refresh = await storage.refreshToken;
    if (refresh == null) throw StateError('no refresh token');

    final response = await dio.post('/auth/refresh', data: {'refreshToken': refresh});
    final data = response.data as Map<String, dynamic>;
    await storage.save(access: data['accessToken'] as String, refresh: data['refreshToken'] as String);
  }

  bool _isAuthEndpoint(String path) =>
      path.contains('/auth/login') || path.contains('/auth/register') || path.contains('/auth/refresh');
}
