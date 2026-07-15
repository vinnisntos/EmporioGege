-- =============================================================================
-- Migration 0008: suporte a telas de Comandas (abertura/consumo/fechamento/
-- cancelamento).
--
-- comandas_itens é uma tabela de staging: guarda o que foi lançado numa
-- comanda ABERTA só para exibir o "carrinho" acumulado e o total corrente.
-- A baixa de estoque real e o registro em vendas/vendas_itens só acontecem
-- no fechamento (VendaService.FinalizarVendaAsync, chamado por
-- ComandaService.FecharAsync) — nada aqui decrementa estoque.
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================

CREATE TABLE IF NOT EXISTS comandas_itens (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL,
    comanda_id               uuid NOT NULL REFERENCES comandas (id),
    produto_id                uuid NOT NULL REFERENCES produtos (id),
    quantidade               integer NOT NULL CHECK (quantidade > 0),
    tipo_preco                text NOT NULL DEFAULT 'BALCAO',
    preco_unitario_aplicado  numeric(12, 2) NOT NULL,
    custo_unitario_aplicado  numeric(12, 2) NOT NULL,
    subtotal                 numeric(12, 2) NOT NULL,
    criado_em                timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_comandas_itens_comanda ON comandas_itens (tenant_id, comanda_id);

-- comandas_itens só é acessada via Dapper (conexão admin) — mesmo padrão de
-- precos_produto/caixa_ledger na migration 0004: RLS habilitado, sem
-- nenhuma policy (deny-all para anon/authenticated via REST/PostgREST).
ALTER TABLE comandas_itens ENABLE ROW LEVEL SECURITY;

-- Usado pela tela Admin/Comandas pra saber quando uma comanda mudou de
-- status (aberta -> fechada/cancelada), já que created_at é fixo na abertura.
ALTER TABLE comandas ADD COLUMN IF NOT EXISTS atualizado_em timestamptz NOT NULL DEFAULT now();

-- Evita duas comandas abertas com o mesmo número no mesmo tenant (ex.: duas
-- "Mesa 5" simultâneas por engano). O número libera de novo assim que a
-- comanda fecha ou é cancelada — mesmo padrão da migration 0002 (turno único
-- aberto), mas por número em vez de por usuário.
CREATE UNIQUE INDEX IF NOT EXISTS idx_comandas_numero_aberta_unico
    ON comandas (tenant_id, numero_comanda)
    WHERE status = 'ABERTA';
