-- =============================================================================
-- Migration 0006: fecha o gap de RLS encontrado no teste end-to-end de QA.
--
-- Duas falhas distintas em tabelas pré-existentes (criadas antes destas
-- migrations, nunca auditadas até agora):
--
-- 1. profiles estava com RLS DESLIGADO — qualquer usuário autenticado podia
--    ler/editar QUALQUER perfil via API REST, inclusive o próprio "role"
--    (escalação de privilégio: um vendedor podia se promover a superadmin
--    com um PATCH direto na API). Liga RLS + policy: cada um só enxerga o
--    próprio perfil.
--
-- 2. produtos e precos_produto tinham RLS ligado mas ZERO policies — nega
--    tudo por padrão pra qualquer sessão que não seja o superuser 'postgres'.
--    Isso não afeta os serviços via Dapper (conectam como 'postgres', que
--    sempre ignora RLS), mas quebrava o cliente Supabase/REST usado pelo PDV
--    (catálogo de produtos) e pelo processador do webhook Zé Delivery (busca
--    de produto por SKU) — confirmado em teste ao vivo: catálogo do PDV
--    vinha sempre vazio mesmo com produto cadastrado no tenant certo.
--
-- Demais tabelas (clientes, comandas, vendas, vendas_itens,
-- estoque_movimentacoes, caixa_turnos, caixa_movimentacoes, caixa_ledger,
-- produtos_variacoes, tenants, integracoes_webhook, webhooks_recebidos)
-- CONTINUAM sem policy de propósito — nada no código hoje lê/escreve nelas
-- via cliente Supabase/REST (só via Dapper/postgres), então "negar tudo via
-- REST" é a postura correta pra elas agora. Se algum dia uma tela passar a
-- usar o cliente Supabase pra alguma dessas, adicione a policy específica
-- naquele momento — não crie policy ampla "por via das dúvidas".
--
-- Rodar manualmente no SQL Editor do Supabase. Idempotente.
-- =============================================================================

ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "profiles_select_own" ON profiles;
CREATE POLICY "profiles_select_own" ON profiles
    FOR SELECT
    TO authenticated
    USING (id = auth.uid());

-- De propósito, SEM policy de INSERT/UPDATE/DELETE em profiles: hoje nada no
-- app escreve nela via REST (só lê, no login). Alteração de role/tenant_id
-- deve continuar passando só pela conexão admin (Dapper/postgres) — nunca
-- editável pelo próprio usuário via API pública, que é exatamente o vetor de
-- escalação de privilégio que este item fecha.

DROP POLICY IF EXISTS "produtos_select_do_proprio_tenant" ON produtos;
CREATE POLICY "produtos_select_do_proprio_tenant" ON produtos
    FOR SELECT
    TO authenticated
    USING (tenant_id = (SELECT tenant_id FROM profiles WHERE id = auth.uid()));

DROP POLICY IF EXISTS "precos_produto_select_do_proprio_tenant" ON precos_produto;
CREATE POLICY "precos_produto_select_do_proprio_tenant" ON precos_produto
    FOR SELECT
    TO authenticated
    USING (tenant_id = (SELECT tenant_id FROM profiles WHERE id = auth.uid()));
