
export default {
  bootstrap: () => import('./main.server.mjs').then(m => m.default),
  inlineCriticalCss: true,
  baseHref: '/MarketingPlace/',
  locale: undefined,
  routes: [
  {
    "renderMode": 2,
    "route": "/MarketingPlace"
  },
  {
    "renderMode": 0,
    "route": "/MarketingPlace/products/*"
  },
  {
    "renderMode": 0,
    "route": "/MarketingPlace/sellers/*"
  },
  {
    "renderMode": 2,
    "route": "/MarketingPlace/login"
  },
  {
    "renderMode": 2,
    "route": "/MarketingPlace/register"
  },
  {
    "renderMode": 2,
    "route": "/MarketingPlace/register-seller"
  },
  {
    "renderMode": 0,
    "route": "/MarketingPlace/forgot-password"
  },
  {
    "renderMode": 0,
    "route": "/MarketingPlace/reset-password"
  },
  {
    "renderMode": 1,
    "route": "/MarketingPlace/cart"
  },
  {
    "renderMode": 1,
    "route": "/MarketingPlace/checkout"
  },
  {
    "renderMode": 1,
    "route": "/MarketingPlace/orders"
  },
  {
    "renderMode": 0,
    "route": "/MarketingPlace/profile"
  },
  {
    "renderMode": 1,
    "route": "/MarketingPlace/add-product"
  },
  {
    "renderMode": 1,
    "route": "/MarketingPlace/seller-dashboard"
  },
  {
    "renderMode": 2,
    "redirectTo": "/MarketingPlace",
    "route": "/MarketingPlace/**"
  }
],
  entryPointToBrowserMapping: undefined,
  assets: {
    'index.csr.html': {size: 15290, hash: '632c8adda3646a26553c66c5ba1eb41afd9789a92b1b86d1f50e9f0cfd6196f8', text: () => import('./assets-chunks/index_csr_html.mjs').then(m => m.default)},
    'index.server.html': {size: 15692, hash: '84eb1f343ee445e8bd64d1f789c9636c104b6e13b6dbc75f302e670c36207d53', text: () => import('./assets-chunks/index_server_html.mjs').then(m => m.default)},
    'login/index.html': {size: 34886, hash: 'd9beb3df23dfbe24b25d6719a8f2c072d634541e844252fdf9631175bab7f712', text: () => import('./assets-chunks/login_index_html.mjs').then(m => m.default)},
    'register-seller/index.html': {size: 38274, hash: '4ea5d92e2bce3ad4a801a32d66e4db29081fc7e389808c6462b30f58e163405c', text: () => import('./assets-chunks/register-seller_index_html.mjs').then(m => m.default)},
    'index.html': {size: 42512, hash: '499ca0a6f04d38527711b5cc5a715beeeb53944c0c6f3655374cc4bbd2b82a13', text: () => import('./assets-chunks/index_html.mjs').then(m => m.default)},
    'register/index.html': {size: 38374, hash: '156b02ae45da4fa72845643bb8f9934aad9425cfe3da64e5c79780c985e859f3', text: () => import('./assets-chunks/register_index_html.mjs').then(m => m.default)},
    'styles-2ILSKFHU.css': {size: 12928, hash: '3TTRHl3FEr0', text: () => import('./assets-chunks/styles-2ILSKFHU_css.mjs').then(m => m.default)}
  },
};
