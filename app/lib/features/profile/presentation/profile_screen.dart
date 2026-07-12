import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/notifications/push_service.dart';
import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../auth/data/auth_repository.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../blocking/presentation/blocking_permissions_screen.dart';

final meProvider = FutureProvider<Map<String, dynamic>>(
  (ref) => ref.read(authRepositoryProvider).me(),
);

/// Profile & settings: account info, change password, blocking permissions, logout.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final meAsync = ref.watch(meProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Perfil')),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          meAsync.when(
            loading: () => const Card(
              child: SizedBox(height: 96, child: Center(child: CircularProgressIndicator())),
            ),
            error: (_, __) => const SizedBox.shrink(),
            data: (me) {
              final name = (me['displayName'] as String?) ?? '';
              final email = (me['email'] as String?) ?? '';
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 28,
                        backgroundColor: AppColors.primaryContainer,
                        child: Text(name.isNotEmpty ? name[0].toUpperCase() : '?',
                            style: const TextStyle(
                                color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 22)),
                      ),
                      const SizedBox(width: AppSpacing.lg),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(name, style: Theme.of(context).textTheme.titleLarge),
                            const SizedBox(height: 2),
                            Text(email, style: Theme.of(context).textTheme.bodySmall),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
          const SizedBox(height: AppSpacing.lg),

          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.lock_reset, color: AppColors.primary),
                  title: const Text('Trocar senha'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => _changePassword(context, ref),
                ),
                const Divider(height: 1, indent: AppSpacing.lg, endIndent: AppSpacing.lg),
                ListTile(
                  leading: const Icon(Icons.security, color: AppColors.primary),
                  title: const Text('Permissões de bloqueio'),
                  subtitle: const Text('Acessibilidade e sobreposição de tela'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(
                    MaterialPageRoute(builder: (_) => const BlockingPermissionsScreen()),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.lg),

          Card(
            child: ListTile(
              leading: const Icon(Icons.logout, color: AppColors.danger),
              title: const Text('Sair', style: TextStyle(color: AppColors.danger)),
              onTap: () => _logout(context, ref),
            ),
          ),

          const SizedBox(height: AppSpacing.xxl),
          const Center(child: GoalWordmark(fontSize: 20)),
          const SizedBox(height: AppSpacing.sm),
          Center(
            child: Text('v1.0.0', style: Theme.of(context).textTheme.bodySmall),
          ),
        ],
      ),
    );
  }

  void _changePassword(BuildContext context, WidgetRef ref) {
    final current = TextEditingController();
    final next = TextEditingController();
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => Padding(
        padding: EdgeInsets.fromLTRB(AppSpacing.xl, AppSpacing.xl, AppSpacing.xl,
            MediaQuery.of(ctx).viewInsets.bottom + AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Trocar senha', style: Theme.of(ctx).textTheme.headlineSmall),
            const SizedBox(height: AppSpacing.lg),
            TextField(
              controller: current,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Senha atual'),
            ),
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: next,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Nova senha (mín. 8 caracteres)'),
            ),
            const SizedBox(height: AppSpacing.lg),
            FilledButton(
              onPressed: () async {
                try {
                  await ref
                      .read(authRepositoryProvider)
                      .changePassword(current.text, next.text);
                  if (ctx.mounted) {
                    Navigator.pop(ctx);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Senha alterada.')),
                    );
                  }
                } catch (_) {
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      const SnackBar(content: Text('Não foi possível alterar. Confira a senha atual.')),
                    );
                  }
                }
              },
              child: const Text('Salvar'),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _logout(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sair da conta?'),
        content: const Text('Você vai precisar entrar de novo para ver seus GOALs.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Sair')),
        ],
      ),
    );
    if (confirmed != true) return;

    await ref.read(pushServiceProvider).unregisterDevice();
    await ref.read(authControllerProvider.notifier).logout();
  }
}
