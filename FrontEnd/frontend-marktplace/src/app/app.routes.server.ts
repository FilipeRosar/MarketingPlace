import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Rotas dinâmicas: Client Side (Navegador busca dados)
  {
    path: 'products/:id',
     renderMode: RenderMode.Server
  },
  {
    path: 'sellers/:id',
     renderMode: RenderMode.Server
  },
  {
    path: 'categorias/:slug',
     renderMode: RenderMode.Server
  },
  {
    path: 'categorias',
     renderMode: RenderMode.Server
  },

  {
    path: 'checkout',
     renderMode: RenderMode.Server
  },
  {
    path: 'cart',
     renderMode: RenderMode.Server
  },
  {
    path: 'orders',
     renderMode: RenderMode.Server
  },
  {
    path: 'profile',
    renderMode: RenderMode.Server
  },
  {
    path: 'seller-dashboard',
    renderMode: RenderMode.Server
  },
  {
    path: 'add-product',
     renderMode: RenderMode.Server
  },
  {
    path: 'favorites',
     renderMode: RenderMode.Server
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
