-- =============================================================================
-- Migration 0002: garante no máximo 1 turno ABERTO por usuário/tenant,
-- evitando abertura duplicada por corrida de cliques/abas.
--
-- Idempotente: seguro rodar mais de uma vez. Rodar manualmente no
-- SQL Editor do Supabase (Project > SQL Editor).
-- =============================================================================
CREATE UNIQUE INDEX IF NOT EXISTS idx_caixa_turnos_um_aberto_por_usuario
    ON caixa_turnos (tenant_id, usuario_id)
    WHERE status = 'ABERTO';
