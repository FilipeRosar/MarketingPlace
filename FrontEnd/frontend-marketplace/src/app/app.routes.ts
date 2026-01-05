import { Routes } from '@angular/router';
import { HomeComponent } from '../pages/home/home.component';
import { CheckoutComponent } from '../pages/checkout/checkout/checkout.component';
import { ProductDetailComponent } from '../pages/product-detail/product-detail/product-detail.component';
import { LoginComponent } from '../pages/login/login/login.component';
import { RegisterCustomerComponent } from '../pages/register-customer/register-customer/register-customer.component';
import { RegisterSellerComponent } from '../pages/register-seller/register-seller/register-seller.component';
import { CartComponent } from '../pages/cart/cart/cart.component';
import { SellerProfileComponent } from '../pages/profile/sellerProfile/seller-profile.component';
import { OrdersComponent } from '../pages/order/orders.component';
import { AddProductComponent } from '../components/add-product/add-product.component';
import { SellerDashboardComponent } from '../components/seller-dashboard/seller-dashboard.component';
import { ProfileComponent } from '../pages/profile/profile/profile.component';
import { ForgotPasswordComponent } from '../pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from '../pages/reset-password/reset-password.component';
import { AdminDashboardComponent } from '../pages/admin-dashboard/admin-dashboard.component';
import { FavoritesComponent } from '../components/favorite/favorite.component';
import { adminGuard } from '../guards/admin.guard';
import { authGuard } from '../guards/auth-guard';
import { sellerGuard } from '../guards/seller-guard';
import { PendingApprovalComponent } from '../components/modal/pending-approval/pending-approval.component';

export const routes: Routes = [
   { path: '', component: HomeComponent },
  { path: 'products/:id', component: ProductDetailComponent },
  { path: 'sellers/:id', component: SellerProfileComponent },
  { path: 'categorias',loadComponent: () => import('../pages/categories-page/categories-page.component').then(m => m.CategoriesPageComponent) },
  { path: 'categorias/:slug', loadComponent: () => import('../pages/categories-page/categories-page.component').then(m => m.CategoriesPageComponent) },
  {
    path: 'pending-approval',
    loadComponent: () => import('../components/modal/pending-approval/pending-approval.component')
      .then(m => m.PendingApprovalComponent)
  },

  // Auth
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterCustomerComponent },
  { path: 'register-seller', component: RegisterSellerComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  {
  path: 'admin',
  canActivate: [adminGuard],
  children: [
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    { path: 'dashboard', loadComponent: () => import('../pages/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
    { path: 'orders', loadComponent: () => import('../pages/order/orders.component').then(m => m.OrdersComponent) },
  ]
  },
  // Privadas
  { path: 'cart', component: CartComponent },
  { path: 'checkout', component: CheckoutComponent },
  {
  path: 'orders',
  component: OrdersComponent,
  canActivate: [authGuard],
  data: { title: 'orders' }
  },
  { path: 'profile', component: ProfileComponent },
  {
    path: 'add-product',
    component: AddProductComponent,
    canActivate: [sellerGuard]
  },
  {
    path: 'seller-dashboard',
    component: SellerDashboardComponent,
    canActivate: [sellerGuard]
  },
  {
    path: 'seller-profile/:id',
    component: SellerProfileComponent,
    canActivate: [sellerGuard]
  },
  {
  path: 'seller-profile/by-user/:id',
  component: SellerProfileComponent
  },
  { path: 'favorites', component: FavoritesComponent },
  { path: '**', redirectTo: '' }
];
