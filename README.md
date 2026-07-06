# 🍷 Empório Gege - Sistema de Gestão

Um sistema completo de Frente de Caixa (PDV) e Retaguarda (ERP) focado na operação ágil de uma adega. Desenvolvido para automatizar os processos do dia a dia, desde a venda rápida no balcão até o controle detalhado do estoque e a gestão do fiado.

## 🎯 Escopo e Funcionalidades

A aplicação foi desenhada com módulos independentes, garantindo que a operação de venda seja rápida e a gestão gerencial seja segura:

* **Frente de Caixa (PDV):** Interface otimizada para o operador. Suporta vendas diretas no balcão, gerenciamento de comandas abertas e registro de métodos de pagamento diversos (incluindo atalhos para apps de delivery).
* **Controle Financeiro e Caixa Cego:** Sistema de fechamento por contagem às cegas. O operador informa o valor em dinheiro físico sem ver o saldo esperado pelo sistema, garantindo auditoria real. Registro rigoroso de sangrias e suprimentos.
* **Estoque Inteligente com Fator de Conversão:** O coração do estoque. Os produtos são cadastrados em sua unidade mínima (ex: lata) e vendidos através de variações (ex: fardo com 12). A venda de um fardo abate automaticamente as latas do estoque. Todo movimento gera um registro de Kardex (auditoria).
* **Gestão de Carteira (Fiado):** Controle de clientes com limite de crédito pré-aprovado. Acompanhamento de saldo devedor, extrato detalhado por cliente e baixa parcial ou total de dívidas direto no caixa.
* **Dashboard Administrativo:** Visão estratégica para o administrador, reunindo dados de vendas, alertas de estoque mínimo e acompanhamento de produtos próximos do vencimento.

## 💻 Stack Tecnológico

O projeto foi construído utilizando as seguintes tecnologias:

* **Linguagem & Framework:** C# 10 rodando sobre ASP.NET Core (Razor Pages).
* **Banco de Dados & Auth:** Supabase (PostgreSQL com UUIDs nativos e Supabase Auth para controle de acesso).
* **Front-end:** HTML5, CSS3, e Bootstrap 5 para uma interface limpa e responsiva.

## 🔒 Status do Repositório

Este é um repositório **privado** de caráter proprietário. 

O código-fonte, a modelagem de banco de dados e as regras de negócio estruturadas aqui são de uso exclusivo. Não há licença pública de código aberto associada a este projeto, sendo proibida a cópia, distribuição, modificação ou uso comercial por terceiros não autorizados. Desenvolvido por Vinnicius Gabriel.
