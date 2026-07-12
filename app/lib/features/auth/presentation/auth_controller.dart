import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/auth_repository.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  const AuthState({required this.status, this.displayName, this.isLoading = false, this.error});
  final AuthStatus status;
  final String? displayName;
  final bool isLoading;
  final String? error;

  AuthState copyWith({AuthStatus? status, String? displayName, bool? isLoading, String? error}) =>
      AuthState(
        status: status ?? this.status,
        displayName: displayName ?? this.displayName,
        isLoading: isLoading ?? this.isLoading,
        error: error,
      );
}

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._repo) : super(const AuthState(status: AuthStatus.unknown)) {
    _bootstrap();
  }

  final AuthRepository _repo;

  Future<void> _bootstrap() async {
    final hasSession = await _repo.hasSession();
    state = state.copyWith(
      status: hasSession ? AuthStatus.authenticated : AuthStatus.unauthenticated,
    );
  }

  Future<void> login(String email, String password) =>
      _run(() => _repo.login(email, password));

  Future<void> register(String email, String name, String password) =>
      _run(() => _repo.register(email, name, password));

  Future<void> _run(Future<AuthTokens> Function() action) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final tokens = await action();
      state = AuthState(status: AuthStatus.authenticated, displayName: tokens.displayName);
    } on DioException catch (e) {
      state = state.copyWith(isLoading: false, error: _message(e));
    }
  }

  Future<void> logout() async {
    await _repo.logout();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void markSignedOut() => state = const AuthState(status: AuthStatus.unauthenticated);

  String _message(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['detail'] != null) return data['detail'].toString();
    if (e.response?.statusCode == 401) return 'E-mail ou senha inválidos.';
    return 'Algo deu errado. Tente novamente.';
  }
}

final authControllerProvider = StateNotifierProvider<AuthController, AuthState>(
  (ref) => AuthController(ref.read(authRepositoryProvider)),
);
