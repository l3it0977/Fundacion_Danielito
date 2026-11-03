import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, Eye, Pencil, Plus, Search, Trash2, UserCheck, UserX } from 'lucide-react';
import { servicioNinos } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { useRetraso } from '../hooks/useRetraso';
import { useAutenticacion } from '../contextos/ContextoAutenticacion';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { CampoCasilla, CampoTexto } from '../componentes/comunes/Campos';
import { DialogoConfirmacion } from '../componentes/comunes/DialogoConfirmacion';
import { Paginacion } from '../componentes/comunes/Paginacion';
import type { Nino } from '../tipos';
import './Listados.css';

const NINOS_POR_PAGINA = 25;

export function Ninos() {
  const { tieneRol } = useAutenticacion();

  const [busqueda, setBusqueda] = useState('');
  // Se espera a que deje de escribir para no consultar al servidor con cada letra.
  const textoBuscado = useRetraso(busqueda.trim());
  // Los inactivos se ocultan por defecto: siguen en el sistema con su historial.
  const [verInactivos, setVerInactivos] = useState(false);
  const [pagina, setPagina] = useState(1);

  // La búsqueda y la paginación las hace el servidor: la lista sigue ágil con miles de fichas.
  const { datos: resultado, cargando, error, recargar } = useCargarDatos(
    () =>
      servicioNinos.buscar({
        texto: textoBuscado,
        incluirInactivos: verInactivos,
        pagina,
        tamano: NINOS_POR_PAGINA,
      }),
    [textoBuscado, verInactivos, pagina],
  );

  const [ninoAEliminar, setNinoAEliminar] = useState<Nino | null>(null);
  const [ninoACambiarEstado, setNinoACambiarEstado] = useState<Nino | null>(null);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [procesando, setProcesando] = useState(false);

  const puedeEditar = tieneRol('Directora', 'Administrador');

  // El servidor ya entrega los niños ordenados por grupo (de menor a mayor edad), así que
  // basta con ir agrupando en ese mismo orden. Una página puede cortar un grupo por la mitad.
  const ninosPorGrupo = useMemo(() => {
    const agrupados = new Map<string, Nino[]>();

    for (const nino of resultado?.items ?? []) {
      const nombreGrupo = nino.nombre_Grupo || 'Sin grupo';
      agrupados.set(nombreGrupo, [...(agrupados.get(nombreGrupo) ?? []), nino]);
    }

    return [...agrupados.entries()];
  }, [resultado]);

  async function cambiarEstado() {
    if (!ninoACambiarEstado) {
      return;
    }

    setProcesando(true);
    setAviso(null);

    try {
      const nuevoEstado = !ninoACambiarEstado.activo;
      await servicioNinos.cambiarEstado(ninoACambiarEstado.id, nuevoEstado);

      setAviso({ tipo: 'exito', texto: nuevoEstado ? 'Niño activado.' : 'Niño desactivado.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
      setNinoACambiarEstado(null);
    }
  }

  async function eliminar() {
    if (!ninoAEliminar) {
      return;
    }

    setProcesando(true);

    try {
      await servicioNinos.eliminar(ninoAEliminar.id);
      setAviso({ tipo: 'exito', texto: 'Niño eliminado.' });
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setProcesando(false);
      setNinoAEliminar(null);
    }
  }

  // Pantalla de carga solo la primera vez: al buscar o cambiar de página la lista
  // se queda visible, atenuada, para no perder el foco del campo de búsqueda.
  if (cargando && !resultado) {
    return <Cargando mensaje="Cargando niños…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  const totalEncontrados = resultado?.total ?? 0;

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Niños"
        descripcion={
          puedeEditar
            ? 'Listado completo, agrupado por grupo.'
            : 'Niños de los grupos que tienes a cargo.'
        }
        acciones={
          puedeEditar && (
            <Link to="/ninos/nuevo" className="enlace-boton enlace-boton--naranja enlace-boton--grande">
              <Plus size={18} aria-hidden="true" />
              Registrar niño
            </Link>
          )
        }
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      <div className="filtros">
        <div className="filtros__campo">
          <CampoTexto
            etiqueta="Buscar"
            type="search"
            placeholder="Nombre, apellido o código"
            value={busqueda}
            onChange={(evento) => {
              setBusqueda(evento.target.value);
              setPagina(1);
            }}
          />
        </div>

        <div className="filtros__campo filtros__campo--casilla">
          <CampoCasilla
            etiqueta="Mostrar inactivos"
            checked={verInactivos}
            onChange={(evento) => {
              setVerInactivos(evento.target.checked);
              setPagina(1);
            }}
          />
        </div>

        <p className="filtros__resultado">
          <Search size={16} aria-hidden="true" />
          {totalEncontrados} {totalEncontrados === 1 ? 'niño' : 'niños'}
        </p>
      </div>

      <div className={cargando ? 'lista-actualizando' : undefined} aria-busy={cargando}>
        {ninosPorGrupo.length === 0 ? (
          <SinDatos
            mensaje={
              textoBuscado
                ? `No se encontraron niños que coincidan con "${textoBuscado}".`
                : verInactivos
                  ? 'Todavía no hay niños registrados.'
                  : 'No hay niños activos. Marca "Mostrar inactivos" para ver los demás.'
            }
          />
        ) : (
          ninosPorGrupo.map(([nombreGrupo, listaDelGrupo]) => (
            <details key={nombreGrupo} className="grupo-plegable" open>
              <summary className="grupo-plegable__resumen">
                <span className="grupo-plegable__titulo">
                  {nombreGrupo}
                  <Insignia texto={`${listaDelGrupo.length}`} tono="teal" />
                </span>

                <ChevronDown className="grupo-plegable__flecha" size={20} aria-hidden="true" />
              </summary>

              <div className="tabla-contenedor">
                <table className="tabla">
                  <thead>
                    <tr>
                      <th scope="col">Código</th>
                      <th scope="col">Nombre</th>
                      <th scope="col">Edad</th>
                      <th scope="col">Maestros</th>
                      <th scope="col">Estado</th>
                      <th scope="col">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {listaDelGrupo.map((nino) => (
                      <tr key={nino.id}>
                        <td data-etiqueta="Código">{nino.codigo}</td>
                        <td data-etiqueta="Nombre">
                          <strong>
                            {nino.nombres} {nino.apellidos}
                          </strong>
                          {nino.vulnerable && (
                            <span className="tabla__secundario">
                              <Insignia texto="Vulnerable" tono="advertencia" />
                            </span>
                          )}
                        </td>
                        <td data-etiqueta="Edad">{nino.edad != null ? `${nino.edad} años` : '—'}</td>
                        <td data-etiqueta="Maestros">
                          {nino.nombres_Maestros.length > 0
                            ? nino.nombres_Maestros.join(', ')
                            : '—'}
                        </td>
                        <td data-etiqueta="Estado">
                          <Insignia
                            texto={nino.activo ? 'Activo' : 'Inactivo'}
                            tono={nino.activo ? 'exito' : 'neutro'}
                          />
                        </td>
                        <td data-etiqueta="Acciones">
                          <div className="tabla__acciones">
                            <Link to={`/ninos/${nino.id}`} className="enlace-boton">
                              <Eye size={14} aria-hidden="true" />
                              Ver
                            </Link>

                            {puedeEditar && (
                              <>
                                <Link
                                  to={`/ninos/${nino.id}/editar`}
                                  className="enlace-boton enlace-boton--naranja"
                                >
                                  <Pencil size={14} aria-hidden="true" />
                                  Editar
                                </Link>

                                <Boton
                                  tamano="chico"
                                  variante="contorno"
                                  onClick={() => setNinoACambiarEstado(nino)}
                                  icono={nino.activo ? <UserX size={14} /> : <UserCheck size={14} />}
                                >
                                  {nino.activo ? 'Desactivar' : 'Activar'}
                                </Boton>

                                <Boton
                                  tamano="chico"
                                  variante="peligro"
                                  onClick={() => setNinoAEliminar(nino)}
                                  icono={<Trash2 size={14} />}
                                >
                                  Eliminar
                                </Boton>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </details>
          ))
        )}
      </div>

      {resultado && (
        <Paginacion
          pagina={resultado.pagina}
          totalPaginas={resultado.totalPaginas}
          resumen={`${resultado.total} ${resultado.total === 1 ? 'niño' : 'niños'}`}
          alCambiar={setPagina}
          ocupado={cargando}
        />
      )}

      {ninoACambiarEstado && (
        <DialogoConfirmacion
          titulo={ninoACambiarEstado.activo ? 'Desactivar niño' : 'Activar niño'}
          mensaje={
            ninoACambiarEstado.activo
              ? `${ninoACambiarEstado.nombres} ${ninoACambiarEstado.apellidos} dejará de aparecer al armar actividades, pero se conserva todo su historial y sigue en los reportes.`
              : `${ninoACambiarEstado.nombres} ${ninoACambiarEstado.apellidos} volverá a poder elegirse en las actividades.`
          }
          textoConfirmar={ninoACambiarEstado.activo ? 'Desactivar' : 'Activar'}
          procesando={procesando}
          alConfirmar={cambiarEstado}
          alCancelar={() => setNinoACambiarEstado(null)}
        />
      )}

      {ninoAEliminar && (
        <DialogoConfirmacion
          titulo="Eliminar niño"
          mensaje={`Se eliminará la ficha de ${ninoAEliminar.nombres} ${ninoAEliminar.apellidos}. Solo es posible si se creó por error y aún no tiene asistencias, visitas ni actividades; si ya las tiene, usa Desactivar. Esta acción no se puede deshacer.`}
          textoConfirmar="Eliminar"
          esDestructiva
          procesando={procesando}
          alConfirmar={eliminar}
          alCancelar={() => setNinoAEliminar(null)}
        />
      )}
    </div>
  );
}
