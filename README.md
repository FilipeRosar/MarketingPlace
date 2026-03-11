

# 🛍️ MarketingPlace Artesanato

Um marketplace completo desenvolvido para conectar vendedores e compradores de forma moderna, escalável e segura.

## 🚀 Tecnologias Utilizadas

### Back-End
- **.NET 9 (C#)** com **Entity Framework**
- **Autenticação JWT**
- **Cache:** Redis  
- **Armazenamento:** Azure Blob Storage  
- **Banco de Dados:** SQL Server / PostgreSQL  
- **Pagamentos:** Stripe  

### Front-End
- **Angular 20**
- **Testes:** Jasmine / Karma

### Testes & Integração Contínua
- **xUnit** (Back-End)
- **CI/CD:** GitHub Actions

---

## 🧩 Funcionalidades Principais

- Cadastro e autenticação de usuários (JWT)
- Listagem e gerenciamento de produtos
- Carrinho de compras e pagamentos integrados via Stripe
- Painel do vendedor para acompanhamento de vendas
- Painel administrativo para controle de produtos e usuários
- Armazenamento seguro de imagens no Azure Blob
- Cache de dados com Redis para alta performance
- **✨ Dashboard de Cupons para Sellers** com automação inteligente e analytics de ROI

---

## 💎 Dashboard de Cupons (Phase 2 - COMPLETO ✅)

Um sistema completo de gerenciamento de cupons para sellers com:

### 📝 Nota sobre API
- URL da API Backend: `http://localhost:5253/api`
- Componentes estão configurados para rodar com essa base URL
- Ver [BUGFIX_URL_DUPLICATE.md](./BUGFIX_URL_DUPLICATE.md) para detalhes de correção de roteamento

### Funcionalidades Backend
- **Serviço de Analytics**: Cálculo de ROI, conversão e impacto em vendas
- **Automação Inteligente (CronJob)**:
  - Desativar cupons expirados automaticamente
  - Aplicar limite de uso automático
  - Ativar cupons sazonais em datas específicas
- **APIs REST** para gerenciamento de cupons e analytics
- **Testes automatizados** com xUnit (12 testes, 100% passing)

### Funcionalidades Frontend
- **Tabela de Cupons** com filtros e ações (editar, clonar, deletar, analytics)
- **Formulário de Criação/Edição** com validação completa
- **Gerador de Códigos** aleatórios com prefixo customizável
- **Dashboard de Analytics**:
  - Cards com resumo (Total Economizado, Cupons Ativos, ROI Médio, Taxa de Conversão)
  - Top 5 performers por ROI
  - Bottom 5 para oportunidades de melhoria
  - Trend mensal
- **Testes Jasmine** para todos os componentes (62 testes)

### Estrutura de Componentes
```
src/components/
├── seller-coupon-table/          # Listagem e filtros
├── seller-coupon-form/           # Create/Edit com validação
├── coupon-code-generator/        # Gerador de códigos
├── seller-coupon-analytics/      # Dashboard de metrics
└── seller-coupon-management/     # Hub central com tabs
```

**Documentação Detalhada**: Veja [`PHASE_2_IMPLEMENTATION_SUMMARY.md`](./PHASE_2_IMPLEMENTATION_SUMMARY.md)

---

## 🧱 Arquitetura

O projeto segue uma arquitetura modular dividida em **camadas independentes**, favorecendo escalabilidade e manutenção:



