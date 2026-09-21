import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import compression from 'compression';
import express from 'express';
import { join } from 'node:path';
import { environment } from './environments/environment';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

// Gzip/Brotli компресия на SSR HTML-а (може да е 1MB+ при много продукти) и на статичните файлове.
app.use(compression());

// Backend-ът сервира /sitemap.xml на своя корен (виж SitemapController).
// Reverse proxy-то пред memoryatelier.bg рутира всичко към този Angular SSR
// сървър, така че проксираме заявката към backend-а тук вместо да разчитаме
// на инфра-ниво рутиране на /sitemap.xml.
const backendOrigin = environment.apiUrl.replace(/\/api\/?$/, '');

app.get('/sitemap.xml', async (req, res) => {
  try {
    const upstream = await fetch(`${backendOrigin}/sitemap.xml`);
    if (!upstream.ok) {
      res.status(upstream.status).end();
      return;
    }
    const xml = await upstream.text();
    res.type('application/xml').send(xml);
  } catch (err) {
    console.error('Failed to fetch /sitemap.xml from backend', err);
    res.status(502).end();
  }
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) => (response ? writeResponseToNodeResponse(response, res) : next()))
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
