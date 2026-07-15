SISTEMA ADEGA GG (EmporioGege) - RESUMO DO TRABALHO REALIZADO
================================================================
Este arquivo resume tudo que foi construído, testado e corrigido nas
sessões de desenvolvimento assistido. Serve como referência rápida do
estado do sistema e do histórico de decisões. Migrations descritas aqui
ficam nesta mesma pasta (Database/Migrations/).


1. ARQUITETURA
================================================================
Projeto único ASP.NET Core 10 / Razor Pages (EmporioGege/), organizado
em camadas por pasta (não por .csproj separado, para reduzir risco e
manter velocidade de entrega):

  Core/            Entidades, enums, interfaces, exceções de negócio.
                    Não depende de Infrastructure nem de nenhuma
                    biblioteca concreta de acesso a dados.
  Application/      Serviços de domínio (regra de negócio) e DTOs.
                    Depende só de abstrações do Core (IDbConnectionFactory,
                    ITenantProvider etc.), nunca de classes concretas
                    de Infrastructure.
  Infrastructure/    Implementações concretas: Npgsql/Dapper, contexto
                    de tenant (Claims), fila de webhook, processador
                    em background.
  Pages/            Razor Pages (Controllers + Views) e PageModels.
  Models/           Modelos mapeados ao cliente REST do Supabase
                    (Postgrest) - usados só no login e em telas que
                    ainda dependem do cliente Supabase diretamente.

Dois caminhos de acesso a dados, cada um com seu papel:
  - Dapper + Npgsql, conectando como o usuário 'postgres' (superuser):
    usado por TODA a lógica transacional (estoque, vendas, caixa,
    dashboard, cadastros). Sempre filtra tenant_id manualmente no
    código, porque essa conexão ignora RLS.
  - Cliente Supabase (Postgrest REST): usado só no login (autenticação
    e leitura do próprio perfil). NÃO é confiável para leituras fora
    do request de login, porque a sessão não é persistida entre
    requisições (ver bug #2 na seção 4).

Isolamento multi-tenant: todo dado tem tenant_id, e todo serviço aplica
o filtro explicitamente via ITenantProvider (lê o claim "TenantId" do
cookie de sessão, ou o AmbientTenantContext quando não há HttpContext,
como no processador de webhook).


2. MIGRATIONS (nesta pasta, todas já rodadas no Supabase)
================================================================
0001_precos_ledger_indices.sql
  - produtos: unidade_medida, quantidade_por_caixa (fator de conversão).
  - precos_produto: preço diferenciado por tipo (BALCAO/CAIXA/ATACADO).
  - caixa_ledger: ledger imutável do caixa (trigger bloqueia
    UPDATE/DELETE), com hash encadeado (SHA256).
  - Índices compostos (tenant_id, id) nas tabelas principais.

0002_turno_unico_aberto.sql
  - Índice único parcial: no máximo 1 turno ABERTO por usuário/tenant.

0003_webhooks_zedelivery.sql
  - integracoes_webhook: credenciais (token de URL + segredo HMAC) por
    tenant/canal de integração.
  - webhooks_recebidos: idempotência e auditoria de eventos recebidos
    (fila durável - fonte da verdade, não a fila em memória).

0004_rls_hardening.sql
  - Habilita RLS (sem policies) em precos_produto, caixa_ledger,
    integracoes_webhook, webhooks_recebidos - tabelas que só devem
    ser acessadas via Dapper (conexão admin), nunca via API REST
    pública do Supabase.

0005_vendas_diretas_por_produto.sql
  - vendas_itens.variacao_id passa a aceitar NULL e ganha produto_id
    (o PDV vende produto direto, nunca implementou variação).
  - vendas.turno_id passa a aceitar NULL (pedidos do Zé Delivery não
    têm turno de caixa físico associado).
  - Índice que faltava em vendas_itens.

0006_rls_profiles_produtos_precos.sql
  - CRÍTICO: profiles estava com RLS DESLIGADO - qualquer usuário
    autenticado podia ler/editar qualquer perfil via API REST,
    inclusive o próprio "role" (escalação de privilégio). Liga RLS +
    policy de leitura do próprio perfil.
  - produtos e precos_produto tinham RLS ligado mas ZERO policies
    (bloqueava tudo, inclusive o catálogo do PDV). Adiciona policy de
    leitura por tenant_id pro role authenticated.

0007_produtos_ativo.sql
  - produtos.ativo: permite desativar produto sem apagar (apagar
    quebraria o histórico de vendas, já que vendas_itens.produto_id
    referencia produtos sem cascade).

0008_comandas_itens.sql
  - comandas_itens: staging do consumo de uma comanda ABERTA (o que foi
    lançado, ainda sem baixar estoque nem virar venda).
  - comandas.atualizado_em: quando a comanda mudou de status.
  - Índice único parcial (tenant_id, numero_comanda) WHERE status =
    'ABERTA': no máximo 1 comanda aberta por número/tenant (mesmo
    padrão da 0002, mas por número em vez de por usuário).
  - RLS ligado em comandas_itens, sem policy (só acessada via Dapper).

0009_dados_teste_comandas.sql
  - Dados de apoio SÓ PRA AMBIENTE DE TESTE (não é schema): insere um
    produto de teste no tenant do usuário caixa01@emporiogege.com (pra
    dar pra testar adicionar-item/fechar-comanda) e vincula perfis
    superadmin sem tenant_id a esse mesmo tenant (só pra destravar
    telas Admin/* durante teste manual - ver nota de reversão no
    próprio arquivo da migration).
  - Credenciais de teste (caixa01, sem senha de superadmin) ficam em
    Database/Migrations/CREDENCIAIS_TESTE.local.txt - arquivo local,
    no .gitignore, NUNCA commitado. Peça a quem já rodou os testes.


3. FUNCIONALIDADES ENTREGUES
================================================================
Autenticação e autorização
  - Login via Supabase Auth, roles (vendedor/administrador/superadmin)
    mapeadas para Claims/Policies do ASP.NET Core.
  - Autorização por pasta (AuthorizeFolder) + atributo explícito em
    cada PageModel.

PDV (Pages/Caixa/Index)
  - Catálogo com busca (código de barras ou nome), atalhos de teclado
    F2 (Nota Fiscal/Controle Interno), F8 (focar busca), F10 (finalizar).
  - Preço por unidade ou caixa fechada (fator de conversão), com
    preços diferenciados configuráveis por produto.
  - Métodos de pagamento: Dinheiro, Débito, Crédito, Pix, Fiado.
  - Exige turno de caixa aberto para vender.
  - Venda é ATÔMICA: baixa de estoque de todos os itens + registro em
    vendas/vendas_itens + crédito no ledger (se dinheiro) ou no saldo
    devedor do cliente (se fiado) - tudo numa única transação
    SERIALIZABLE. Se qualquer item falhar, nada é gravado.

Turno de caixa (Pages/Caixa/Turno)
  - Abertura (saldo inicial) e fechamento (confere saldo sistema vs.
    informado, destaca quebra de caixa).
  - Sangria/suprimento gravados no ledger imutável.

Dashboard (Pages/Admin/Index)
  - Faturamento (hoje/mês), CMV, lucro bruto, ROI.
  - Alertas: estoque crítico, produtos próximos ao vencimento
    (vermelho se vencido ou <=7 dias, amarelo até 30 dias).
  - Fiado pendente, comandas ativas.

Cadastro de Produto (Pages/Admin/Estoque)
  - CRUD completo: nome, código de barras, custo, preço de venda,
    estoque atual/mínimo, unidade, fator de conversão, validade.
  - Preços diferenciados (caixa/atacado) direto no formulário.
  - Ativar/desativar em vez de excluir.

Cadastro de Funcionário (Pages/Admin/Funcionarios)
  - Cria conta de acesso (Supabase Auth) + perfil (nome, cargo).
  - Cargo restrito a vendedor/administrador (nunca superadmin, evita
    escalação de privilégio pela tela).

Cliente / Fiado (Pages/Admin/Clientes)
  - CRUD de cliente com limite de crédito.
  - Registrar pagamento (abate o saldo devedor).
  - Limite de crédito é checado atomicamente na mesma transação da
    venda fiado - nunca deixa o cliente passar do limite.

Comandas (Pages/Caixa/Comandas e Pages/Admin/Comandas)
  - Abertura por número/mesa (impede duplicar número enquanto ABERTA),
    consumo incremental (itens ficam em comandas_itens, sem baixar
    estoque ainda) e fechamento, que vira uma venda de verdade (baixa
    de estoque + vendas/vendas_itens + ledger/fiado) reaproveitando o
    mesmo VendaService atômico do PDV - a comanda é marcada FECHADA na
    MESMA transação da venda.
  - Cancelamento (comanda sem consumo, não vira venda).
  - Autorização de supervisor: vendedor só cancela comanda ou remove
    item já lançado com login de um administrador/superadmin digitado
    na hora (SupervisorAutorizacaoService, mesmo mecanismo do login,
    sem trocar a sessão do caixa). Administrador/superadmin fazem as
    duas ações livremente.
  - Admin/Comandas é supervisão: histórico completo (abertas, fechadas,
    canceladas), ver itens, e cancelar comanda travada aberta sem
    precisar de senha (a policy AdminOnly já garante quem chega lá).

Cadastro de Lojas / Gestão de Contexto (Pages/SuperAdmin/Adegas)
  - CRUD de tenant (loja): nome fantasia, responsável, CNPJ, contatos,
    status de licença, data de expiração. Antes só dava pra criar via
    SQL direto.
  - "Entrar" numa loja: grava as claims ImpersonatedTenantId/Nome no
    próprio cookie do superadmin (SignInAsync de novo, mesmo scheme -
    não troca de usuário nem afeta sessão de mais ninguém) e passa a
    resolver TenantId por essa claim (HttpContextTenantProvider,
    checada antes da claim TenantId normal). Isso destrava todas as
    telas Admin/*/Caixa/* pra aquele tenant específico sem precisar de
    conta administrador nele - inclusive dá pra criar o primeiro
    administrador de uma loja nova via Admin/Funcionarios normalmente,
    já "dentro" do contexto dela.
  - Banner "Modo Loja: {nome}" fixo no topo (_Layout.cshtml) enquanto
    impersonando, com botão "Sair do modo loja" que reemite o cookie
    sem as claims - funciona de qualquer página do sistema.
  - Segurança: só quem já passou pela policy SuperAdminOnly consegue
    gravar essa claim (o handler que assina o cookie é gated por ela);
    administrador/vendedor não tem como forjar acesso a outro tenant.

Recibo impresso (impressora térmica Bluetooth, ESC/POS)
  - Impressão automática ao finalizar venda de balcão (Caixa/Index) ou
    fechar comanda (Caixa/Comandas): nome da loja, itens, total e
    forma de pagamento. Testado ao vivo numa impressora térmica
    Bluetooth (Leopardo, identificada pelo Windows como "MPT-III"),
    pareada como porta serial virtual (Bluetooth SPP) e comandada via
    System.IO.Ports.SerialPort com comandos ESC/POS crus (ESC @, ESC
    E, ESC a) - IImpressoraReciboService/SerialPortImpressoraReciboService
    em Infrastructure/Impressao.
  - Configuração por máquina física (seção "Impressora" em
    appsettings.json/user-secrets, igual à connection string) - cada
    PC de caixa tem sua própria impressora pareada, possivelmente numa
    porta COM diferente. Vem desligada por padrão (Habilitada: false)
    no repo.
  - Preço/quantidade impressos vêm do valor REALMENTE aplicado na
    transação da venda (ResultadoVendaDto.Itens), não de uma releitura
    do catálogo depois do fato.
  - Falha de impressão (impressora desligada, porta errada, sem
    papel) NUNCA bloqueia a venda - já está tudo gravado no banco
    antes da tentativa; só loga um warning e devolve
    `reciboImpresso: false` na resposta, que a tela mostra como aviso.
  - Sem corte automático de papel (a MPT-III não tem guilhotina) e sem
    botão de reimpressão manual nesta rodada.

Integração Zé Delivery (webhook)
  - Endpoint público POST /webhooks/zedelivery/{token}, autenticado
    por token de URL + assinatura HMAC-SHA256 do corpo.
  - Fila durável (tabela + canal em memória como sinal de wake-up) +
    processamento em BackgroundService, com recuperação em caso de
    reinício do processo.
  - Rate limiting por token.
  - IMPORTANTE: a Zé Delivery não expõe publicamente o formato exato
    do payload/assinatura sem aprovação de parceiro. O adaptador está
    pronto mas com suposições marcadas no código (ver
    HmacSignatureValidator e ZeDeliveryWebhookPayload) - ajustar
    quando houver acesso real à documentação de parceiro.


4. BUGS CRÍTICOS ENCONTRADOS E CORRIGIDOS
================================================================
Encontrados sobretudo testando os fluxos ao vivo (login real, HTTP,
conferência direto no banco) - nenhum aparecia só com "dotnet build".

#1 - BackgroundService podia derrubar a aplicação inteira
  Uma falha transitória processando UM webhook (ex.: banco
  temporariamente indisponível) propagava e derrubava o host inteiro
  (login, PDV, tudo), por padrão do .NET. Corrigido com rede de
  segurança que nunca deixa exceção escapar do processador.

#2 - Cliente Supabase (Postgrest) sem sessão entre requisições
  Supabase.Client é registrado Scoped (uma instância nova por
  requisição) e a sessão de login nunca era persistida. Toda leitura
  via esse cliente FORA do próprio request de login rodava como
  usuário anônimo - o catálogo do PDV e a busca de SKU do webhook
  sempre voltavam vazios. Corrigido migrando essas leituras para
  Dapper (mesma conexão admin usada em todo o resto do sistema).

#3 - RLS ausente/mal configurado (ver migrations 0004 e 0006)
  profiles com RLS desligado (risco de escalação de privilégio via
  API REST pública); produtos e precos_produto com RLS ligado mas
  zero policies (bloqueava o catálogo do PDV).

#4 - XSS armazenado no PDV
  Nome/código de barras de produto (texto livre) iam sem escape para
  innerHTML no carrinho/sugestões/toasts. Corrigido com escape
  explícito de HTML antes de qualquer interpolação.

#5 - Vazamento de stack trace no webhook
  Endpoint público sem tratamento de exceção vazava stack trace
  completo (com caminho de arquivo do servidor) para qualquer
  chamador não autenticado em caso de erro. Corrigido.

#6 - Venda nunca gravada (gap estrutural da Fase 2 original)
  O PDV só baixava estoque; nunca criava registro em vendas/
  vendas_itens nem creditava o ledger. Dashboard sempre mostraria
  R$ 0,00 de faturamento e o fechamento de turno nunca refletiria
  vendas reais. Corrigido com o VendaService atômico.

#7 - Application acoplado a classe concreta de Infrastructure
  Todo serviço dependia de NpgsqlConnectionFactory (classe concreta)
  em vez de uma abstração do Core. Corrigido com IDbConnectionFactory.

#8 - Dapper x System.DateOnly
  O Npgsql atual mapeia coluna "date" do Postgres para DateOnly, não
  DateTime - quebrava a listagem de produtos com validade. E o Dapper
  (biblioteca) não sabe nativamente usar DateOnly como parâmetro de
  query - quebrava salvar um produto com validade. Dois bugs
  distintos, corrigidos com DTOs em DateOnly + type handler customizado
  registrado no startup (Infrastructure/Data/DateOnlyTypeHandler.cs).

#9 - Dapper x COUNT(*) (bigint vs int)
  COUNT(*) no Postgres retorna bigint; os DTOs esperavam int. Quebrava
  o dashboard inteiro (silenciosamente - o erro nem estava sendo
  logado). Corrigido com cast ::int no SQL + adicionado log de erro
  que faltava no dashboard.

#10 - CRÍTICO: todo valor em dinheiro do sistema podia ser salvo 100x
      maior por causa da cultura do servidor
  A máquina roda em cultura pt-BR ("," decimal, "." milhar). Um
  <input type="number"> de navegador SEMPRE envia formato "20.00"
  (ponto), independente do idioma - e o ASP.NET Core usa a cultura
  ambiente pra interpretar decimal em formulário. Resultado: um limite
  de crédito digitado como 20.00 foi salvo como 2000,00. Isso afetava
  TODO campo de dinheiro do sistema (preço de produto, custo, saldo de
  turno, sangria/suprimento, limite de crédito, pagamento de fiado).
  Corrigido fixando a cultura de parsing em invariant
  (Program.cs -> UseRequestLocalization) e tornando a exibição de
  moeda explícita em pt-BR em todas as telas (antes dependia da
  cultura ambiente "por acaso" bater com o formato brasileiro).

  RECOMENDAÇÃO: se algum dado foi cadastrado ANTES desta correção
  (qualquer produto/cliente/turno criado ou editado via formulário
  antes desta sessão), vale conferir manualmente se os valores em
  dinheiro não ficaram 100x maiores por esse bug.


5. O QUE AINDA NÃO EXISTE (conhecido, não é bug)
================================================================
- NFC-e real: hoje só existe um flag "emitir_nota_fiscal" (marcação
  interna). Não emite nota fiscal de verdade - falta escolher um
  provedor (Focus NFe / eNotas / PlugNotas) e integrar.
- Bloqueio de login por status_licenca/data_expiracao do tenant: os
  campos existem e são editáveis em SuperAdmin/Adegas, mas nada ainda
  impede login de um tenant "suspenso"/"cancelado"/expirado.
- Testes automatizados (nenhum criado até agora).
- Proteção contra força bruta no login.
- Integração de pagamento (maquininha, QR code Pix) - os métodos de
  pagamento no PDV são só rótulos hoje.


6. COMO RODAR AS MIGRATIONS
================================================================
Todas as migrations desta pasta são idempotentes (usam IF NOT EXISTS /
DROP + CREATE) - seguro rodar de novo se precisar. Rodar manualmente
no SQL Editor do Supabase, em ordem numérica (0001 até a mais recente).
Nenhuma é executada automaticamente pela aplicação.
