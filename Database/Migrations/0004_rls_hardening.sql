-- =============================================================================
-- Migration 0004: fecha um gap real de segurança das migrations 0001-0003.
--
-- Por padrão, o Supabase expõe toda tabela do schema "public" via API REST
-- (PostgREST) para qualquer portador da chave anon/publishable — que é uma
-- chave pública por design (já está até commitada em appsettings.json). O que
-- bloqueia esse acesso é Row Level Security: se a tabela não tiver RLS
-- habilitado, ela fica de fato pública para leitura/escrita via REST.
--
-- As tabelas criadas nas migrations 0001-0003 (precos_produto, caixa_ledger,
-- integracoes_webhook, webhooks_recebidos) foram criadas com CREATE TABLE puro,
-- sem habilitar RLS — o Postgres não liga RLS automaticamente. Isso é
-- especialmente grave em integracoes_webhook, que guarda o segredo HMAC de
-- cada tenant em texto puro: sem RLS, qualquer um com a chave anon conseguiria
-- ler esses segredos via REST.
--
-- Nenhuma dessas 4 tabelas é acessada pelo app via cliente Supabase/PostgREST —
-- toda leitura/escrita passa pela conexão direta Npgsql/Dapper (usuário
-- 'postgres', que sempre ignora RLS). Portanto habilitar RLS SEM nenhuma
-- policy (= deny-all para anon/authenticated via REST) é exatamente o que
-- essas tabelas precisam, e não quebra nada que o app realmente faz.
--
-- Rodar manualmente no SQL Editor do Supabase. Idempotente.
-- =============================================================================
ALTER TABLE precos_produto     ENABLE ROW LEVEL SECURITY;
ALTER TABLE caixa_ledger       ENABLE ROW LEVEL SECURITY;
ALTER TABLE integracoes_webhook ENABLE ROW LEVEL SECURITY;
ALTER TABLE webhooks_recebidos ENABLE ROW LEVEL SECURITY;

-- Reforço redundante e explícito: mesmo que alguém crie uma policy PERMISSIVE
-- displicente no futuro sem perceber a sensibilidade desta tabela específica,
-- REVOKE nos grants base do PostgREST barra o acesso das roles usadas pela API.
REVOKE ALL ON integracoes_webhook FROM anon, authenticated;
REVOKE ALL ON webhooks_recebidos  FROM anon, authenticated;

-- -----------------------------------------------------------------------------
-- IMPORTANTE — verificar manualmente (não é algo que este script possa garantir
-- de fora): confirme no Supabase Studio (Authentication > Policies, ou
-- Table Editor > ... > RLS) que TODAS as tabelas abaixo, criadas antes desta
-- migration, também têm RLS habilitado — foram assumidas como "já cobertas
-- pelo RLS ativo no banco" mas não foram criadas por estas migrations e não
-- foram auditadas aqui:
--   tenants, profiles, produtos, produtos_variacoes, clientes, comandas,
--   vendas, vendas_itens, estoque_movimentacoes, caixa_turnos,
--   caixa_movimentacoes
-- -----------------------------------------------------------------------------
