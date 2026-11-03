import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ProveedorAutenticacion, useAutenticacion } from './contextos/ContextoAutenticacion';
import { RutaProtegida } from './componentes/RutaProtegida';
import { Diseno } from './componentes/layout/Diseno';
import { Login } from './paginas/Login';
import { PanelGeneral } from './paginas/PanelGeneral';
import { PanelMaestro } from './paginas/PanelMaestro';
import { Ninos } from './paginas/Ninos';
import { FormularioNino } from './paginas/FormularioNino';
import { DetalleNino } from './paginas/DetalleNino';
import { Actividades } from './paginas/Actividades';
import { AsignarNinos } from './paginas/AsignarNinos';
import { Asistencia, ElegirActividadParaAsistencia } from './paginas/Asistencia';
import { Grupos } from './paginas/Grupos';
import { Usuarios } from './paginas/Usuarios';
import { Reportes } from './paginas/Reportes';
import { Importar } from './paginas/Importar';
import { Perfil } from './paginas/Perfil';

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
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </ProveedorAutenticacion>
  );
}
