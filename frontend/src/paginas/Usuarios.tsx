import { useState } from 'react';
import { ShieldCheck, UserCheck, UserX } from 'lucide-react';
import { servicioUsuarios } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { useAutenticacion } from '../contextos/useAutenticacion';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { CampoSeleccion } from '../componentes/comunes/Campos';
import { DialogoConfirmacion } from '../componentes/comunes/DialogoConfirmacion';
import type { Usuario } from '../tipos';
import './Listados.css';

const ROLES = ['Directora', 'Administrador', 'Maestro'];

export function Usuarios() {
  const { usuario: usuarioActual } = useAutenticacion();
  const { datos: usuarios, cargando, error, recargar } = useCargarDatos(servicioUsuarios.listar);

  const [usuarioACambiarEstado, setUsuarioACambiarEstado] = useState<Usuario | null>(null);
  const [rolEnEdicion, setRolEnEdicion] = useState<{ usuario: Usuario; rol: string } | null>(null);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [procesando, setProcesando] = useState(false);

  async function cambiarEstado() {
    if (!usuarioACambiarEstado) {
      return;
    }

    setProcesando(true);
    setAviso(null);

    try {
      const nuevoEstado = !usuarioACambiarEstado.activo;
      await servicioUsuarios.cambiarEstado(usuarioACambiarEstado.id, nuevoEstado);

      setAviso({
        tipo: 'exito',
        texto: nuevoEstado ? 'Usuario activado.' : 'Usuario desactivado.',
      });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
      setUsuarioACambiarEstado(null);
    }
  }

  async function cambiarRol() {
    if (!rolEnEdicion) {
      return;
    }

    setProcesando(true);
    setAviso(null);

    try {
      await servicioUsuarios.cambiarRol(rolEnEdicion.usuario.id, rolEnEdicion.rol);
      setAviso({ tipo: 'exito', texto: 'Rol actualizado.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
      setRolEnEdicion(null);
    }
  }

  if (cargando) {
    return <Cargando mensaje="Cargando usuarios…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Usuarios"
        descripcion="Los usuarios no se eliminan: se desactivan, para conservar el registro de lo que hicieron."
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      {rolEnEdicion && (
        <Panel titulo={`Cambiar rol de ${rolEnEdicion.usuario.nombre} ${rolEnEdicion.usuario.apellido}`}>
          <form
            onSubmit={(evento) => {
              evento.preventDefault();
              cambiarRol();
            }}
          >
            <CampoSeleccion
              etiqueta="Nuevo rol"
              value={rolEnEdicion.rol}
              onChange={(evento) =>
                setRolEnEdicion({ ...rolEnEdicion, rol: evento.target.value })
              }
              opciones={ROLES.map((rol) => ({ valor: rol, etiqueta: rol }))}
              ayuda="Un maestro con grupos a cargo no puede cambiar de rol hasta reasignarlos."
            />

            <div className="formulario-acciones">
              <Boton type="submit" cargando={procesando}>
                Guardar rol
              </Boton>

              <Boton variante="contorno" type="button" onClick={() => setRolEnEdicion(null)}>
                Cancelar
              </Boton>
            </div>
          </form>
        </Panel>
      )}

      <Panel titulo={`Listado (${usuarios?.length ?? 0})`}>
        {!usuarios || usuarios.length === 0 ? (
          <SinDatos mensaje="No hay usuarios registrados." />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Correo</th>
                  <th scope="col">Rol</th>
                  <th scope="col">Grupos a cargo</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {usuarios.map((usuario) => {
                  // El backend impide actuar sobre la propia cuenta; aquí se refleja
                  // deshabilitando los botones, para no ofrecer algo que va a fallar.
                  const esMiCuenta = usuario.id === usuarioActual?.perfilId;

                  return (
                    <tr key={usuario.id}>
                      <td data-etiqueta="Nombre">
                        <strong>
                          {usuario.nombre} {usuario.apellido}
                        </strong>
                        {esMiCuenta && (
                          <span className="tabla__secundario">Es tu cuenta</span>
                        )}
                      </td>
                      <td data-etiqueta="Correo">{usuario.email}</td>
                      <td data-etiqueta="Rol">
                        <Insignia
                          texto={usuario.rol}
                          tono={usuario.rol === 'Maestro' ? 'teal' : 'naranja'}
                        />
                      </td>
                      <td data-etiqueta="Grupos a cargo">
                        {usuario.mis_Grupos.length > 0 ? usuario.mis_Grupos.join(', ') : '—'}
                      </td>
                      <td data-etiqueta="Estado">
                        <Insignia
                          texto={usuario.activo ? 'Activo' : 'Inactivo'}
                          tono={usuario.activo ? 'exito' : 'error'}
                        />
                      </td>
                      <td data-etiqueta="Acciones">
                        <div className="tabla__acciones">
                          <Boton
                            tamano="chico"
                            variante="naranja"
                            disabled={esMiCuenta}
                            onClick={() => setRolEnEdicion({ usuario, rol: usuario.rol })}
                            icono={<ShieldCheck size={14} />}
                          >
                            Rol
                          </Boton>

                          <Boton
                            tamano="chico"
                            variante={usuario.activo ? 'peligro' : 'primario'}
                            disabled={esMiCuenta}
                            onClick={() => setUsuarioACambiarEstado(usuario)}
                            icono={
                              usuario.activo ? <UserX size={14} /> : <UserCheck size={14} />
                            }
                          >
                            {usuario.activo ? 'Desactivar' : 'Activar'}
                          </Boton>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Panel>

      {usuarioACambiarEstado && (
        <DialogoConfirmacion
          titulo={usuarioACambiarEstado.activo ? 'Desactivar usuario' : 'Activar usuario'}
          mensaje={
            usuarioACambiarEstado.activo
              ? `${usuarioACambiarEstado.nombre} ${usuarioACambiarEstado.apellido} dejará de poder entrar al sistema. Si tiene una sesión abierta, se le cerrará en su próxima acción.`
              : `${usuarioACambiarEstado.nombre} ${usuarioACambiarEstado.apellido} podrá volver a entrar al sistema.`
          }
          textoConfirmar={usuarioACambiarEstado.activo ? 'Desactivar' : 'Activar'}
          esDestructiva={usuarioACambiarEstado.activo}
          procesando={procesando}
          alConfirmar={cambiarEstado}
          alCancelar={() => setUsuarioACambiarEstado(null)}
        />
      )}
    </div>
  );
}
