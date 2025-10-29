

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
- **CI/CD:** Azure DevOps / GitHub Actions

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




## Documentação da API

#### Retorna todos os itens

```http
  GET /api/items
```

| Parâmetro   | Tipo       | Descrição                           |
| :---------- | :--------- | :---------------------------------- |
| `api_key` | `string` | **Obrigatório**. A chave da sua API |

#### Retorna um item

```http
  GET /api/items/${id}
```

| Parâmetro   | Tipo       | Descrição                                   |
| :---------- | :--------- | :------------------------------------------ |
| `id`      | `string` | **Obrigatório**. O ID do item que você quer |

#### add(num1, num2)

Recebe dois números e retorna a sua soma.


## Referência

 - [Awesome Readme Templates](https://awesomeopensource.com/project/elangosundar/awesome-README-templates)
 - [Awesome README](https://github.com/matiassingers/awesome-readme)
 - [How to write a Good readme](https://bulldogjob.com/news/449-how-to-write-a-good-readme-for-your-github-project)


## 🚀 Sobre mim
Eu sou uma pessoa desenvolvedora full-stack...

