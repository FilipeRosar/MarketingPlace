import { Routes } from '@angular/router';
import { HomeComponent } from '../pages/home/home.component';
import { CheckoutComponent } from '../pages/checkout/checkout/checkout.component';
import { ProductDetailComponent } from '../pages/product-detail/product-detail/product-detail.component';
import { LoginComponent } from '../pages/login/login/login.component';
import { RegisterCustomerComponent } from '../pages/register-customer/register-customer/register-customer.component';
import { RegisterSellerComponent } from '../pages/register-seller/register-seller/register-seller.component';
import { CartComponent } from '../pages/cart/cart/cart.component';
import { SellerProfileComponent } from '../pages/profile/sellerProfile/seller-profile.component';
import { OrdersComponent } from '../pages/order/order.component';
import { AddProductComponent } from '../components/add-product/add-product.component';
import { SellerDashboardComponent } from '../components/seller-dashboard/seller-dashboard.component';

export const routes: Routes = [
  // Públicas
  { path: '', component: HomeComponent },
  { path: 'products/:id', component: ProductDetailComponent },
  { path: 'sellers/:id', component: SellerProfileComponent },

  // Auth
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterCustomerComponent },
  { path: 'register-seller', component: RegisterSellerComponent },

  // Privadas (Cliente)
  { path: 'cart', component: CartComponent },
  { path: 'checkout', component: CheckoutComponent },
  { path: 'orders', component: OrdersComponent },

  // Privadas (Vendedor)
  { path: 'add-product', component: AddProductComponent },
  { path: 'seller-dashboard', component: SellerDashboardComponent },

  // Fallback
  { path: '**', redirectTo: '' }
];
