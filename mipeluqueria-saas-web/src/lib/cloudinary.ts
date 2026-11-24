import { toast } from 'sonner';

export interface CloudinaryUploadResult {
  url: string;
  publicId?: string;
  isFallback: boolean;
  warningMessage?: string;
}

export interface CloudinaryUploadOptions {
  fallbackUrl?: string;
  folder?: string;
  silentWarning?: boolean;
}

interface CloudinaryApiSuccessResponse {
  secure_url?: string;
  url?: string;
  public_id?: string;
  error?: {
    message?: string;
  };
}

export const DEFAULT_FICHA_TECNICA_PLACEHOLDER_URL =
  'https://placehold.co/800x600/f3e8ff/6b21a8?text=Ficha+Tecnica+MiPeluqueria';

const ALLOWED_IMAGE_MIME_TYPES: readonly string[] = [
  'image/jpeg',
  'image/jpg',
  'image/png',
  'image/webp',
  'image/gif',
  'image/avif',
];

const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB

export function isCloudinaryConfigured(): boolean {
  const cloudName =
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim() || '';
  const uploadPreset =
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET?.trim() || '';
  return (
    cloudName.length > 0 &&
    cloudName.toLowerCase() !== 'demo' &&
    uploadPreset.length > 0
  );
}

export function validateImageFile(file: File): {
  valid: boolean;
  error?: string;
} {
  if (!ALLOWED_IMAGE_MIME_TYPES.includes(file.type)) {
    return {
      valid: false,
      error:
        'Formato de archivo no admitido. Seleccioná una imagen JPG, PNG, WEBP, GIF o AVIF.',
    };
  }

  if (file.size > MAX_IMAGE_SIZE_BYTES) {
    return {
      valid: false,
      error: 'La imagen supera el tamaño máximo permitido de 10 MB.',
    };
  }

  return { valid: true };
}

function resolveControlledFallbackUrl(manualFallbackUrl?: string): string {
  const trimmed = manualFallbackUrl?.trim();
  if (
    trimmed &&
    (trimmed.startsWith('https://') || trimmed.startsWith('http://'))
  ) {
    return trimmed;
  }
  return DEFAULT_FICHA_TECNICA_PLACEHOLDER_URL;
}

function triggerDefensiveFallback(
  reasonMessage: string,
  options?: CloudinaryUploadOptions
): CloudinaryUploadResult {
  const fallbackUrl = resolveControlledFallbackUrl(options?.fallbackUrl);

  if (!options?.silentWarning && typeof window !== 'undefined') {
    toast.warning(reasonMessage, { duration: 8000 });
  }

  return {
    url: fallbackUrl,
    isFallback: true,
    warningMessage: reasonMessage,
  };
}

/**
 * Sube una fotografía Direct-to-Cloud a Cloudinary para el módulo de Fichas Técnicas.
 * Cumple con el contrato de Fallback Defensivo (09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md Sección 3):
 * Si NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME es "demo" o está vacío y la petición falla en entorno local,
 * retorna una URL manual o placeholder controlado emitiendo un toast.warning sin bloquear el guardado.
 */
export async function uploadImageToCloudinary(
  file: File,
  options?: CloudinaryUploadOptions
): Promise<CloudinaryUploadResult> {
  const validation = validateImageFile(file);
  if (!validation.valid) {
    const validationMsg =
      validation.error || 'Archivo de imagen inválido para subida.';
    if (typeof window !== 'undefined' && !options?.silentWarning) {
      toast.error(validationMsg);
    }
    throw new Error(validationMsg);
  }

  const cloudName =
    process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME?.trim() || '';
  const uploadPreset =
    process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET?.trim() ||
    'mipeluqueria_preset';
  const isDemoOrUnconfigured =
    cloudName.length === 0 || cloudName.toLowerCase() === 'demo';

  if (cloudName.length === 0) {
    return triggerDefensiveFallback(
      'Cloudinary no está configurado en el entorno local. Se asignó una URL de respaldo controlada (o podés ingresar una URL manual) para no bloquear el guardado de la Ficha Técnica.',
      options
    );
  }

  const formData = new FormData();
  formData.append('file', file);
  formData.append('upload_preset', uploadPreset);
  formData.append(
    'folder',
    options?.folder || 'mipeluqueria/fichas-tecnicas'
  );

  try {
    const response = await fetch(
      `https://api.cloudinary.com/v1_1/${encodeURIComponent(cloudName)}/image/upload`,
      {
        method: 'POST',
        body: formData,
      }
    );

    const payload = (await response
      .json()
      .catch(() => null)) as CloudinaryApiSuccessResponse | null;

    if (response.ok && payload && (payload.secure_url || payload.url)) {
      return {
        url: payload.secure_url || payload.url || '',
        publicId: payload.public_id,
        isFallback: false,
      };
    }

    const remoteErrorMessage =
      payload?.error?.message ||
      `HTTP ${response.status} al subir imagen a Cloudinary.`;

    if (isDemoOrUnconfigured || process.env.NODE_ENV !== 'production') {
      return triggerDefensiveFallback(
        `Cloudinary en modo local/demo (${remoteErrorMessage}). Se aplicó una URL de imagen controlada para permitir guardar la Ficha Técnica; también podés ingresar una URL manual.`,
        options
      );
    }

    throw new Error(remoteErrorMessage);
  } catch (err: unknown) {
    if (isDemoOrUnconfigured || process.env.NODE_ENV !== 'production') {
      return triggerDefensiveFallback(
        'No se pudo conectar con Cloudinary en entorno local (cloud_name="demo"). Se asignó un placeholder controlado para no bloquear el guardado de la Ficha Técnica; podés editar la URL manualmente si lo deseás.',
        options
      );
    }

    const errorMessage =
      err instanceof Error
        ? err.message
        : 'Error inesperado al subir la imagen a Cloudinary.';
    throw new Error(errorMessage);
  }
}

/**
 * Helper directo que retorna la URL final (real de Cloudinary o fallback controlado en desarrollo).
 */
export async function subirImagenCloudinary(
  file: File,
  manualFallbackUrl?: string
): Promise<string> {
  const result = await uploadImageToCloudinary(file, {
    fallbackUrl: manualFallbackUrl,
  });
  return result.url;
}
