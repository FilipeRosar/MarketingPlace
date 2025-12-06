import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Rotas dinâmicas: Client Side (Navegador busca dados)
  {
    path: 'products/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'sellers/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'categorias/:slug',
    renderMode: RenderMode.Client
  },
  {
    path: 'categorias',
    renderMode: RenderMode.Client
  },

  {
    path: 'checkout',
    renderMode: RenderMode.Client
  },
  {
    path: 'cart',
    renderMode: RenderMode.Client
  },
  {
    path: 'orders',
    renderMode: RenderMode.Client
  },
  {
    path: 'profile',
    renderMode: RenderMode.Client
  },
  {
    path: 'seller-dashboard',
    renderMode: RenderMode.Client
  },
  {
    path: 'add-product',
    renderMode: RenderMode.Client
  },
  {
    path: 'favorites',
    renderMode: RenderMode.Client
  },
  {
    path: 'admin',
    renderMode: RenderMode.Client
  },

  {
    path: '**',
    renderMode: RenderMode.Client
  }
];
