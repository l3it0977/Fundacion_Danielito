import { useState } from 'react';
import { FileDown, FileSpreadsheet } from 'lucide-react';
import { servicioGrupos, servicioReportes } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { CampoSeleccion } from '../componentes/comunes/Campos';
import './Listados.css';

const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

/** Los últimos cinco años, para no ofrecer un rango innecesariamente largo. */
function aniosDisponibles(): number[] {
  const anioActual = new Date().getFullYear();
  return [0, 1, 2, 3, 4].map((resta) => anioActual - resta);
}

export function Reportes() {
  const { datos: grupos } = useCargarDatos(servicioGrupos.listar);

  const hoy = new Date();
  // Cadena vacía = "todos": el servidor la entiende como sin filtro.
  const [grupoId, setGrupoId] = useState('');
  const [mes, setMes] = useState(String(hoy.getMonth() + 1));
  const [anio, setAnio] = useState(String(hoy.getFullYear()));

  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [descargando, setDescargando] = useState('');

  const filtros = {
    grupoId: grupoId ? Number(grupoId) : null,
    mes: mes ? Number(mes) : null,
    anio: Number(anio),
  };

  const nombreGrupo = (grupos ?? []).find((grupo) => String(grupo.id) === grupoId)?.nombre;
  const textoGrupo = nombreGrupo ?? 'todos los grupos de edad';
  const textoPeriodo = mes ? `${MESES[Number(mes) - 1]} de ${anio}` : `todos los meses de ${anio}`;

  async function descargar(nombreReporte: string, accion: () => Promise<void>) {
    setDescargando(nombreReporte);
    setAviso(null);

    try {
      await accion();
      setAviso({ tipo: 'exito', texto: 'El archivo se descargó correctamente.' });
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setDescargando('');
    }
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Reportes"
        descripcion="Los archivos se generan en PDF con la plantilla de la fundación y se descargan al momento."
      />

      {aviso && (
        <Aviso tipo={aviso.tipo} alCerrar={() => setAviso(null)}>
          {aviso.texto}
        </Aviso>
      )}

      {/* Un solo juego de filtros para ambos reportes, para no repetir la elección. */}
      <Panel titulo="¿Qué quieres reportar?">
        <p className="texto-ayuda">
          Deja “Todos” para incluir todas las edades o todos los meses en un solo reporte.
        </p>

        <div className="filtros">
          <div className="filtros__campo">
            <CampoSeleccion
              etiqueta="Grupo de edad"
              textoVacio="Todos los grupos"
              value={grupoId}
              onChange={(evento) => setGrupoId(evento.target.value)}
              opciones={(grupos ?? []).map((grupo) => ({
                valor: grupo.id,
                etiqueta: grupo.nombre,
              }))}
            />
          </div>

          <div className="filtros__campo">
            <CampoSeleccion
              etiqueta="Mes"
              textoVacio="Todos los meses"
              value={mes}
              onChange={(evento) => setMes(evento.target.value)}
              opciones={MESES.map((nombre, indice) => ({
                valor: indice + 1,
                etiqueta: nombre,
              }))}
            />
          </div>

          <div className="filtros__campo">
            <CampoSeleccion
              etiqueta="Año"
              value={anio}
              onChange={(evento) => setAnio(evento.target.value)}
              opciones={aniosDisponibles().map((valor) => ({
                valor,
                etiqueta: String(valor),
              }))}
            />
          </div>
        </div>
      </Panel>

      <Panel titulo="Asistencia total y por grupo">
        <p className="texto-ayuda">
          Resumen general con el porcentaje de asistencia, el desglose por grupo
          {mes ? ' y el detalle día por día' : ', por mes y por niño'}.
          <strong> Se generará para: {textoGrupo}, {textoPeriodo}.</strong>
        </p>

        <div className="acciones-reporte">
          <Boton
            cargando={descargando === 'asistencia'}
            icono={<FileDown size={18} />}
            onClick={() =>
              descargar('asistencia', () => servicioReportes.asistencia(filtros))
            }
          >
            Descargar PDF
          </Boton>

          {/* Mismas cifras en celdas, con filtros y fechas reales, para analizarlas o graficarlas. */}
          <Boton
            variante="contorno"
            cargando={descargando === 'asistencia-excel'}
            icono={<FileSpreadsheet size={18} />}
            onClick={() =>
              descargar('asistencia-excel', () => servicioReportes.asistenciaExcel(filtros))
            }
          >
            Descargar Excel
          </Boton>
        </div>
      </Panel>

      <Panel titulo="Actividades">
        <p className="texto-ayuda">
          Listado de las actividades realizadas, con sus grupos y participantes.
          <strong> Se generará para: {textoGrupo}, {textoPeriodo}.</strong>
        </p>

        <Boton
          variante="naranja"
          cargando={descargando === 'actividades'}
          icono={<FileDown size={18} />}
          onClick={() =>
            descargar('actividades', () => servicioReportes.actividades(filtros))
          }
        >
          Descargar PDF
        </Boton>
      </Panel>

      <Aviso tipo="informacion">
        El reporte de asistencia por actividad se descarga desde la pantalla de Actividades,
        con el botón PDF de cada fila.
      </Aviso>
    </div>
  );
}
