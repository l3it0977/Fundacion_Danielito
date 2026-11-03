import { Outlet } from 'react-router-dom';
import {
  BarChart3,
  Baby,
  CalendarCheck,
  ClipboardList,
  FileSpreadsheet,
  LayoutDashboard,
  Users,
  UserCog,
} from 'lucide-react';
import { BarraSuperior } from './BarraSuperior';
import { useAutenticacion } from '../../contextos/ContextoAutenticacion';
import './Diseno.css';

/**
 * Enlaces del menú. Los que llevan `roles` solo aparecen para esos perfiles,
 * en correspondencia con los [Authorize] de cada controlador del backend.
 */
const ENLACES = [
  { etiqueta: 'Panel', ruta: '/', Icono: LayoutDashboard },
  { etiqueta: 'Niños', ruta: '/ninos', Icono: Baby },
  { etiqueta: 'Actividades', ruta: '/actividades', Icono: ClipboardList },
  { etiqueta: 'Asistencia', ruta: '/asistencia', Icono: CalendarCheck },
  {
    etiqueta: 'Grupos',
    ruta: '/grupos',
    Icono: Users,
    roles: ['Directora', 'Administrador'] as const,
  },
  {
    etiqueta: 'Reportes',
    ruta: '/reportes',
    Icono: BarChart3,
    roles: ['Directora', 'Administrador'] as const,
  },
  {
    etiqueta: 'Importar',
    ruta: '/importar',
    Icono: FileSpreadsheet,
    roles: ['Directora', 'Administrador'] as const,
  },
  {
    etiqueta: 'Usuarios',
    ruta: '/usuarios',
    Icono: UserCog,
    roles: ['Directora', 'Administrador'] as const,
  },
];

const SUBTITULOS: Record<string, string> = {
  Directora: 'Panel de dirección',
  Administrador: 'Panel administrativo',
  Maestro: 'Panel del maestro',
};

export function Diseno() {
  const { usuario } = useAutenticacion();

  return (
    <div className="diseno">
      {/* Permite saltarse el menú con el teclado e ir directo al contenido. */}
      <a href="#contenido" className="salto-contenido">
        Saltar al contenido
      </a>

      <BarraSuperior
        enlaces={ENLACES as never}
        subtitulo={usuario ? (SUBTITULOS[usuario.rol] ?? '') : ''}
      />

      <main id="contenido" className="diseno__contenido">
        <Outlet />
      </main>

      <footer className="diseno__pie">
        <p>© Fundación Danielito — Warnes, Santa Cruz</p>
      </footer>
    </div>
  );
}
