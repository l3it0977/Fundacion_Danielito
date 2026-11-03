import { useState } from 'react';
import { servicioBitacora } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { useRetraso } from '../hooks/useRetraso';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { Paginacion } from '../componentes/comunes/Paginacion';
import { CampoSeleccion, CampoTexto } from '../componentes/comunes/Campos';
import { finDelDiaEnUtc, inicioDelDiaEnUtc } from '../utilidades/fechas';
import {
  NOMBRES_DE_ENTIDAD,
  etiquetaDeCampo,
  nombreDeEntidad,
  textoDeValor,
} from '../utilidades/bitacora';
import type { RegistroBitacora } from '../tipos';
import './Listados.css';
import './Bitacora.css';

const REGISTROS_POR_PAGINA = 25;

const ACCIONES = ['Creado', 'Modificado', 'Eliminado'];

function tonoDeLaAccion(accion: string) {
  if (accion === 'Creado') return 'exito';
  if (accion === 'Eliminado') return 'error';
  return 'advertencia';
}

/** Fecha y hora en la zona de quien mira: el servidor guarda UTC. */
function fechaYHora(fechaUtc: string): string {
  return new Date(fechaUtc).toLocaleString('es-BO', { dateStyle: 'short', timeStyle: 'short' });
}

function Detalle({ registro }: { registro: RegistroBitacora }) {
  if (registro.cambios.length === 0) {
    return <span className="tabla__secundario">—</span>;
  }

  return (
    <details className="bitacora__detalle">
      <summary>
        {registro.cambios.length} {registro.cambios.length === 1 ? 'campo' : 'campos'}
      </summary>

      <ul className="bitacora__cambios">
        {registro.cambios.map((cambio) => (
          <li key={cambio.campo}>
            <strong>{etiquetaDeCampo(cambio.campo)}:</strong>{' '}
            <span className="bitacora__antes">{textoDeValor(cambio.antes)}</span>
            {' → '}
            <span className="bitacora__despues">{textoDeValor(cambio.despues)}</span>
          </li>
        ))}
      </ul>
    </details>
  );
}

export function Bitacora() {
  const [entidad, setEntidad] = useState('');
  const [accion, setAccion] = useState('');
  const [desde, setDesde] = useState('');
  const [hasta, setHasta] = useState('');
  const [busqueda, setBusqueda] = useState('');
  const textoBuscado = useRetraso(busqueda.trim());
  const [pagina, setPagina] = useState(1);

  const { datos: resultado, cargando, error } = useCargarDatos(
    () =>
      servicioBitacora.buscar({
        entidad,
        accion,
        texto: textoBuscado,
        desde: desde ? inicioDelDiaEnUtc(desde) : undefined,
        hasta: hasta ? finDelDiaEnUtc(hasta) : undefined,
        pagina,
        tamano: REGISTROS_POR_PAGINA,
      }),
    [entidad, accion, textoBuscado, desde, hasta, pagina],
  );

  // Cambiar cualquier filtro vuelve a la primera página.
  function conFiltro<T>(cambiar: (valor: T) => void) {
    return (valor: T) => {
      cambiar(valor);
      setPagina(1);
    };
  }

  if (cargando && !resultado) {
    return <Cargando mensaje="Cargando la bitácora…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Bitácora"
        descripcion="Registro de quién creó, cambió o eliminó niños, grupos, actividades, visitas y cuentas."
      />

      <div className="filtros">
        <div className="filtros__campo">
          <CampoSeleccion
            etiqueta="Qué"
            textoVacio="Todo"
            value={entidad}
            onChange={(evento) => conFiltro(setEntidad)(evento.target.value)}
            opciones={Object.entries(NOMBRES_DE_ENTIDAD).map(([valor, etiqueta]) => ({ valor, etiqueta }))}
          />
        </div>

        <div className="filtros__campo">
          <CampoSeleccion
            etiqueta="Acción"
            textoVacio="Todas"
            value={accion}
            onChange={(evento) => conFiltro(setAccion)(evento.target.value)}
            opciones={ACCIONES.map((valor) => ({ valor, etiqueta: valor }))}
          />
        </div>

        <div className="filtros__campo">
          <CampoTexto
            etiqueta="Desde"
            type="date"
            value={desde}
            max={hasta || undefined}
            onChange={(evento) => conFiltro(setDesde)(evento.target.value)}
          />
        </div>

        <div className="filtros__campo">
          <CampoTexto
            etiqueta="Hasta"
            type="date"
            value={hasta}
            min={desde || undefined}
            onChange={(evento) => conFiltro(setHasta)(evento.target.value)}
          />
        </div>

        <div className="filtros__campo">
          <CampoTexto
            etiqueta="Buscar"
            type="search"
            placeholder="Nombre o código"
            value={busqueda}
            onChange={(evento) => conFiltro(setBusqueda)(evento.target.value)}
          />
        </div>
      </div>

      <Panel titulo={`Registros (${resultado?.total ?? 0})`}>
        <div className={cargando ? 'lista-actualizando' : undefined} aria-busy={cargando}>
          {!resultado || resultado.items.length === 0 ? (
            <SinDatos mensaje="No hay registros con esos filtros." />
          ) : (
            <div className="tabla-contenedor">
              <table className="tabla">
                <thead>
                  <tr>
                    <th scope="col">Fecha</th>
                    <th scope="col">Quién</th>
                    <th scope="col">Qué</th>
                    <th scope="col">Acción</th>
                    <th scope="col">Cambios</th>
                  </tr>
                </thead>
                <tbody>
                  {resultado.items.map((registro) => (
                    <tr key={registro.id}>
                      <td data-etiqueta="Fecha">{fechaYHora(registro.fecha)}</td>
                      <td data-etiqueta="Quién">{registro.usuario}</td>
                      <td data-etiqueta="Qué">
                        <Insignia texto={nombreDeEntidad(registro.entidad)} tono="teal" />
                        <span className="tabla__secundario">{registro.descripcion}</span>
                      </td>
                      <td data-etiqueta="Acción">
                        <Insignia texto={registro.accion} tono={tonoDeLaAccion(registro.accion)} />
                      </td>
                      <td data-etiqueta="Cambios">
                        <Detalle registro={registro} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </Panel>

      {resultado && (
        <Paginacion
          pagina={resultado.pagina}
          totalPaginas={resultado.totalPaginas}
          resumen={`${resultado.total} registros`}
          alCambiar={setPagina}
          ocupado={cargando}
        />
      )}
    </div>
  );
}
