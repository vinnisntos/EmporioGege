-- =============================================================================
-- Migration 0009: dados de apoio pra testar o fluxo de Comandas (0008) de
-- ponta a ponta em ambiente de desenvolvimento.
--
-- 1) Insere um produto de teste ativo no tenant do usuário de teste
--    caixa01@emporiogege.com — sem isso não dava pra testar
--    adicionar-item/fechar-comanda (o tenant de teste não tinha nenhum
--    produto cadastrado).
-- 2) Vincula todo perfil com role 'superadmin' (que ainda não tenha
--    tenant_id) ao mesmo tenant do caixa01, só pra destravar as telas
--    Admin/* (Admin/Comandas, Admin/Estoque etc.), que exigem tenant_id,
--    em ambiente de teste. Isso tira o superadmin do estado "sem loja".
--
-- ATENÇÃO — reversível manualmente, avaliar se é o comportamento desejado
-- fora de um ambiente de teste (rodar depois de terminar os testes):
--   UPDATE profiles SET tenant_id = NULL WHERE role = 'superadmin';
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================

INSERT INTO produtos (
    id, tenant_id, nome, codigo_barras, custo_medio, preco_venda_base,
    estoque_atual, estoque_minimo, unidade_medida, quantidade_por_caixa,
    data_validade, ativo, created_at
)
SELECT
    gen_random_uuid(), p.tenant_id, 'Produto Teste - Cerveja Lata 350ml', '7891000000019',
    3.00, 5.00, 100, 10, 'Un', 12, NULL, true, now()
FROM profiles p
JOIN auth.users u ON u.id = p.id
WHERE u.email = 'caixa01@emporiogege.com'
  AND p.tenant_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM produtos existente
      WHERE existente.tenant_id = p.tenant_id AND existente.codigo_barras = '7891000000019'
  );

UPDATE profiles p
SET tenant_id = (
    SELECT p2.tenant_id
    FROM profiles p2
    JOIN auth.users u2 ON u2.id = p2.id
    WHERE u2.email = 'caixa01@emporiogege.com'
)
WHERE p.role = 'superadmin' AND p.tenant_id IS NULL;
