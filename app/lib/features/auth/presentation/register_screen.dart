import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import 'auth_controller.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});
  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(authControllerProvider);

    return Scaffold(
      appBar: AppBar(),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const GoalWordmark(fontSize: 32),
              const SizedBox(height: AppSpacing.sm),
              Text('Crie sua conta', style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: AppSpacing.xxl),
              TextField(controller: _name, decoration: const InputDecoration(labelText: 'Nome')),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _email,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(labelText: 'E-mail'),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _password,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Senha (mín. 8 caracteres)'),
              ),
              if (state.error != null) ...[
                const SizedBox(height: AppSpacing.md),
                Text(state.error!, style: const TextStyle(color: AppColors.danger, fontSize: 13)),
              ],
              const SizedBox(height: AppSpacing.xl),
              FilledButton(
                onPressed: state.isLoading
                    ? null
                    : () => ref
                        .read(authControllerProvider.notifier)
                        .register(_email.text.trim(), _name.text.trim(), _password.text),
                child: state.isLoading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Criar conta'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
