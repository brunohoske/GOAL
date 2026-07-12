import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';

/// Whether the user has completed the first-run onboarding.
/// Overridden in main() with the persisted value; the router redirects while false.
final onboardingDoneProvider = StateProvider<bool>((_) => false);

const onboardingPrefKey = 'onboarding_done';

/// First-run onboarding. The last page is the prominent disclosure required by the
/// Play Store: the app blocks the social apps the group chose, via Accessibility,
/// and only with the user's explicit opt-in.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingState();
}

class _OnboardingState extends ConsumerState<OnboardingScreen> {
  final _controller = PageController();
  int _page = 0;

  static const _pages = [
    (
      icon: Icons.flag_rounded,
      title: 'GOALs em grupo',
      body: 'Crie um GOAL com seus amigos, montem juntos o catálogo de tarefas '
          'e avancem sprint a sprint. Cada tarefa concluída vale XP.',
    ),
    (
      icon: Icons.how_to_vote_rounded,
      title: 'Provas, não promessas',
      body: 'Concluir uma tarefa exige documentar o que foi feito — texto, foto ou anexo. '
          'O grupo vota: só com a maioria aprovando o XP é seu.',
    ),
    (
      icon: Icons.lock_rounded,
      title: 'Bloqueio de verdade',
      body: 'Se você não atingir sua meta de XP na sprint, o Goal BLOQUEIA os apps '
          'que o grupo escolheu (ex: Instagram, TikTok) — e também os sites deles '
          'no navegador — até você se recuperar.\n\n'
          'O criador do GOAL pode ainda ligar o "modo caos": lembretes surpresa na '
          'tela e a troca do que você digita por uma cobrança — só enquanto você '
          'estiver devendo XP.\n\n'
          'Para isso o app usa o serviço de Acessibilidade do Android, com a sua '
          'permissão explícita (que você concede e pode revogar no sistema). Ele só '
          'verifica o app aberto, o endereço do navegador e o campo em edição no '
          'momento — nada é armazenado ou enviado para fora do aparelho.',
    ),
  ];

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _finish() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(onboardingPrefKey, true);
    ref.read(onboardingDoneProvider.notifier).state = true; // router redirects
  }

  @override
  Widget build(BuildContext context) {
    final isLast = _page == _pages.length - 1;

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            const SizedBox(height: AppSpacing.xxl),
            const GoalWordmark(fontSize: 36),
            Expanded(
              child: PageView.builder(
                controller: _controller,
                onPageChanged: (i) => setState(() => _page = i),
                itemCount: _pages.length,
                itemBuilder: (_, i) {
                  final p = _pages[i];
                  return Padding(
                    padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xxl),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          width: 96,
                          height: 96,
                          decoration: const BoxDecoration(
                            color: AppColors.primaryContainer,
                            shape: BoxShape.circle,
                          ),
                          child: Icon(p.icon, size: 44, color: AppColors.primary),
                        ),
                        const SizedBox(height: AppSpacing.xxl),
                        Text(p.title,
                            style: Theme.of(context).textTheme.headlineMedium,
                            textAlign: TextAlign.center),
                        const SizedBox(height: AppSpacing.lg),
                        Text(p.body,
                            style: Theme.of(context)
                                .textTheme
                                .bodyLarge
                                ?.copyWith(color: AppColors.onSurfaceMuted, height: 1.5),
                            textAlign: TextAlign.center),
                      ],
                    ),
                  );
                },
              ),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(_pages.length, (i) {
                return AnimatedContainer(
                  duration: const Duration(milliseconds: 200),
                  margin: const EdgeInsets.symmetric(horizontal: 4),
                  width: i == _page ? 24 : 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: i == _page ? AppColors.primary : AppColors.outline,
                    borderRadius: BorderRadius.circular(4),
                  ),
                );
              }),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Row(
                children: [
                  if (!isLast)
                    TextButton(
                      onPressed: _finish,
                      child: const Text('Pular'),
                    ),
                  const Spacer(),
                  FilledButton(
                    onPressed: isLast
                        ? _finish
                        : () => _controller.nextPage(
                            duration: const Duration(milliseconds: 250), curve: Curves.easeOut),
                    child: Text(isLast ? 'Entendi, começar' : 'Próximo'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
