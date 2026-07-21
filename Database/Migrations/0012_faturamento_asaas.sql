-- =============================================================================
-- Migration 0012: dados de assinatura/cobrança (Asaas) por loja.
--
-- Escopo desta rodada (combinado com o usuário 2026-07-21): só automatizar a
-- cobrança de lojas JÁ cadastradas manualmente pelo superadmin (SuperAdmin/
-- Adegas) - não é o cadastro self-service completo (isso fica pra depois).
-- O superadmin escolhe um plano/valor pra loja e o sistema cria a assinatura
-- no Asaas; dali em diante, status_licenca (coluna já existente) passa a ser
-- atualizado automaticamente pelo webhook do Asaas (pagamento confirmado =
-- ativo, vencido = suspenso), substituindo a edição manual do status.
--
-- Todos os campos nullable de propósito - lojas já cadastradas antes desta
-- migration não têm assinatura, e criar a assinatura é uma ação separada e
-- posterior ao cadastro da loja.
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS plano text; -- 'start', 'pro' ou 'enterprise'
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS valor_mensalidade numeric(10,2);
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS asaas_customer_id text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS asaas_subscription_id text;
