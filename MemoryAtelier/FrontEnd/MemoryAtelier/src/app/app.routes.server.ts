import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Изискват логин/потребителски данни — няма смисъл да се prerender-ват или SSR-ват.
  { path: 'sign-in', renderMode: RenderMode.Client },
  { path: 'signup', renderMode: RenderMode.Client },
  { path: 'admin', renderMode: RenderMode.Client },
  { path: 'cart', renderMode: RenderMode.Client },
  { path: 'favourites', renderMode: RenderMode.Client },
  { path: 'checkout', renderMode: RenderMode.Client },
  { path: 'profile', renderMode: RenderMode.Client },
  // Продуктовите данни се сменят без ребилд — рендират се на сървъра при всяка заявка.
  { path: 'product/:id', renderMode: RenderMode.Server },
  { path: 'product/:id/:slug', renderMode: RenderMode.Server },
  // Статично съдържание — prerender-ва се веднъж при build.
  {
    path: '**',
    renderMode: RenderMode.Prerender,
  },
];
