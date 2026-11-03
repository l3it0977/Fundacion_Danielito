import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { servicioActividades, servicioAsistencias, servicioNinos } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { usePermisosActividad } from '../hooks/usePermisosActividad';
import { fechaLocalDeHoy, formatearFecha, soloFecha } from '../utilidades/fechas';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { CampoTexto } from '../componentes/comunes/Campos';
import { Insignia } from '../componentes/comunes/Insignia';
import type { EstadoAsistencia } from '../tipos';
import './Asistencia.css';

const ESTADOS: { valor: EstadoAsistencia; etiqueta: string }[] = [
  { valor: 'presente', etiqueta: 'Presente' },
  { valor: 'ausente', etiqueta: 'Ausente' },
  { valor: 'justificado', etiqueta: 'Justificado' },
];

/** Lo marcado para cada niño antes de guardar. */
interface MarcaAsistencia {
  estado: EstadoAsistencia;
  motivo: string;
}

export function Asistencia() {
  const { id } = useParams();
  const navegar = useNavigate();
  const actividadId = Number(id);
  const { puedeGestionar } = usePermisosActividad();

  const {
    datos: actividad,
    cargando: cargandoActividad,
    error: errorActividad,
  } = useCargarDatos(() => servicioActividades.obtener(actividadId), [actividadId]);

  // Se usa la lista reducida y no la ficha de niños: la ficha de un maestro solo
  // trae a los de sus grupos, y una actividad puede reunir niños de cualquier grupo.
  const { datos: todosLosNinos } = useCargarDatos(servicioNinos.paraSeleccion);

  const {
    datos: registradas,
    cargando: cargandoRegistros,
    recargar,
  } = useCargarDatos(() => servicioAsistencias.porActividad(actividadId), [actividadId]);

  const [fecha, setFecha] = useState(fechaLocalDeHoy);
  const [marcas, setMarcas] = useState<Record<number, MarcaAsistencia>>({});
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [guardando, setGuardando] = useState(false);

  // Evita que la fecha se vuelva a elegir sola cada vez que se recargan los registros:
  // solo se propone al entrar; después manda lo que la persona elija.
  const fechaYaPropuesta = useRef(false);

  // Solo los niños asignados a la actividad pueden tener asistencia.
  const participantes = useMemo(() => {
    if (!actividad || !todosLosNinos) {
      return [];
    }

    return todosLosNinos
      .filter((nino) => actividad.ninos.includes(nino.id))
      .sort((unNino, otroNino) => unNino.nombre_Completo.localeCompare(otroNino.nombre_Completo));
  }, [actividad, todosLosNinos]);

  /*
    La lista generada al crear la actividad queda con la fecha de la actividad.
    Si esa lista sigue pendiente, se abre en ese día para que aparezca armada; si
    no hay nada pendiente, se abre en hoy, que es lo habitual para una sesión nueva.
  */
  useEffect(() => {
    if (fechaYaPropuesta.current || !registradas) {
      return;
    }

    const fechasConPendientes = registradas
      .filter((registro) => registro.estado === 'pendiente')
      .map((registro) => soloFecha(registro.fecha))
      .sort();

    if (fechasConPendientes.length > 0) {
      setFecha(fechasConPendientes[0]);
    }

    fechaYaPropuesta.current = true;
  }, [registradas]);

  // La lista llega armada desde el backend al crear la actividad: todos los
  // participantes aparecen ya en estado pendiente. Al cambiar de fecha se cargan
  // los estados guardados de ese día, para corregir sin volver a marcar todo.
  useEffect(() => {
    if (!registradas) {
      return;
    }

    const delDia = registradas.filter((registro) => soloFecha(registro.fecha) === fecha);

    const marcasDelDia: Record<number, MarcaAsistencia> = {};
    for (const registro of delDia) {
      marcasDelDia[registro.ninoId] = {
        estado: registro.estado,
        motivo: registro.motivo_Justificacion ?? '',
      };
    }

    setMarcas(marcasDelDia);
  }, [registradas, fecha]);

  function marcar(ninoId: number, estado: EstadoAsistencia) {
    setMarcas((anteriores) => ({
      ...anteriores,
      [ninoId]: { estado, motivo: anteriores[ninoId]?.motivo ?? '' },
    }));
  }

  function escribirMotivo(ninoId: number, motivo: string) {
    setMarcas((anteriores) => ({
      ...anteriores,
      [ninoId]: { estado: anteriores[ninoId]?.estado ?? 'presente', motivo },
    }));
  }

  function marcarTodosPresentes() {
    const todas: Record<number, MarcaAsistencia> = {};

    for (const nino of participantes) {
      todas[nino.id] = { estado: 'presente', motivo: marcas[nino.id]?.motivo ?? '' };
    }

    setMarcas(todas);
  }

  async function guardar() {
    setGuardando(true);
    setAviso(null);

    // Los pendientes se dejan como están: enviarlos no aporta nada y el backend
    // ya los tiene guardados con ese mismo estado.
    const asistencias = Object.entries(marcas)
      .filter(([, marca]) => marca.estado !== 'pendiente')
      .map(([ninoId, marca]) => ({
        ninoId: Number(ninoId),
        estado: marca.estado,
        motivo_Justificacion: marca.estado === 'justificado' ? marca.motivo.trim() || null : null,
        observacion: null,
      }));

    try {
      await servicioAsistencias.registrarPorActividad({ actividadId, fecha, asistencias });
      setAviso({ tipo: 'exito', texto: 'Asistencia guardada.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardando(false);
    }
  }

  if (cargandoActividad) {
    return <Cargando mensaje="Cargando actividad…" />;
  }

  if (errorActividad) {
    return <MensajeError mensaje={errorActividad} />;
  }

  if (!actividad) {
    return <MensajeError mensaje="No se encontró la actividad." />;
  }

  // Los pendientes no cuentan como marcados: son los que faltan por revisar.
  const cantidadMarcada = Object.values(marcas).filter(
    (marca) => marca.estado !== 'pendiente',
  ).length;

  const cantidadPendiente = participantes.length - cantidadMarcada;

  // Las sesiones son las fechas distintas en las que ya se tomó asistencia.
  const sesiones = [...new Set((registradas ?? []).map((registro) => soloFecha(registro.fecha)))]
    .sort()
    .reverse();

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Tomar asistencia"
        descripcion={`Actividad: ${actividad.nombre}`}
        acciones={
          <Boton
            variante="contorno"
            onClick={() => navegar('/actividades')}
            icono={<ArrowLeft size={18} />}
          >
            Volver
          </Boton>
        }
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      <Panel titulo="Sesión">
        <div className="filtros">
          <div className="filtros__campo">
            <CampoTexto
              etiqueta="Fecha de la sesión"
              type="date"
              value={fecha}
              /* El servidor rechaza sesiones anteriores a la actividad; el
                 calendario ya no deja elegirlas, así el error no llega a ocurrir. */
              min={soloFecha(actividad.fecha_Actividad)}
              onChange={(evento) => setFecha(evento.target.value)}
              ayuda="Si ya hay registros de ese día, se cargan para corregirlos."
            />
          </div>

          {participantes.length > 0 && (
            <Boton variante="contorno" onClick={marcarTodosPresentes}>
              Marcar todos presentes
            </Boton>
          )}
        </div>
      </Panel>

      <Panel
        titulo={`Participantes (${cantidadMarcada} de ${participantes.length} con asistencia)`}
        acciones={
          cantidadPendiente > 0 && (
            <Insignia
              texto={`${cantidadPendiente} sin pasar lista`}
              tono="neutro"
            />
          )
        }
      >
        {participantes.length === 0 ? (
          <SinDatos mensaje="Esta actividad todavía no tiene niños asignados.">
            {/* Solo quien puede cambiar la actividad ve el atajo para asignar niños. */}
            {puedeGestionar(actividad) && (
              <Boton variante="naranja" onClick={() => navegar(`/actividades/${actividadId}/ninos`)}>
                Asignar niños
              </Boton>
            )}
          </SinDatos>
        ) : (
          <>
            <ul className="lista-asistencia">
              {participantes.map((nino) => {
                const marca = marcas[nino.id];

                return (
                  <li key={nino.id} className="fila-asistencia">
                    <div className="fila-asistencia__nino">
                      <span className="fila-asistencia__nombre">{nino.nombre_Completo}</span>
                      <span className="fila-asistencia__codigo">
                        {/* El grupo ayuda a ubicar al niño: la lista puede mezclar edades. */}
                        {nino.codigo} · {nino.nombre_Grupo}
                        {/* Se marca con texto, no solo con color: quien no distingue
                            tonos debe poder ver igual a quién le falta asistencia. */}
                        {(!marca || marca.estado === 'pendiente') && ' · sin pasar lista'}
                      </span>
                    </div>

                    {/* Grupo de botones: el nombre del niño da contexto al lector de pantalla. */}
                    <div
                      className="grupo-estados"
                      role="group"
                      aria-label={`Estado de ${nino.nombre_Completo}`}
                    >
                      {ESTADOS.map((estado) => (
                        <button
                          key={estado.valor}
                          type="button"
                          className={`boton-estado boton-estado--${estado.valor} ${
                            marca?.estado === estado.valor ? 'boton-estado--activo' : ''
                          }`}
                          aria-pressed={marca?.estado === estado.valor}
                          onClick={() => marcar(nino.id, estado.valor)}
                        >
                          {estado.etiqueta}
                        </button>
                      ))}
                    </div>

                    {/* El motivo solo aparece cuando la falta es justificada. */}
                    {marca?.estado === 'justificado' && (
                      <input
                        type="text"
                        className="campo__control fila-asistencia__motivo"
                        placeholder="Motivo de la justificación"
                        value={marca.motivo}
                        onChange={(evento) => escribirMotivo(nino.id, evento.target.value)}
                        aria-label={`Motivo de justificación de ${nino.nombre_Completo}`}
                      />
                    )}
                  </li>
                );
              })}
            </ul>

            <div className="formulario-acciones">
              <Boton
                onClick={guardar}
                cargando={guardando}
                disabled={cantidadMarcada === 0}
                /* Solo se envían los estados ya marcados, no los pendientes. */
                icono={<Save size={18} />}
              >
                Guardar asistencia
              </Boton>
            </div>
          </>
        )}
      </Panel>

      <Panel titulo={`Sesiones registradas (${sesiones.length})`}>
        {cargandoRegistros ? (
          <Cargando />
        ) : sesiones.length === 0 ? (
          <SinDatos mensaje="Aún no se registró ninguna sesión de esta actividad." />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th scope="col">Fecha</th>
                  <th scope="col">Presentes</th>
                  <th scope="col">Ausentes</th>
                  <th scope="col">Justificados</th>
                  <th scope="col">Asistencia</th>
                </tr>
              </thead>
              <tbody>
                {sesiones.map((fechaSesion) => {
                  const registrosDelDia = (registradas ?? []).filter(
                    (registro) => soloFecha(registro.fecha) === fechaSesion,
                  );

                  const presentes = registrosDelDia.filter(
                    (registro) => registro.estado === 'presente',
                  ).length;
                  const ausentes = registrosDelDia.filter(
                    (registro) => registro.estado === 'ausente',
                  ).length;
                  const justificados = registrosDelDia.filter(
                    (registro) => registro.estado === 'justificado',
                  ).length;

                  // El porcentaje se calcula solo sobre lo ya revisado: incluir
                  // los pendientes lo haría ver como si hubieran faltado.
                  const yaRevisados = presentes + ausentes + justificados;

                  const porcentaje =
                    yaRevisados === 0 ? 0 : Math.round((presentes / yaRevisados) * 100);

                  return (
                    <tr key={fechaSesion}>
                      <td data-etiqueta="Fecha">
                        {formatearFecha(fechaSesion)}
                      </td>
                      <td data-etiqueta="Presentes">{presentes}</td>
                      <td data-etiqueta="Ausentes">{ausentes}</td>
                      <td data-etiqueta="Justificados">{justificados}</td>
                      <td data-etiqueta="Asistencia">
                        {yaRevisados === 0 ? (
                          <span className="texto-pendiente">Sin pasar lista</span>
                        ) : (
                          <strong>{porcentaje}%</strong>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </div>
  );
}

/** Pantalla intermedia: pide elegir una actividad antes de tomar asistencia. */
export function ElegirActividadParaAsistencia() {
  const { datos: actividades, cargando, error } = useCargarDatos(servicioActividades.listar);
  const navegar = useNavigate();

  if (cargando) {
    return <Cargando mensaje="Cargando actividades…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Asistencia"
        descripcion="Elige la actividad para la que quieres registrar o revisar asistencia."
      />

      <Panel titulo="Actividades">
        {!actividades || actividades.length === 0 ? (
          <SinDatos mensaje="No hay actividades registradas." />
        ) : (
          <ul className="lista-asistencia">
            {actividades.map((actividad) => (
              <li key={actividad.id} className="fila-asistencia">
                <div className="fila-asistencia__nino">
                  <span className="fila-asistencia__nombre">{actividad.nombre}</span>
                  <span className="fila-asistencia__codigo">
                    {actividad.fecha_Actividad
                      ? formatearFecha(actividad.fecha_Actividad)
                      : 'Sin fecha'}{' '}
                    · {actividad.ninos.length} participantes
                  </span>
                </div>

                <Boton
                  tamano="chico"
                  onClick={() => navegar(`/asistencia/${actividad.id}`)}
                >
                  Tomar asistencia
                </Boton>
              </li>
            ))}
          </ul>
        )}
      </Panel>
    </div>
  );
}
