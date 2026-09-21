import { Pipe, PipeTransform } from '@angular/core';

const OBJECT_MARKER = '/storage/v1/object/public/';

/**
 * Извежда URL-а на "-thumb" companion версията, която бекендът генерира до всяка Supabase snимка
 * (виж SupabaseStorageService.ToThumbnailPath) — Supabase-ият image-transformation endpoint не е наличен
 * за проекта, затова смалените варианти се пазят като отделни статични файлове до оригинала.
 * Ако URL-ът не е Supabase storage обект (напр. placehold.co резервен вариант), връща го непроменен.
 */
export function thumbnailUrl(url: string): string {
  const markerIdx = url.indexOf(OBJECT_MARKER);
  if (markerIdx === -1) return url;

  const dot = url.lastIndexOf('.');
  const slash = url.lastIndexOf('/');
  if (dot === -1 || dot < slash) return url;

  return url.slice(0, dot) + '-thumb' + url.slice(dot);
}

@Pipe({ name: 'thumbUrl', standalone: true })
export class ThumbUrlPipe implements PipeTransform {
  transform(url: string | null | undefined): string {
    return url ? thumbnailUrl(url) : (url ?? '');
  }
}
