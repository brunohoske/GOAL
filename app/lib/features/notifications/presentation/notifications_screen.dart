import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../design_system/components/empty_state.dart';
import '../../../design_system/components/error_state.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../data/notifications_repository.dart';

/// Notification center: everything the backend recorded (reviews, approvals, blocking nags).
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final itemsAsync = ref.watch(notificationsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Alertas'),
        actions: [
          TextButton(
            onPressed: () async {
              await ref.read(notificationsRepositoryProvider).markAllRead();
              ref.invalidate(notificationsProvider);
            },
            child: const Text('Marcar lidas'),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(notificationsProvider),
        child: itemsAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => ErrorState(
            error: e,
            onRetry: () => ref.invalidate(notificationsProvider),
          ),
          data: (items) {
            if (items.isEmpty) {
              return const EmptyState(
                icon: Icons.notifications_none,
                title: 'Nenhum alerta',
                message: 'Revisões pendentes, aprovações e avisos de bloqueio aparecem aqui.',
              );
            }
            return ListView.separated(
              padding: const EdgeInsets.all(AppSpacing.lg),
              itemCount: items.length,
              separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
              itemBuilder: (_, i) => _NotificationTile(item: items[i]),
            );
          },
        ),
      ),
    );
  }
}

class _NotificationTile extends ConsumerWidget {
  const _NotificationTile({required this.item});
  final AppNotification item;

  static const _icons = <int, IconData>{
    0: Icons.mail_outline, // InviteReceived
    1: Icons.how_to_vote_outlined, // ReviewRequested
    2: Icons.check_circle_outline, // CompletionApproved
    3: Icons.cancel_outlined, // CompletionRejected
    4: Icons.timer_outlined, // SprintEndingSoon
    5: Icons.lock_outline, // BlockedReminder
    6: Icons.trending_down, // DebtWarning
  };

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Card(
      color: item.isRead ? null : AppColors.primaryContainer.withOpacity(0.4),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppColors.surfaceAlt,
          child: Icon(_icons[item.type] ?? Icons.notifications_none,
              color: AppColors.primary, size: 20),
        ),
        title: Text(item.title,
            style: TextStyle(fontWeight: item.isRead ? FontWeight.w500 : FontWeight.w700)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(item.body),
            const SizedBox(height: 2),
            Text(DateFormat('dd/MM · HH:mm').format(item.createdAt),
                style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
        onTap: () async {
          if (!item.isRead) {
            await ref.read(notificationsRepositoryProvider).markRead(item.id);
            ref.invalidate(notificationsProvider);
          }
          if (item.goalId != null && context.mounted) {
            context.push('/goals/${item.goalId}');
          }
        },
      ),
    );
  }
}
