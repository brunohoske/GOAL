import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/platform/app_blocker.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import 'blocking_sync.dart';

/// Guides the user to grant the special permissions blocking needs. Graceful: the app works
/// without them, but real blocking only happens once they're granted.
class BlockingPermissionsScreen extends ConsumerStatefulWidget {
  const BlockingPermissionsScreen({super.key});
  @override
  ConsumerState<BlockingPermissionsScreen> createState() => _State();
}

class _State extends ConsumerState<BlockingPermissionsScreen> with WidgetsBindingObserver {
  BlockerPermissions? _perms;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _refresh();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) _refresh();
  }

  Future<void> _refresh() async {
    final perms = await ref.read(appBlockerProvider).checkPermissions();
    if (mounted) setState(() => _perms = perms);
  }

  @override
  Widget build(BuildContext context) {
    final blocker = ref.read(appBlockerProvider);
    final p = _perms;

    return Scaffold(
      appBar: AppBar(title: const Text('Permissões de bloqueio')),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          Text(
            'Para bloquear apps de verdade, o Goal precisa de 3 permissões do sistema. '
            'Sem elas, o app funciona normalmente, mas não bloqueia.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: AppSpacing.xl),
          _PermTile(
            title: 'Acessibilidade',
            subtitle: 'Detecta quando um app bloqueado é aberto.',
            granted: p?.accessibility,
            onTap: blocker.openAccessibilitySettings,
          ),
          _PermTile(
            title: 'Sobreposição de tela',
            subtitle: 'Mostra o aviso de bloqueio sobre o app.',
            granted: p?.overlay,
            onTap: blocker.openOverlaySettings,
          ),
          _PermTile(
            title: 'Acesso ao uso',
            subtitle: 'Apoia a detecção de apps em uso.',
            granted: p?.usageAccess,
            onTap: blocker.openUsageAccessSettings,
          ),
        ],
      ),
    );
  }
}

class _PermTile extends StatelessWidget {
  const _PermTile({required this.title, required this.subtitle, required this.granted, required this.onTap});
  final String title;
  final String subtitle;
  final bool? granted;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Card(
        child: ListTile(
          contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.sm),
          title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(subtitle),
          trailing: granted == true
              ? const Icon(Icons.check_circle, color: AppColors.success)
              : FilledButton(onPressed: onTap, child: const Text('Conceder')),
        ),
      ),
    );
  }
}
