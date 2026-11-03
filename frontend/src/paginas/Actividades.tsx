import { useState } from 'react';
import { Link } from 'react-router-dom';
import { CalendarCheck, FileDown, Pencil, Plus, Trash2, UserPlus } from 'lucide-react';
import { servicioActividades, servicioNinos, servicioReportes } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { usePermisosActividad } from '../hooks/usePermisosActividad';
import { fechaLocalDeHoy, formatearFecha, soloFecha } from '../utilidades/fechas';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import {
  CampoCasilla,
  CampoSeleccion,
  CampoTexto,
  CampoTextoLargo,
} from '../componentes/comunes/Campos';
import { DialogoConfirmacion } from '../componentes/comunes/DialogoConfirmacion';
import { SelectorNinos } from '../componentes/comunes/SelectorNinos';
import type { Actividad } from '../tipos';
import './Listados.css';

interface FormularioActividad {
  id?: number;
  nombre: string;
  descripcion: string;
  fecha_Actividad: string;
  tipo: string;
  /** Participantes elegidos uno a uno, de cualquier grupo. */
  ninosId: number[];
  activa: boolean;
}

/**
 * Formulario en blanco para una actividad nueva. Es una función y no una
 * constante para que la fecha de hoy se calcule al abrirlo, no al cargar la
 * página: si la pestaña queda abierta de un día para otro, la fecha sigue bien.
 */
function formularioNuevo(): FormularioActividad {
  return {
    nombre: '',
    descripcion: '',
    fecha_Actividad: fechaLocalDeHoy(),
    tipo: 'normal',
    ninosId: [],
    activa: true,
  };
}

/** Carga en el formulario los datos de una actividad existente para editarla. */
function formularioDesdeActividad(actividad: Actividad): FormularioActividad {
  return {
    id: actividad.id,
    nombre: actividad.nombre,
    descripcion: actividad.descripcion ?? '',
    fecha_Actividad: soloFecha(actividad.fecha_Actividad),
    tipo: actividad.tipo,
    ninosId: actividad.ninos,
    activa: actividad.activa,
  };
}

export function Actividades() {
  // Todos los roles crean actividades; editar o borrar depende de quién la creó.
  const { esPropia, puedeGestionar } = usePermisosActividad();
  const {
    datos: actividades,
    cargando,
    error,
    recargar,
  } = useCargarDatos(servicioActividades.listar);
  // Lista reducida de todos los niños: se puede elegir de cualquier grupo.
  const { datos: ninosDisponibles } = useCargarDatos(servicioNinos.paraSeleccion);

  const [formulario, setFormulario] = useState<FormularioActividad | null>(null);
  const [actividadAEliminar, setActividadAEliminar] = useState<Actividad | null>(null);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [procesando, setProcesando] = useState(false);

  /**
   * Abre el formulario, vacío o con una actividad cargada. Se sube al inicio de
   * la página porque el formulario aparece arriba del listado y, si la persona
   * tocó "Editar" en una fila de abajo, no lo vería abrirse.
   */
  function abrirFormulario(datosIniciales: FormularioActividad) {
    setAviso(null);
    setFormulario(datosIniciales);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function guardar() {
    if (!formulario) {
      return;
    }

    setProcesando(true);
    setAviso(null);

    const datosAEnviar = {
      nombre: formulario.nombre.trim(),
      descripcion: formulario.descripcion.trim() || null,
      fecha_Actividad: formulario.fecha_Actividad,
      tipo: formulario.tipo,
      ninosId: formulario.ninosId,
    };

    try {
      if (formulario.id) {
        await servicioActividades.actualizar({
          ...datosAEnviar,
          id: formulario.id,
          activa: formulario.activa,
        });
      } else {
        await servicioActividades.crear(datosAEnviar);
      }

      setAviso({
        tipo: 'exito',
        texto: formulario.id
          ? 'Actividad actualizada.'
          : 'Actividad creada. Su lista de asistencia ya quedó armada.',
      });
      setFormulario(null);
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
    }
  }

  async function eliminar() {
    if (!actividadAEliminar) {
      return;
    }

    setProcesando(true);

    try {
      await servicioActividades.eliminar(actividadAEliminar.id);
      setAviso({ tipo: 'exito', texto: 'Actividad eliminada.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
      setActividadAEliminar(null);
    }
  }

  async function descargarReporte(actividadId: number) {
    setAviso(null);

    try {
      await servicioReportes.asistenciaPorActividad(actividadId);
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    }
  }

  if (cargando) {
    return <Cargando mensaje="Cargando actividades…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Actividades"
        descripcion="Elige los participantes de cualquier grupo. Al crear la actividad se arma sola su lista de asistencia."
        acciones={
          // Disponible para todos los roles: el maestro también arma sus actividades.
          <Boton
            variante="naranja"
            onClick={() => abrirFormulario(formularioNuevo())}
            icono={<Plus size={18} />}
          >
            Nueva actividad
          </Boton>
        }
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      {formulario && (
        <Panel titulo={formulario.id ? 'Editar actividad' : 'Nueva actividad'}>
          <form
            onSubmit={(evento) => {
              evento.preventDefault();
              guardar();
            }}
          >
            <div className="formulario-rejilla">
              <CampoTexto
                etiqueta="Nombre"
                requerido
                value={formulario.nombre}
                onChange={(evento) =>
                  setFormulario({ ...formulario, nombre: evento.target.value })
                }
              />

              <CampoSeleccion
                etiqueta="Tipo"
                requerido
                value={formulario.tipo}
                onChange={(evento) => setFormulario({ ...formulario, tipo: evento.target.value })}
                opciones={[
                  { valor: 'normal', etiqueta: 'Normal' },
                  { valor: 'intervencion', etiqueta: 'Intervención' },
                ]}
              />

              <CampoTexto
                etiqueta="Fecha"
                type="date"
                requerido
                value={formulario.fecha_Actividad}
                onChange={(evento) =>
                  setFormulario({ ...formulario, fecha_Actividad: evento.target.value })
                }
                ayuda="Con esta fecha se arma la lista de asistencia."
              />

              {formulario.id && (
                <CampoCasilla
                  etiqueta="Actividad activa"
                  checked={formulario.activa}
                  onChange={(evento) =>
                    setFormulario({ ...formulario, activa: evento.target.checked })
                  }
                />
              )}

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
              Participantes: se eligen niño por niño, de cualquier grupo. El selector
              los muestra agrupados por edad y permite marcar un grupo entero de una vez.
            */}
            <div className="formulario-seccion" style={{ marginTop: 'var(--espacio-5)' }}>
              <h3 className="formulario-seccion__titulo">Niños participantes</h3>

              <SelectorNinos
                ninosDisponibles={ninosDisponibles ?? []}
                seleccionados={formulario.ninosId}
                alCambiarSeleccion={(elegidos) =>
                  setFormulario({ ...formulario, ninosId: elegidos })
                }
              />
            </div>

            <div className="formulario-acciones">
              {/* El backend exige al menos un participante: se evita el envío fallido. */}
              <Boton
                type="submit"
                cargando={procesando}
                disabled={formulario.ninosId.length === 0}
              >
                {formulario.id ? 'Guardar cambios' : 'Crear actividad'}
              </Boton>

              <Boton variante="contorno" type="button" onClick={() => setFormulario(null)}>
                Cancelar
              </Boton>
            </div>
          </form>
        </Panel>
      )}

      <Panel titulo={`Listado (${actividades?.length ?? 0})`}>
        {!actividades || actividades.length === 0 ? (
          <SinDatos mensaje="Todavía no hay actividades registradas." />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th scope="col">Actividad</th>
                  <th scope="col">Fecha</th>
                  <th scope="col">Tipo</th>
                  <th scope="col">Participantes</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {actividades.map((actividad) => {
                  // Se calcula una vez por fila y decide qué botones se muestran.
                  const gestionable = puedeGestionar(actividad);

                  return (
                  <tr key={actividad.id}>
                    <td data-etiqueta="Actividad">
                      <strong>{actividad.nombre}</strong>
                      {actividad.descripcion && (
                        <span className="tabla__secundario">{actividad.descripcion}</span>
                      )}
                      {/*
                        Quién la organizó, escrito y no solo con un color: explica por
                        qué un maestro ve "Editar" en unas filas y en otras no.
                      */}
                      <span className="tabla__secundario">
                        {esPropia(actividad)
                          ? 'Creada por ti'
                          : `Creada por ${actividad.nombre_Creador}`}
                      </span>
                    </td>
                    <td data-etiqueta="Fecha">
                      {formatearFecha(actividad.fecha_Actividad)}
                    </td>
                    <td data-etiqueta="Tipo">
                      <Insignia
                        texto={actividad.tipo === 'intervencion' ? 'Intervención' : 'Normal'}
                        tono={actividad.tipo === 'intervencion' ? 'intervencion' : 'teal'}
                      />
                    </td>
                    <td data-etiqueta="Participantes">
                      {actividad.ninos.length}
                      {actividad.grupos.length > 0 && (
                        <span className="tabla__secundario">
                          {actividad.grupos.join(', ')}
                        </span>
                      )}
                    </td>
                    <td data-etiqueta="Estado">
                      <Insignia
                        texto={actividad.activa ? 'Activa' : 'Inactiva'}
                        tono={actividad.activa ? 'exito' : 'neutro'}
                      />
                    </td>
                    <td data-etiqueta="Acciones">
                      <div className="tabla__acciones">
                        {/* Pasar lista y descargar el PDF: cualquiera que vea la actividad. */}
                        <Link
                          to={`/asistencia/${actividad.id}`}
                          className="enlace-boton"
                        >
                          <CalendarCheck size={14} aria-hidden="true" />
                          Asistencia
                        </Link>

                        <Boton
                          tamano="chico"
                          variante="secundario"
                          onClick={() => descargarReporte(actividad.id)}
                          icono={<FileDown size={14} />}
                          aria-label={`Descargar PDF de asistencia de ${actividad.nombre}`}
                        >
                          PDF
                        </Boton>

                        {/*
                          Cambiar la actividad: dirección en todas, el maestro en las suyas.
                          Se ocultan en lugar de deshabilitarse, porque no es algo que la
                          persona pueda destrabar por sí misma.
                        */}
                        {gestionable && (
                          <>
                            <Link
                              to={`/actividades/${actividad.id}/ninos`}
                              className="enlace-boton enlace-boton--naranja"
                            >
                              <UserPlus size={14} aria-hidden="true" />
                              Niños
                            </Link>

                            <Boton
                              tamano="chico"
                              variante="contorno"
                              onClick={() => abrirFormulario(formularioDesdeActividad(actividad))}
                              icono={<Pencil size={14} />}
                              aria-label={`Editar ${actividad.nombre}`}
                            >
                              Editar
                            </Boton>

                            <Boton
                              tamano="chico"
                              variante="peligro"
                              onClick={() => setActividadAEliminar(actividad)}
                              icono={<Trash2 size={14} />}
                              aria-label={`Eliminar ${actividad.nombre}`}
                            >
                              Eliminar
                            </Boton>
                          </>
                        )}
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

      {actividadAEliminar && (
        <DialogoConfirmacion
          titulo="Eliminar actividad"
          mensaje={`Se eliminará "${actividadAEliminar.nombre}" junto con todas sus asistencias registradas. Esta acción no se puede deshacer.`}
          textoConfirmar="Eliminar"
          esDestructiva
          procesando={procesando}
          alConfirmar={eliminar}
          alCancelar={() => setActividadAEliminar(null)}
        />
      )}
    </div>
  );
}
