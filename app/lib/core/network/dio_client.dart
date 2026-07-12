import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

import '../config/env.dart';
import '../storage/secure_storage.dart';
import 'auth_interceptor.dart';

/// Configured Dio instance: base URL, JSON, auth + refresh interceptors, logging.
final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: Env.apiBaseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 20),
    contentType: 'application/json',
  ));

  final storage = ref.read(tokenStorageProvider);
  dio.interceptors.add(AuthInterceptor(dio: dio, storage: storage, onSignedOut: () {
    ref.read(authSignedOutProvider.notifier).state = true;
  }));
  dio.interceptors.add(PrettyDioLogger(requestBody: true, responseBody: false, compact: true));

  return dio;
});

/// Flips to true when a refresh fails and the user must re-authenticate.
/// The router watches this to redirect to login.
final authSignedOutProvider = StateProvider<bool>((ref) => false);
