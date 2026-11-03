import { useEffect, useState } from 'react';
import { Baby, BookOpen, CheckCircle2, TriangleAlert, Users } from 'lucide-react';
import { servicioPanel } from '../api/servicios';
import { formatearFecha } from '../utilidades/fechas';
import { TarjetaResumen } from '../componentes/comunes/TarjetaResumen';
import { Panel } from '../componentes/comunes/Panel';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import type { PanelGeneral as DatosPanel } from '../tipos';
import './Paneles.css';

/** Panel de Directora y Administrador: cifras de todo el sistema. */
export function PanelGeneral() {
  const [datos, setDatos] = useState<DatosPanel | null>(null);
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    servicioPanel
      .general()
      .then(setDatos)
      .catch((fallo) => setError(fallo.message))
      .finally(() => setCargando(false));
  }, []);

  if (cargando) {
    return <Cargando mensaje="Cargando el resumen…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  if (!datos) {
    return <SinDatos mensaje="No hay información para mostrar." />;
  }

  return (
    <div className="panel-pagina">
      <header className="panel-pagina__encabezado">
        <h1 className="panel-pagina__titulo">Resumen general</h1>
        <p className="panel-pagina__descripcion">
          Cifras de todos los grupos, actividades y asistencias registradas.
        </p>
      </header>

      <section className="rejilla-tarjetas" aria-label="Cifras generales">
        <TarjetaResumen titulo="Niños" valor={datos.total_Ninos} Icono={Baby} />
        <TarjetaResumen titulo="Grupos" valor={datos.total_Grupos} Icono={Users} acento="claro" />
        <TarjetaResumen
          titulo="Actividades"
          valor={datos.total_Actividades}
          Icono={BookOpen}
          acento="naranja"
        />
        <TarjetaResumen
          titulo="Asistencia"
          valor={`${datos.porcentaje_Asistencia}%`}
          Icono={CheckCircle2}
          detalle={`${datos.presentes} presentes de ${datos.total_Asistencias} registros`}
        />
        <TarjetaResumen
          titulo="Vulnerables"
          valor={datos.ninos_Vulnerables}
          Icono={TriangleAlert}
          acento="alerta"
        />
      </section>

      <div className="rejilla-paneles">
        <Panel titulo="Asistencia por grupo">
          {datos.asistencia_Por_Grupo.length === 0 ? (
            <SinDatos mensaje="Todavía no se registró asistencia en ningún grupo." />
          ) : (
            <div className="tabla-contenedor">
              <table className="tabla">
                <thead>
                  <tr>
                    <th scope="col">Grupo</th>
                    <th scope="col">Total</th>
                    <th scope="col">Presentes</th>
                    <th scope="col">Ausentes</th>
                    <th scope="col">Justificados</th>
                    <th scope="col">Asistencia</th>
                  </tr>
                </thead>
                <tbody>
                  {datos.asistencia_Por_Grupo.map((fila) => {
                    const porcentaje =
                      fila.total === 0 ? 0 : Math.round((fila.presentes / fila.total) * 100);

                    return (
                      <tr key={fila.grupo}>
                        <td data-etiqueta="Grupo">{fila.grupo}</td>
                        <td data-etiqueta="Total">{fila.total}</td>
                        <td data-etiqueta="Presentes">{fila.presentes}</td>
                        <td data-etiqueta="Ausentes">{fila.ausentes}</td>
                        <td data-etiqueta="Justificados">{fila.justificados}</td>
                        <td data-etiqueta="Asistencia">
                          <strong>{porcentaje}%</strong>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </Panel>

        <Panel titulo="Últimas actividades">
          {datos.ultimas_Actividades.length === 0 ? (
            <SinDatos mensaje="Aún no hay actividades registradas." />
          ) : (
            <ul className="lista-simple">
              {datos.ultimas_Actividades.map((actividad) => (
                <li key={actividad.id} className="lista-simple__elemento">
                  <div className="lista-simple__principal">
                    <span className="lista-simple__titulo">{actividad.nombre}</span>
                    <span className="lista-simple__secundario">
                      {actividad.fecha_Actividad
                        ? formatearFecha(actividad.fecha_Actividad)
                        : 'Sin fecha'}
                      {actividad.grupos.length > 0 && ` · ${actividad.grupos.join(', ')}`}
                    </span>
                  </div>

                  <Insignia
                    texto={actividad.tipo === 'intervencion' ? 'Intervención' : 'Normal'}
                    tono={actividad.tipo === 'intervencion' ? 'intervencion' : 'teal'}
                  />
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>

      <Panel titulo={`Niños en situación vulnerable (${datos.lista_Vulnerables.length})`}>
        {datos.lista_Vulnerables.length === 0 ? (
          <SinDatos mensaje="No hay niños marcados como vulnerables." />
        ) : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead>
                <tr>
                  <th scope="col">Código</th>
                  <th scope="col">Nombre</th>
                  <th scope="col">Grupo</th>
                  <th scope="col">Motivo</th>
                </tr>
              </thead>
              <tbody>
                {datos.lista_Vulnerables.map((nino) => (
                  <tr key={nino.id}>
                    <td data-etiqueta="Código">{nino.codigo}</td>
                    <td data-etiqueta="Nombre">{nino.nombre_Completo}</td>
                    <td data-etiqueta="Grupo">{nino.grupo}</td>
                    <td data-etiqueta="Motivo">{nino.motivo_Vulnerabilidad ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </div>
  );
}
