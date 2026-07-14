-- =============================================================================
-- Migration 0001: fator de conversão de produtos, preços diferenciados,
-- ledger imutável do caixa e índices compostos multi-tenant.
--
-- Idempotente: seguro rodar mais de uma vez. Rodar manualmente no
-- SQL Editor do Supabase (Project > SQL Editor).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Fator de conversão em "produtos" (Un/Cx e quantidade por caixa)
-- -----------------------------------------------------------------------------
ALTER TABLE produtos
    ADD COLUMN IF NOT EXISTS unidade_medida text NOT NULL DEFAULT 'Un',
    ADD COLUMN IF NOT EXISTS quantidade_por_caixa int NOT NULL DEFAULT 1;

ALTER TABLE produtos
    DROP CONSTRAINT IF EXISTS produtos_quantidade_por_caixa_check;
ALTER TABLE produtos
    ADD CONSTRAINT produtos_quantidade_por_caixa_check CHECK (quantidade_por_caixa > 0);

-- -----------------------------------------------------------------------------
-- 2. Preços diferenciados por tipo (Balcão / Caixa / Atacado)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS precos_produto (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL,
    produto_id    uuid NOT NULL REFERENCES produtos (id) ON DELETE CASCADE,
    tipo_preco    text NOT NULL CHECK (tipo_preco IN ('BALCAO', 'CAIXA', 'ATACADO')),
    valor         numeric(10, 2) NOT NULL CHECK (valor >= 0),
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT precos_produto_unico UNIQUE (tenant_id, produto_id, tipo_preco)
);

-- -----------------------------------------------------------------------------
-- 3. Ledger de auditoria do caixa (append-only, com hash encadeado)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS caixa_ledger (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL,
    caixa_id          uuid NOT NULL REFERENCES caixa_turnos (id),
    valor             numeric(10, 2) NOT NULL,
    tipo_operacao     text NOT NULL CHECK (tipo_operacao IN ('DEBITO', 'CREDITO')),
    motivo            text NOT NULL,
    criado_em         timestamptz NOT NULL DEFAULT now(),
    hash_anterior     text,
    hash_verificacao  text NOT NULL
);

-- Bloqueia UPDATE/DELETE na tabela a nível de banco, independente do que a
-- aplicação faça — garante que o ledger é verdadeiramente imutável.
CREATE OR REPLACE FUNCTION caixa_ledger_bloquear_alteracao()
RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'caixa_ledger é append-only: % não é permitido nesta tabela', TG_OP;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_caixa_ledger_bloquear_update ON caixa_ledger;
CREATE TRIGGER trg_caixa_ledger_bloquear_update
    BEFORE UPDATE OR DELETE ON caixa_ledger
    FOR EACH ROW EXECUTE FUNCTION caixa_ledger_bloquear_alteracao();

-- -----------------------------------------------------------------------------
-- 4. Índices compostos (tenant_id, id) para isolamento multi-tenant eficiente
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_produtos_tenant_id            ON produtos (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_vendas_tenant_id               ON vendas (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_clientes_tenant_id             ON clientes (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_comandas_tenant_id             ON comandas (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_estoque_movimentacoes_tenant_id ON estoque_movimentacoes (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_caixa_movimentacoes_tenant_id  ON caixa_movimentacoes (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_caixa_turnos_tenant_id         ON caixa_turnos (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_precos_produto_tenant_id       ON precos_produto (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_caixa_ledger_tenant_id         ON caixa_ledger (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_caixa_ledger_caixa_id          ON caixa_ledger (caixa_id, criado_em);
