import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'products/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'sellers/:id',
    renderMode: RenderMode.Server
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
    path: 'seller-dashboard',
    renderMode: RenderMode.Client
  },
  {
    path: 'add-product',
    renderMode: RenderMode.Client
  },

  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
