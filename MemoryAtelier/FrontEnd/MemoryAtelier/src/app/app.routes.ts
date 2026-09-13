import { Routes } from '@angular/router';
import { authGuard } from './services/AuthGuard/auth-guard-guard';
import { adminGuard } from './services/admin/admin';
import { Navbar } from './layout/navbar/navbar';
import { SignIn } from './pages/sign-in/sign-in';
import { Signup } from './pages/signup/signup';
import { Cart } from './pages/cart/cart/cart';
import { Favourites } from './pages/favourites/favourites';
import { ProductDetail } from './pages/product-detail/product-detail';
import { Checkout } from './pages/checkout/checkout';


export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'sign-in', component: SignIn },
  { path: 'signup', component: Signup },
  {
    path: '',
    component: Navbar,
    children: [
      { path: 'home', loadComponent: () => import('./pages/home-page/home-page').then(m => m.HomePage) },
      { path: 'product/:id/:slug', loadComponent: () => import('./pages/product-detail/product-detail').then(m => m.ProductDetail) },
      { path: 'product/:id', loadComponent: () => import('./pages/product-detail/product-detail').then(m => m.ProductDetail) },
      { path: 'admin', loadComponent: () => import('./pages/admin/admin').then(m => m.Admin), canActivate: [authGuard, adminGuard] },
      { path: 'cart', component: Cart, canActivate: [authGuard] },
      { path: 'favourites', component: Favourites, canActivate: [authGuard] },
      { path: 'checkout', component: Checkout, canActivate: [authGuard] },
      { path: 'contact', loadComponent: () => import('./pages/contact/contact').then(m => m.Contact) },
      { path: 'sitemap', loadComponent: () => import('./pages/sitemap/sitemap').then(m => m.Sitemap) },
      { path: 'terms', loadComponent: () => import('./pages/terms/terms').then(m => m.Terms) }
    ]
  },
  { path: '**', redirectTo: 'home' } // последен
];