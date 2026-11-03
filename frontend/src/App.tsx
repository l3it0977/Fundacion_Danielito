import { lazy } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ProveedorAutenticacion } from './contextos/ContextoAutenticacion';
import { useAutenticacion } from './contextos/useAutenticacion';
import { RutaProtegida } from './componentes/RutaProtegida';
import { Diseno } from './componentes/layout/Diseno';
import { Login } from './paginas/Login';

/*
  Las pantallas se descargan cuando se visitan por primera vez, no todas al abrir la aplicación.
  Así la pantalla de ingreso carga rápido, y el mapa (Leaflet, lo más pesado) solo se baja
  al abrir la ficha de un niño. Cada pantalla es un export con nombre, de ahí el .then.
*/
const PanelGeneral = lazy(() => import('./paginas/PanelGeneral').then((m) => ({ default: m.PanelGeneral })));
const PanelMaestro = lazy(() => import('./paginas/PanelMaestro').then((m) => ({ default: m.PanelMaestro })));
const Ninos = lazy(() => import('./paginas/Ninos').then((m) => ({ default: m.Ninos })));
const FormularioNino = lazy(() => import('./paginas/FormularioNino').then((m) => ({ default: m.FormularioNino })));
const DetalleNino = lazy(() => import('./paginas/DetalleNino').then((m) => ({ default: m.DetalleNino })));
const Actividades = lazy(() => import('./paginas/Actividades').then((m) => ({ default: m.Actividades })));
const AsignarNinos = lazy(() => import('./paginas/AsignarNinos').then((m) => ({ default: m.AsignarNinos })));
const Asistencia = lazy(() => import('./paginas/Asistencia').then((m) => ({ default: m.Asistencia })));
const ElegirActividadParaAsistencia = lazy(() =>
  import('./paginas/Asistencia').then((m) => ({ default: m.ElegirActividadParaAsistencia })),
);
const Grupos = lazy(() => import('./paginas/Grupos').then((m) => ({ default: m.Grupos })));
const Usuarios = lazy(() => import('./paginas/Usuarios').then((m) => ({ default: m.Usuarios })));
const Bitacora = lazy(() => import('./paginas/Bitacora').then((m) => ({ default: m.Bitacora })));
const Reportes = lazy(() => import('./paginas/Reportes').then((m) => ({ default: m.Reportes })));
const Importar = lazy(() => import('./paginas/Importar').then((m) => ({ default: m.Importar })));
const Perfil = lazy(() => import('./paginas/Perfil').then((m) => ({ default: m.Perfil })));

/** Roles que administran el sistema; coincide con los [Authorize] del backend. */
const ADMINISTRACION = ['Directora', 'Administrador'] as const;

/**
 * La ruta principal muestra un panel distinto según el rol: el maestro ve solo
 * sus grupos, la directora y el administrador ven las cifras de todo el sistema.
 */
function PanelSegunRol() {
  const { usuario } = useAutenticacion();

  return usuario?.rol === 'Maestro' ? <PanelMaestro /> : <PanelGeneral />;
}

export function App() {
  return (
    <ProveedorAutenticacion>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />

          <Route
            element={
              <RutaProtegida>
                <Diseno />
              </RutaProtegida>
            }
          >
            <Route index element={<PanelSegunRol />} />

            {/* Niños: ver es para todos; crear y editar, solo administración. */}
            <Route path="ninos" element={<Ninos />} />
            <Route path="ninos/:id" element={<DetalleNino />} />
            <Route
              path="ninos/nuevo"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <FormularioNino />
                </RutaProtegida>
              }
            />
            <Route
              path="ninos/:id/editar"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <FormularioNino />
                </RutaProtegida>
              }
            />

            {/*
              Actividades: cualquier rol las crea y elige a sus participantes. No se
              limita la ruta por rol porque un maestro sí puede gestionar las suyas;
              el backend comprueba en cada petición que la actividad le corresponda.
            */}
            <Route path="actividades" element={<Actividades />} />
            <Route path="actividades/:id/ninos" element={<AsignarNinos />} />

            <Route path="asistencia" element={<ElegirActividadParaAsistencia />} />
            <Route path="asistencia/:id" element={<Asistencia />} />

            <Route path="perfil" element={<Perfil />} />

            <Route
              path="grupos"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <Grupos />
                </RutaProtegida>
              }
            />
            <Route
              path="reportes"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <Reportes />
                </RutaProtegida>
              }
            />
            <Route
              path="importar"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <Importar />
                </RutaProtegida>
              }
            />
            <Route
              path="usuarios"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <Usuarios />
                </RutaProtegida>
              }
            />
            <Route
              path="bitacora"
              element={
                <RutaProtegida rolesPermitidos={[...ADMINISTRACION]}>
                  <Bitacora />
                </RutaProtegida>
              }
            />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </ProveedorAutenticacion>
  );
}
