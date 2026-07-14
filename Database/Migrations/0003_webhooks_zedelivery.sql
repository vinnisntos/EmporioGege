-- =============================================================================
-- Migration 0003: infraestrutura de webhooks de integração (Zé Delivery e
-- futuros canais), com token de URL por tenant + idempotência/auditoria.
--
-- Idempotente: seguro rodar mais de uma vez. Rodar manualmente no
-- SQL Editor do Supabase (Project > SQL Editor).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Credenciais de webhook por tenant/canal.
--    token_url vai na URL (ex.: /webhooks/zedelivery/{token_url}) para resolver
--    o tenant sem depender de o payload do parceiro trazer nosso tenant_id.
--    segredo_hmac valida a assinatura do webhook (HMAC-SHA256 sobre o corpo bruto).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS integracoes_webhook (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL,
    canal         text NOT NULL CHECK (canal IN ('ZEDELIVERY')),
    token_url     text NOT NULL,
    segredo_hmac  text NOT NULL,
    ativo         boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT integracoes_webhook_token_unico UNIQUE (token_url),
    CONSTRAINT integracoes_webhook_tenant_canal_unico UNIQUE (tenant_id, canal)
);

-- -----------------------------------------------------------------------------
-- 2. Eventos de webhook recebidos: idempotência (não processar o mesmo evento
--    duas vezes em reentregas do parceiro) + auditoria/fila durável.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS webhooks_recebidos (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id          uuid NOT NULL,
    canal              text NOT NULL CHECK (canal IN ('ZEDELIVERY')),
    evento_externo_id  text NOT NULL,
    payload            jsonb NOT NULL,
    status             text NOT NULL DEFAULT 'RECEBIDO' CHECK (status IN ('RECEBIDO', 'PROCESSANDO', 'PROCESSADO', 'ERRO')),
    erro               text,
    recebido_em        timestamptz NOT NULL DEFAULT now(),
    processado_em      timestamptz,
    CONSTRAINT webhooks_recebidos_evento_unico UNIQUE (tenant_id, canal, evento_externo_id)
);

CREATE INDEX IF NOT EXISTS idx_integracoes_webhook_tenant_id  ON integracoes_webhook (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_webhooks_recebidos_tenant_id   ON webhooks_recebidos (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_webhooks_recebidos_pendentes   ON webhooks_recebidos (status, recebido_em) WHERE status = 'RECEBIDO';
