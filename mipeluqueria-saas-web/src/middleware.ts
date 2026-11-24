import { NextRequest, NextResponse } from 'next/server';
import type { RolNombreType } from '@/types/api.types';

const ROLE_ROUTE_MAP: Record<string, readonly RolNombreType[]> = {
  '/saas-admin': ['SuperAdminSaaS', 'Administrador'],
  '/admin': ['Administrador', 'SuperAdminSaaS'],
  '/recepcion': ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
  '/peluquero': ['Peluquero', 'Administrador', 'SuperAdminSaaS'],
  '/mi-cuenta': [
    'Cliente',
    'Peluquero',
    'Recepcionista',
    'Cajero',
    'Administrador',
    'SuperAdminSaaS',
  ],
};

function getDefaultHomeForRole(role: string): string {
  switch (role as RolNombreType) {
    case 'SuperAdminSaaS':
    case 'Administrador':
      return '/admin/dashboard';
    case 'Recepcionista':
    case 'Cajero':
      return '/recepcion/calendario';
    case 'Peluquero':
      return '/peluquero';
    case 'Cliente':
      return '/mi-cuenta/turnos';
    default:
      return '/reservar';
  }
}

export function middleware(request: NextRequest): NextResponse {
  const { pathname } = request.nextUrl;

  // Rutas públicas abiertas 24/7 (Booking Público, Legal y Autenticación)
  if (
    pathname === '/' ||
    pathname.startsWith('/reservar') ||
    pathname.startsWith('/legal') ||
    pathname.startsWith('/login') ||
    pathname.startsWith('/registro') ||
    pathname.startsWith('/recuperar-clave')
  ) {
    return NextResponse.next();
  }

  const token = request.cookies.get('mipeluqueria_token')?.value;
  const role = request.cookies.get('mipeluqueria_role')?.value;

  if (!token || !role) {
    const loginUrl = new URL('/login', request.url);
    loginUrl.searchParams.set('redirect', pathname);
    return NextResponse.redirect(loginUrl);
  }

  for (const [routePrefix, allowedRoles] of Object.entries(ROLE_ROUTE_MAP)) {
    if (
      pathname.startsWith(routePrefix) &&
      !allowedRoles.includes(role as RolNombreType)
    ) {
      const targetHome = getDefaultHomeForRole(role);
      return NextResponse.redirect(new URL(targetHome, request.url));
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/saas-admin/:path*',
    '/admin/:path*',
    '/recepcion/:path*',
    '/peluquero/:path*',
    '/mi-cuenta/:path*',
  ],
};
