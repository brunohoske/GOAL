import 'package:dio/dio.dart';

/// Retries idempotent GET requests that fail with a transient error — gateway
/// hiccups (502/503/504) or connection problems — before surfacing the failure.
/// Keeps short backoffs so a proxy blip doesn't blank out a whole screen.
class RetryInterceptor extends Interceptor {
  RetryInterceptor(this._dio);

  final Dio _dio;

  static const _maxAttempts = 3;
  static const _backoffs = [Duration(milliseconds: 400), Duration(milliseconds: 900)];

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final attempt = (err.requestOptions.extra['retry_attempt'] as int?) ?? 1;
    if (!_isTransient(err) || err.requestOptions.method != 'GET' || attempt >= _maxAttempts) {
      return handler.next(err);
    }

    await Future<void>.delayed(_backoffs[attempt - 1]);
    try {
      final options = err.requestOptions..extra['retry_attempt'] = attempt + 1;
      final response = await _dio.fetch<dynamic>(options);
      return handler.resolve(response);
    } on DioException catch (retryErr) {
      return handler.next(retryErr);
    }
  }

  bool _isTransient(DioException err) {
    final status = err.response?.statusCode;
    if (status == 502 || status == 503 || status == 504) return true;
    return err.type == DioExceptionType.connectionError ||
        err.type == DioExceptionType.connectionTimeout ||
        err.type == DioExceptionType.receiveTimeout;
  }
}
