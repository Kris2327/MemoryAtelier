import { Pipe, PipeTransform } from '@angular/core';

const OBJECT_MARKER = '/storage/v1/object/public/';
const RENDER_MARKER = '/storage/v1/render/image/public/';

/**
 * Пренасочва Supabase Storage публичен обект URL към техния image-transformation endpoint,
 * за да не тегли браузърът пълноразмерния оригинал (някои по-стари качвания са по 3-5 MB)
 * за слот, който реално е няколкостотин пиксела широк.
 * Ако URL-ът не е Supabase storage обект (напр. placehold.co резервен вариант), връща го непроменен.
 */
export function resizedImageUrl(url: string, width: number, quality = 75): string {
  const idx = url.indexOf(OBJECT_MARKER);
  if (idx === -1) return url;

  const transformed = url.slice(0, idx) + RENDER_MARKER + url.slice(idx + OBJECT_MARKER.length);
  const sep = transformed.includes('?') ? '&' : '?';
  return `${transformed}${sep}width=${width}&quality=${quality}&resize=cover`;
}

/** Обратната трансформация — ползва се като (error) fallback, ако transformation endpoint-ът не е наличен. */
export function originalImageUrl(possiblyResizedUrl: string): string {
  const idx = possiblyResizedUrl.indexOf(RENDER_MARKER);
  if (idx === -1) return possiblyResizedUrl;

  const withoutQuery = possiblyResizedUrl.split('?')[0];
  return withoutQuery.slice(0, idx) + OBJECT_MARKER + withoutQuery.slice(idx + RENDER_MARKER.length);
}

@Pipe({ name: 'resizeImage', standalone: true })
export class ResizeImagePipe implements PipeTransform {
  transform(url: string | null | undefined, width: number, quality = 75): string {
    if (!url) return url ?? '';
    return resizedImageUrl(url, width, quality);
  }
}
