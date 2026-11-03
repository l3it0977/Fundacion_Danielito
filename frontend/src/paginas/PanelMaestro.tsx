import { useEffect, useState } from 'react';
import { Baby, BookOpen, Cake, CheckCircle2, Trophy } from 'lucide-react';
import { servicioPanel } from '../api/servicios';
import { TarjetaResumen } from '../componentes/comunes/TarjetaResumen';
import { Panel } from '../componentes/comunes/Panel';
import { Cargando, MensajeError, SinDatos } from '../componentes/comunes/Estados';
import { useAutenticacion } from '../contextos/ContextoAutenticacion';
import type { PanelMaestro as DatosPanel } from '../tipos';
import './Paneles.css';

/** Panel del Maestro: solo los niños de los grupos que tiene a cargo. */
export function PanelMaestro() {
  const { usuario } = useAutenticacion();

  const [datos, setDatos] = useState<DatosPanel | null>(null);
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    servicioPanel
      .maestro()
      .then(setDatos)
      .catch((fallo) => setError(fallo.message))
      .finally(() => setCargando(false));
  }, []);

  if (cargando) {
    return <Cargando mensaje="Cargando tu resumen…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  if (!datos) {
    return <SinDatos mensaje="No hay información para mostrar." />;
  }

  const { asistencia_Del_Mes: asistenciaDelMes } = datos;

  return (
    <div className="panel-pagina">
      <header className="panel-pagina__encabezado">
        <h1 className="panel-pagina__titulo">Hola, {usuario?.nombre}</h1>
        <p className="panel-pagina__descripcion">
          Resumen de los niños de los grupos que tienes a cargo.
        </p>
      </header>

      <section className="rejilla-tarjetas" aria-label="Tus cifras">
        <TarjetaResumen titulo="Mis niños" valor={datos.total_Mis_Ninos} Icono={Baby} />
        <TarjetaResumen
          titulo="Mis actividades"
          valor={datos.total_Mis_Actividades}
          Icono={BookOpen}
          acento="naranja"
        />
        <TarjetaResumen
          titulo="Asistencia del mes"
          valor={`${asistenciaDelMes.porcentaje_Asistencia}%`}
          Icono={CheckCircle2}
          acento="claro"
          detalle={`${asistenciaDelMes.presentes} de ${asistenciaDelMes.total} registros`}
        />
      </section>

      <div className="rejilla-paneles">
        <Panel titulo="Cumpleaños próximos">
          {datos.cumpleanos_Cercanos.length === 0 ? (
            <SinDatos mensaje="Ningún niño cumple años en los próximos 30 días." />
          ) : (
            <ul className="lista-simple">
              {datos.cumpleanos_Cercanos.map((cumpleanos) => (
                <li key={cumpleanos.ninoId} className="lista-simple__elemento">
                  <div className="lista-simple__principal">
                    <span className="lista-simple__titulo">
                      <Cake size={16} aria-hidden="true" className="lista-simple__icono" />
                      {cumpleanos.nombre_Completo}
                    </span>
                    <span className="lista-simple__secundario">
                      Cumple {cumpleanos.edad_Que_Cumple} años
                    </span>
                  </div>

                  <span className="destacado-dias">
                    {cumpleanos.dias_Restantes === 0
                      ? '¡Hoy!'
                      : `en ${cumpleanos.dias_Restantes} día${cumpleanos.dias_Restantes === 1 ? '' : 's'}`}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Panel>

        <Panel titulo="Mayor asistencia">
          {datos.mis_Mas_Asistentes.length === 0 ? (
            <SinDatos mensaje="Todavía no hay asistencias registradas." />
          ) : (
            <ul className="lista-simple">
              {datos.mis_Mas_Asistentes.map((asistente, posicion) => (
                <li key={asistente.ninoId} className="lista-simple__elemento">
                  <div className="lista-simple__principal">
                    <span className="lista-simple__titulo">
                      {posicion === 0 && (
                        <Trophy
                          size={16}
                          aria-hidden="true"
                          className="lista-simple__icono lista-simple__icono--trofeo"
                        />
                      )}
                      {asistente.nombre_Completo}
                    </span>
                  </div>

                  <span className="destacado-dias">
                    {asistente.total_Asistencias}{' '}
                    {asistente.total_Asistencias === 1 ? 'asistencia' : 'asistencias'}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>

      <Panel titulo="Detalle de asistencia del mes">
        <div className="rejilla-mini">
          <div className="mini-cifra">
            <span className="mini-cifra__valor">{asistenciaDelMes.total}</span>
            <span className="mini-cifra__etiqueta">Registros</span>
          </div>
          <div className="mini-cifra mini-cifra--exito">
            <span className="mini-cifra__valor">{asistenciaDelMes.presentes}</span>
            <span className="mini-cifra__etiqueta">Presentes</span>
          </div>
          <div className="mini-cifra mini-cifra--error">
            <span className="mini-cifra__valor">{asistenciaDelMes.ausentes}</span>
            <span className="mini-cifra__etiqueta">Ausentes</span>
          </div>
          <div className="mini-cifra mini-cifra--advertencia">
            <span className="mini-cifra__valor">{asistenciaDelMes.justificados}</span>
            <span className="mini-cifra__etiqueta">Justificados</span>
          </div>
        </div>
      </Panel>
    </div>
  );
}
