-- =============================================================================
-- Migration 0011: dados fiscais da loja (tenants) pra emissão de NFC-e via
-- Focus NFe, e classificação fiscal opcional do produto (NCM/CFOP).
--
-- tenants ganha os campos que a Focus NFe exige no cadastro de empresa
-- (POST /v2/empresas): razão social, regime tributário, endereço estruturado,
-- inscrição estadual, CSC (só usado em produção, não em homologação). Todos
-- nullable de propósito - lojas já cadastradas antes desta migration não têm
-- esses dados, e ativação de NFC-e é opcional/posterior ao cadastro da loja.
--
-- focus_nfe_token_*_cifrado guardam os DOIS tokens que a Focus NFe devolve
-- ao criar a empresa (um por ambiente - produção e homologação, cada emissão
-- de nota usa o token do ambiente correspondente), diferentes do token de
-- conta usado só pra criar/consultar empresas - cifrados em repouso pela
-- aplicação (Core/Security/CriptografiaSimetrica), nunca em texto plano.
-- O CERTIFICADO digital (.pfx + senha) em si NUNCA é
-- persistido aqui nem em nenhuma outra tabela - vai direto da tela pro Focus
-- NFe (que passa a guardá-lo) e é descartado da memória logo depois da
-- chamada, por decisão explícita de produto (menor superfície de risco).
--
-- Idempotente. Rodar manualmente no SQL Editor do Supabase.
-- =============================================================================
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS razao_social text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS regime_tributario smallint; -- 1=Simples Nacional, 2=Simples excesso, 3=Normal, 4=MEI (valores da Focus NFe)
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS inscricao_estadual text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS logradouro text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS numero text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS bairro text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS municipio text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS uf text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS cep text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS csc_nfce_producao text; -- só produção; homologação da Focus NFe não exige CSC
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS id_token_nfce_producao text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS focus_nfe_empresa_id text; -- id interno da empresa no Focus NFe (consulta/atualização futura)
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS focus_nfe_token_producao_cifrado text; -- token da EMPRESA (não o de conta), cifrado (AES-GCM) pela aplicação
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS focus_nfe_token_homologacao_cifrado text;
ALTER TABLE tenants ADD COLUMN IF NOT EXISTS nfce_habilitada boolean NOT NULL DEFAULT false;

-- Classificação fiscal do produto, exigida pela Focus NFe item a item na
-- emissão da NFC-e (codigo_ncm, cfop). Nullable/opcional de propósito: é
-- conhecimento tributário real (varia por produto), não algo que a aplicação
-- possa inferir sozinha - fica em branco até alguém (dono da loja ou
-- contador) preencher pelo cadastro de produto.
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS codigo_ncm text;
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS cfop text;
