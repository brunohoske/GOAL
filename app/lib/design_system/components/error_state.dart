import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// A calm error state matching [EmptyState]'s tone: friendly message derived
/// from the failure (never a raw exception dump) plus a retry action.
class ErrorState extends StatelessWidget {
  const ErrorState({super.key, required this.error, this.onRetry});

  final Object error;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xxl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off_outlined, size: 48, color: AppColors.onSurfaceMuted),
            const SizedBox(height: AppSpacing.lg),
            Text(friendlyErrorMessage(error),
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: AppSpacing.sm),
            Text('Puxe para baixo para atualizar ou tente novamente.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodySmall),
            if (onRetry != null) ...[
              const SizedBox(height: AppSpacing.xl),
              OutlinedButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
                label: const Text('Tentar novamente'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Maps an exception to a short human message in pt-BR.
String friendlyErrorMessage(Object error) {
  if (error is DioException) {
    final status = error.response?.statusCode;
    if (status != null && status >= 500) {
      return 'O servidor está instável no momento.';
    }
    switch (error.type) {
      case DioExceptionType.connectionError:
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return 'Sem conexão com o servidor.';
      default:
        return 'Não foi possível carregar os dados.';
    }
  }
  return 'Algo deu errado ao carregar.';
}
