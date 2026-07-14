-- =============================================================================
-- Migration 0005: fecha o gap encontrado na revisão de QA — o PDV nunca gravava
-- a venda (vendas/vendas_itens) nem creditava o caixa_ledger, então Faturamento/
-- CMV/ROI do dashboard e o "saldo sistema" do fechamento de turno nunca refletiam
-- vendas reais. Para o VendaService gravar vendas_itens sem depender de uma tela
-- de variações de produto que nunca foi construída, e para pedidos do Zé Delivery
-- (que não têm um turno de caixa físico associado) também poderem virar vendas:
--
-- 1. vendas_itens.variacao_id passa a aceitar NULL, e ganha produto_id (referência
--    direta ao produto, usada pelo fluxo atual de PDV/webhook). Pelo menos um dos
--    dois precisa estar preenchido.
-- 2. vendas.turno_id passa a aceitar NULL (vendas do Zé Delivery não pertencem a
--    nenhum turno de caixa físico).
-- 3. Índice composto (tenant_id, id) que faltou em vendas_itens na migration 0001.
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================
ALTER TABLE vendas_itens
    ALTER COLUMN variacao_id DROP NOT NULL,
    ADD COLUMN IF NOT EXISTS produto_id uuid REFERENCES produtos (id);

ALTER TABLE vendas_itens
    DROP CONSTRAINT IF EXISTS vendas_itens_produto_ou_variacao;
ALTER TABLE vendas_itens
    ADD CONSTRAINT vendas_itens_produto_ou_variacao CHECK (produto_id IS NOT NULL OR variacao_id IS NOT NULL);

ALTER TABLE vendas
    ALTER COLUMN turno_id DROP NOT NULL;

CREATE INDEX IF NOT EXISTS idx_vendas_itens_tenant_id ON vendas_itens (tenant_id, id);
CREATE INDEX IF NOT EXISTS idx_vendas_itens_produto_id ON vendas_itens (produto_id);
