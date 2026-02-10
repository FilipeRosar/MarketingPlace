

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

---

## 🧱 Arquitetura

O projeto segue uma arquitetura modular dividida em **camadas independentes**, favorecendo escalabilidade e manutenção:




---

## Stripe Webhook (local dev)

When running the API via Docker and using Stripe CLI, keep the listener running and update the webhook secret in `.env`.

1) Start the listener (keep this terminal open):
```
stripe listen --forward-to http://localhost:5253/api/webhook
```

2) Update the secret shown by the CLI in `.env`:
```
STRIPE_WEBHOOK_SECRET=whsec_...
```

3) Restart the backend container to reload the `.env`:
```
docker compose restart backend
```

4) (Optional) Send a test event:
```
stripe trigger checkout.session.completed
```
