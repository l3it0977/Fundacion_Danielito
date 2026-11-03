import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { LogOut, Menu, X } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useAutenticacion } from '../../contextos/ContextoAutenticacion';
import type { NombreRol } from '../../tipos';
import './BarraSuperior.css';

interface EnlaceNavegacion {
  etiqueta: string;
  ruta: string;
  Icono: LucideIcon;
  /** Roles que ven este enlace. Sin definir, lo ven todos. */
  roles?: NombreRol[];
}

interface Props {
  enlaces: EnlaceNavegacion[];
  subtitulo: string;
}

export function BarraSuperior({ enlaces, subtitulo }: Props) {
  const { usuario, cerrarSesion } = useAutenticacion();
  const [menuAbierto, setMenuAbierto] = useState(false);
  const ubicacion = useLocation();
  const botonMenu = useRef<HTMLButtonElement>(null);

  // Al navegar se cierra el menú: en móvil quedaría tapando la pantalla nueva.
  useEffect(() => {
    setMenuAbierto(false);
  }, [ubicacion.pathname]);

  // Escape cierra el menú y devuelve el foco al botón, para no perderlo al navegar con teclado.
  useEffect(() => {
    if (!menuAbierto) {
      return;
    }

    const alPresionarTecla = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') {
        setMenuAbierto(false);
        botonMenu.current?.focus();
      }
    };

    document.addEventListener('keydown', alPresionarTecla);
    return () => document.removeEventListener('keydown', alPresionarTecla);
  }, [menuAbierto]);

  if (!usuario) {
    return null;
  }

  const enlacesVisibles = enlaces.filter(
    (enlace) => !enlace.roles || enlace.roles.includes(usuario.rol),
  );

  const iniciales = `${usuario.nombre.charAt(0)}${usuario.apellido.charAt(0)}`.toUpperCase();

  const fechaEnMinusculas = new Date().toLocaleDateString('es-BO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  // Solo la primera letra va en mayúscula. Dejarlo en manos de CSS con "capitalize"
  // escribía "Lunes, 28 De Septiembre De 2026", con los "de" en mayúscula.
  const fechaDeHoy = fechaEnMinusculas.charAt(0).toUpperCase() + fechaEnMinusculas.slice(1);

  return (
    <>
      <header className="barra">
        <div className="barra__interior">
          <Link to="/" className="barra__marca">
            <img
              src="/img/logo-danielito.png"
              alt="Logo de la Fundación Danielito"
              className="barra__logo"
              width={56}
              height={56}
            />
            <span className="barra__marca-texto">
              <span className="barra__marca-titulo">
                Fundación <strong>Danielito</strong>
              </span>
              <span className="barra__marca-subtitulo">{subtitulo}</span>
            </span>
          </Link>

          <button
            ref={botonMenu}
            type="button"
            className="barra__boton-menu"
            onClick={() => setMenuAbierto((abierto) => !abierto)}
            aria-expanded={menuAbierto}
            aria-controls="menu-principal"
          >
            {menuAbierto ? <X size={22} aria-hidden="true" /> : <Menu size={22} aria-hidden="true" />}
            <span className="solo-lectores">
              {menuAbierto ? 'Cerrar menú' : 'Abrir menú'}
            </span>
          </button>

          <div className="barra__usuario">
            <span className="barra__avatar" aria-hidden="true">
              {iniciales}
            </span>

            <span className="barra__usuario-datos">
              <Link to="/perfil" className="barra__usuario-nombre">
                {usuario.nombre} {usuario.apellido}
              </Link>
              <span className="barra__usuario-fecha">{fechaDeHoy}</span>
            </span>

            <button type="button" className="barra__salir" onClick={cerrarSesion}>
              <LogOut size={16} aria-hidden="true" />
              <span>Salir</span>
            </button>
          </div>
        </div>
      </header>

      <nav
        id="menu-principal"
        className={`navegacion ${menuAbierto ? 'navegacion--abierta' : ''}`}
        aria-label="Navegación principal"
      >
        <ul className="navegacion__lista">
          {enlacesVisibles.map(({ etiqueta, ruta, Icono }) => (
            <li key={ruta}>
              <NavLink
                to={ruta}
                end={ruta === '/'}
                className={({ isActive }) =>
                  `navegacion__enlace ${isActive ? 'navegacion__enlace--activo' : ''}`
                }
              >
                <Icono size={18} aria-hidden="true" />
                <span>{etiqueta}</span>
              </NavLink>
            </li>
          ))}
        </ul>

        {/* Solo aparece en móvil, dentro del menú desplegado. */}
        <button type="button" className="navegacion__salir-movil" onClick={cerrarSesion}>
          <LogOut size={18} aria-hidden="true" />
          <span>Cerrar sesión</span>
        </button>
      </nav>

      <div className="franja-degradado" />

      {/* Capa oscura que cierra el menú al tocar fuera, solo en móvil. */}
      {menuAbierto && (
        <button
          type="button"
          className="navegacion__cortina"
          onClick={() => setMenuAbierto(false)}
        >
          <span className="solo-lectores">Cerrar menú</span>
        </button>
      )}
    </>
  );
}
