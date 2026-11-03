import { useState } from 'react';
import { Pencil, Plus, Trash2, Users } from 'lucide-react';
import { servicioGrupos, servicioUsuarios } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { CampoTexto, CampoTextoLargo, CampoCasilla } from '../componentes/comunes/Campos';
import { DialogoConfirmacion } from '../componentes/comunes/DialogoConfirmacion';
import type { Grupo } from '../tipos';
import './Listados.css';

/** Valores del formulario mientras se crea o edita un grupo. */
interface FormularioGrupo {
  id?: number;
  nombre: string;
  descripcion: string;
  /** Un grupo puede quedar a cargo de varios maestros a la vez. */
  maestrosId: number[];
  activo: boolean;
}

const FORMULARIO_VACIO: FormularioGrupo = {
  nombre: '',
  descripcion: '',
  maestrosId: [],
  activo: true,
};

export function Grupos() {
  const { datos: grupos, cargando, error, recargar } = useCargarDatos(servicioGrupos.listar);
  const { datos: usuarios } = useCargarDatos(servicioUsuarios.listar);

  const [formulario, setFormulario] = useState<FormularioGrupo | null>(null);
  const [grupoAEliminar, setGrupoAEliminar] = useState<Grupo | null>(null);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [guardando, setGuardando] = useState(false);

  // Solo los maestros pueden quedar a cargo de un grupo.
  const maestros = (usuarios ?? []).filter((usuario) => usuario.rol === 'Maestro');

  function abrirNuevo() {
    setAviso(null);
    setFormulario(FORMULARIO_VACIO);
  }

  function abrirEdicion(grupo: Grupo) {
    setAviso(null);
    setFormulario({
      id: grupo.id,
      nombre: grupo.nombre,
      descripcion: grupo.descripcion ?? '',
      maestrosId: grupo.maestrosId,
      activo: grupo.activo,
    });
  }

  async function guardar() {
    if (!formulario) {
      return;
    }

    setGuardando(true);
    setAviso(null);

    const datosAEnviar = {
      nombre: formulario.nombre.trim(),
      descripcion: formulario.descripcion.trim() || null,
      maestrosId: formulario.maestrosId,
      activo: formulario.activo,
    };

    try {
      if (formulario.id) {
        await servicioGrupos.actualizar({ ...datosAEnviar, id: formulario.id });
      } else {
        await servicioGrupos.crear(datosAEnviar);
      }

      setAviso({
        tipo: 'exito',
        texto: formulario.id ? 'Grupo actualizado.' : 'Grupo creado.',
      });
      setFormulario(null);
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardando(false);
    }
  }

  async function eliminar() {
    if (!grupoAEliminar) {
      return;
    }

    setGuardando(true);

    try {
      await servicioGrupos.eliminar(grupoAEliminar.id);
      setAviso({ tipo: 'exito', texto: 'Grupo eliminado.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardando(false);
      setGrupoAEliminar(null);
    }
  }

  if (cargando) {
    return <Cargando mensaje="Cargando grupos…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Grupos"
        descripcion="Cada grupo tiene un maestro a cargo, del que se deriva el maestro de sus niños."
        acciones={
          <Boton variante="naranja" onClick={abrirNuevo} icono={<Plus size={18} />}>
            Nuevo grupo
          </Boton>
        }
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      {formulario && (
        <Panel titulo={formulario.id ? 'Editar grupo' : 'Nuevo grupo'}>
          <form
            onSubmit={(evento) => {
              evento.preventDefault();
              guardar();
            }}
          >
            <div className="formulario-rejilla">
              <CampoTexto
                etiqueta="Nombre del grupo"
                requerido
                value={formulario.nombre}
                onChange={(evento) =>
                  setFormulario({ ...formulario, nombre: evento.target.value })
                }
                ayuda="Por ejemplo: 6 a 8 años"
              />

              <CampoCasilla
                etiqueta="Grupo activo"
                checked={formulario.activo}
                onChange={(evento) =>
                  setFormulario({ ...formulario, activo: evento.target.checked })
                }
              />

              <div className="formulario-rejilla__completo">
                <CampoTextoLargo
                  etiqueta="Descripción"
                  value={formulario.descripcion}
                  onChange={(evento) =>
                    setFormulario({ ...formulario, descripcion: evento.target.value })
                  }
                />
              </div>

            </div>

            {/*
              Varios maestros por grupo: se marcan con casillas en lugar de un
              desplegable, porque hay que poder elegir más de uno a la vez.
            */}
            <fieldset className="bloque-maestros">
              <legend className="bloque-maestros__titulo">
                Maestros a cargo
                <span className="bloque-maestros__conteo">
                  {formulario.maestrosId.length} elegido
                  {formulario.maestrosId.length === 1 ? '' : 's'}
                </span>
              </legend>

              {maestros.length === 0 ? (
                <p className="texto-ayuda">
                  No hay usuarios con rol Maestro. Crea uno desde la pantalla de Usuarios.
                </p>
              ) : (
                <div className="rejilla-seleccion">
                  {maestros.map((maestro) => {
                    const marcado = formulario.maestrosId.includes(maestro.id);

                    return (
                      <label
                        key={maestro.id}
                        className={`ficha-seleccion ${marcado ? 'ficha-seleccion--marcada' : ''}`}
                      >
                        <input
                          type="checkbox"
                          className="casilla__control"
                          checked={marcado}
                          onChange={() =>
                            setFormulario({
                              ...formulario,
                              maestrosId: marcado
                                ? formulario.maestrosId.filter((id) => id !== maestro.id)
                                : [...formulario.maestrosId, maestro.id],
                            })
                          }
                        />

                        <span className="ficha-seleccion__texto">
                          <span className="ficha-seleccion__nombre">
                            {maestro.nombre} {maestro.apellido}
                          </span>
                          <span className="ficha-seleccion__detalle">
                            {maestro.mis_Grupos.length > 0
                              ? `A cargo de: ${maestro.mis_Grupos.join(', ')}`
                              : 'Sin grupos asignados'}
                          </span>
                        </span>
                      </label>
                    );
                  })}
                </div>
              )}
            </fieldset>

            <div className="formulario-acciones">
              {/* El backend exige al menos un maestro: se evita el envío que fallaría. */}
              <Boton
                type="submit"
                cargando={guardando}
                disabled={formulario.maestrosId.length === 0}
              >
                {formulario.id ? 'Guardar cambios' : 'Crear grupo'}
              </Boton>

              <Boton variante="contorno" type="button" onClick={() => setFormulario(null)}>
                Cancelar
              </Boton>
            </div>
          </form>
        </Panel>
      )}

      <Panel titulo={`Listado (${grupos?.length ?? 0})`}>
        {!grupos || grupos.length === 0 ? (
          <SinDatos mensaje="Todavía no hay grupos registrados." />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th scope="col">Grupo</th>
                  <th scope="col">Maestros</th>
                  <th scope="col">Niños</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {grupos.map((grupo) => (
                  <tr key={grupo.id}>
                    <td data-etiqueta="Grupo">
                      <strong>{grupo.nombre}</strong>
                      {grupo.descripcion && (
                        <span className="tabla__secundario">{grupo.descripcion}</span>
                      )}
                    </td>
                    <td data-etiqueta="Maestros">
                      {grupo.nombres_Maestros.length > 0
                        ? grupo.nombres_Maestros.join(', ')
                        : '—'}
                    </td>
                    <td data-etiqueta="Niños">
                      <span className="tabla__con-icono">
                        <Users size={15} aria-hidden="true" />
                        {grupo.cantidad_Ninos}
                      </span>
                    </td>
                    <td data-etiqueta="Estado">
                      <Insignia
                        texto={grupo.activo ? 'Activo' : 'Inactivo'}
                        tono={grupo.activo ? 'exito' : 'neutro'}
                      />
                    </td>
                    <td data-etiqueta="Acciones">
                      <div className="tabla__acciones">
                        <Boton
                          tamano="chico"
                          variante="naranja"
                          onClick={() => abrirEdicion(grupo)}
                          icono={<Pencil size={14} />}
                        >
                          Editar
                        </Boton>

                        <Boton
                          tamano="chico"
                          variante="peligro"
                          onClick={() => setGrupoAEliminar(grupo)}
                          icono={<Trash2 size={14} />}
                        >
                          Eliminar
                        </Boton>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>

      {grupoAEliminar && (
        <DialogoConfirmacion
          titulo="Eliminar grupo"
          mensaje={`¿Seguro que quieres eliminar "${grupoAEliminar.nombre}"? Si tiene niños asignados, el sistema no permitirá borrarlo.`}
          textoConfirmar="Eliminar"
          esDestructiva
          procesando={guardando}
          alConfirmar={eliminar}
          alCancelar={() => setGrupoAEliminar(null)}
        />
      )}
    </div>
  );
}
