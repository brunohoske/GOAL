import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import 'auth_controller.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});
  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Center(child: GoalWordmark(fontSize: 40)),
                const SizedBox(height: AppSpacing.sm),
                Center(
                  child: Text('Conquiste seus objetivos, juntos.',
                      style: Theme.of(context).textTheme.bodySmall),
                ),
                const SizedBox(height: AppSpacing.xxl * 1.5),
                TextField(
                  controller: _email,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(labelText: 'E-mail'),
                ),
                const SizedBox(height: AppSpacing.md),
                TextField(
                  controller: _password,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Senha'),
                ),
                if (state.error != null) ...[
                  const SizedBox(height: AppSpacing.md),
                  Text(state.error!, style: const TextStyle(color: AppColors.danger, fontSize: 13)),
                ],
                const SizedBox(height: AppSpacing.xl),
                FilledButton(
                  onPressed: state.isLoading
                      ? null
                      : () => ref.read(authControllerProvider.notifier).login(_email.text.trim(), _password.text),
                  child: state.isLoading
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Text('Entrar'),
                ),
                const SizedBox(height: AppSpacing.md),
                TextButton(
                  onPressed: () => context.push('/register'),
                  child: const Text('Criar uma conta'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
