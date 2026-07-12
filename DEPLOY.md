# Deploy do backend Goal (grátis) + APK apontando para ele

## Por que o Goal precisa de um servidor "sempre ligado"

O produto depende de duas coisas que **quebram em hospedagens que "dormem"**:

1. O celular consulta o `blocking-state` com frequência — se o servidor hiberna
   (Render free, Cloud Run sem instância mínima), cada consulta pode levar 30–60s.
2. O **Hangfire** roda jobs recorrentes (fechar sprint, escalar notificações). Se o
   processo dorme, sprint não fecha e ninguém é notificado.

## Recomendação: Oracle Cloud "Always Free"

É a única opção realmente **grátis para sempre e sem dormir**: até 4 vCPUs ARM +
24 GB de RAM em VMs que ficam ligadas 24/7. Roda o `docker-compose.prod.yml`
inteiro (API + Postgres) com folga.

- Alternativas grátis e por que não: **Render free** (API dorme após 15 min e o
  Postgres grátis expira em 30 dias), **Railway/Fly.io** (créditos de teste, não é
  grátis contínuo), **Cloud Run** (escala a zero → mata o Hangfire).
- Se a Oracle recusar seu cadastro (acontece), o plano B mais barato é um VPS de
  ~US$ 4–5/mês (Hetzner, Contabo, DigitalOcean) — os passos abaixo são os mesmos.

## Passo a passo — servidor

1. **Conta:** crie em <https://www.oracle.com/cloud/free/> (pede cartão para
   verificação; não cobra no Always Free).

2. **VM:** Console → Compute → Instances → *Create instance*
   - Image: **Ubuntu 24.04**; Shape: **Ampere A1.Flex (Always Free)**, 2 OCPUs / 12 GB já basta.
   - Baixe/guarde a chave SSH gerada.

3. **Abra a porta da API** (5080):
   - Networking → Virtual Cloud Networks → sua VCN → Security List → *Add Ingress Rule*:
     Source `0.0.0.0/0`, protocolo TCP, destination port `5080`.
   - Na VM (o Ubuntu da Oracle também tem firewall próprio):
     ```bash
     sudo iptables -I INPUT -p tcp --dport 5080 -j ACCEPT
     sudo netfilter-persistent save
     ```

4. **Instale o Docker** (via SSH: `ssh ubuntu@IP_DA_VM`):
   ```bash
   curl -fsSL https://get.docker.com | sudo sh
   sudo usermod -aG docker ubuntu && newgrp docker
   ```

5. **Suba o projeto** (só o backend é necessário — a pasta `app/` é ignorada pelo build):
   ```bash
   git clone <seu-repositorio> goal && cd goal
   # (sem git: copie a pasta com scp/rsync)
   cp .env.example .env
   nano .env   # defina POSTGRES_PASSWORD e JWT_SIGNING_KEY fortes
   docker compose -f docker-compose.prod.yml up -d --build
   ```
   As migrations rodam sozinhas na subida. Teste: `curl http://localhost:5080/api/v1/goals`
   deve responder **401** (exige login — sinal de que está no ar).

6. **Teste de fora:** no navegador do celular, `http://IP_DA_VM:5080/api/v1/goals`
   deve mostrar um erro 401 — perfeito.

## Passo a passo — APK apontando para o servidor

O endereço da API entra em **tempo de build** via `--dart-define`:

```bash
cd app
flutter build apk --release --dart-define=API_BASE_URL=http://IP_DA_VM:5080/api/v1
```

O APK sai em `app/build/app/outputs/flutter-apk/app-release.apk` — mande para os
amigos instalarem (vão precisar permitir "instalar apps desconhecidos").

Para desenvolvimento local nada muda: `flutter run` continua usando
`localhost` + `adb reverse`.

## Atualizando o backend depois

```bash
cd goal && git pull
docker compose -f docker-compose.prod.yml up -d --build
```
Dados e uploads ficam em volumes (`goal_pgdata`, `goal_uploads`) e sobrevivem.

## Antes de publicar na Play Store (não bloqueia o uso com amigos)

- **HTTPS:** hoje o app aceita HTTP (`usesCleartextTraffic="true"`). Para produção
  de verdade: aponte um domínio para a VM, ponha um **Caddy** na frente
  (`caddy reverse-proxy --from seudominio.com --to localhost:5080`, HTTPS automático),
  rebuilde o APK com `https://` e remova o `usesCleartextTraffic` do manifest.
- **FCM:** siga a seção Firebase do `app/SETUP.md`; no servidor, monte o
  `firebase-credentials.json` (linhas comentadas no compose).
- **Backup:** `docker exec goalprod-db-1 pg_dump -U goal goal > backup.sql` num cron.
