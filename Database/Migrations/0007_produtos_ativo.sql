-- =============================================================================
-- Migration 0007: permite desativar um produto sem apagá-lo (apagar quebraria
-- o histórico de vendas — vendas_itens.produto_id referencia produtos e não
-- tem ON DELETE CASCADE de propósito). A tela de cadastro (Admin/Estoque)
-- usa "ativo" pra esconder produtos descontinuados do PDV sem perder o
-- histórico de vendas/CMV desses produtos no dashboard.
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS ativo boolean NOT NULL DEFAULT true;

CREATE INDEX IF NOT EXISTS idx_produtos_ativo ON produtos (tenant_id, ativo);
