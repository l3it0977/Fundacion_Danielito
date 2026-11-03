import { useRef, useState } from 'react';
import { Download, FileSpreadsheet, Upload } from 'lucide-react';
import { servicioImportacion } from '../api/servicios';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import './Importar.css';

interface Resultado {
  filas_Leidas: number;
  importados: number;
  rechazados: number;
  errores: string[];
}

export function Importar() {
  const campoArchivo = useRef<HTMLInputElement>(null);

  const [archivo, setArchivo] = useState<File | null>(null);
  const [resultado, setResultado] = useState<Resultado | null>(null);
  const [error, setError] = useState('');
  const [procesando, setProcesando] = useState(false);

  async function importar() {
    if (!archivo) {
      return;
    }

    setProcesando(true);
    setError('');
    setResultado(null);

    try {
      setResultado(await servicioImportacion.importarNinos(archivo));
      setArchivo(null);

      // Se limpia el campo para poder volver a elegir el mismo archivo corregido.
      if (campoArchivo.current) {
        campoArchivo.current.value = '';
      }
    } catch (fallo) {
      setError((fallo as Error).message);
    } finally {
      setProcesando(false);
    }
  }

  async function descargarPlantilla() {
    setError('');

    try {
      await servicioImportacion.descargarPlantilla();
    } catch (fallo) {
      setError((fallo as Error).message);
    }
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Importar niños"
        descripcion="Carga varios niños a la vez desde un archivo de Excel."
      />

      <Panel titulo="Antes de empezar">
        <ol className="pasos">
          <li className="pasos__item">
            Descarga la plantilla: trae las columnas con los nombres exactos que el sistema espera.
          </li>
          <li className="pasos__item">
            Llena una fila por niño. El <strong>código</strong>, los <strong>nombres</strong>,
            los <strong>apellidos</strong> y el <strong>grupo</strong> son obligatorios.
          </li>
          <li className="pasos__item">
            El grupo se escribe por su nombre, tal como aparece en la pantalla de Grupos.
          </li>
          <li className="pasos__item">
            El maestro no se carga: se toma del grupo al que pertenece cada niño.
          </li>
        </ol>

        <div className="formulario-acciones">
          <Boton variante="contorno" onClick={descargarPlantilla} icono={<Download size={18} />}>
            Descargar plantilla
          </Boton>
        </div>
      </Panel>

      <Panel titulo="Subir archivo">
        {error && <Aviso tipo="error">{error}</Aviso>}

        <label className="zona-archivo">
          <FileSpreadsheet size={36} aria-hidden="true" className="zona-archivo__icono" />

          <span className="zona-archivo__titulo">
            {archivo ? archivo.name : 'Elige un archivo .xlsx'}
          </span>

          <span className="zona-archivo__ayuda">Tamaño máximo: 5 MB</span>

          <input
            ref={campoArchivo}
            type="file"
            accept=".xlsx"
            className="zona-archivo__campo"
            onChange={(evento) => {
              setArchivo(evento.target.files?.[0] ?? null);
              setResultado(null);
              setError('');
            }}
          />
        </label>

        <div className="formulario-acciones">
          <Boton
            onClick={importar}
            disabled={!archivo}
            cargando={procesando}
            icono={<Upload size={18} />}
          >
            Importar
          </Boton>

          {archivo && (
            <Boton
              variante="contorno"
              onClick={() => {
                setArchivo(null);
                if (campoArchivo.current) {
                  campoArchivo.current.value = '';
                }
              }}
            >
              Quitar archivo
            </Boton>
          )}
        </div>
      </Panel>

      {resultado && (
        <Panel titulo="Resultado de la importación">
          <div className="rejilla-mini">
            <div className="mini-cifra">
              <span className="mini-cifra__valor">{resultado.filas_Leidas}</span>
              <span className="mini-cifra__etiqueta">Filas leídas</span>
            </div>

            <div className="mini-cifra mini-cifra--exito">
              <span className="mini-cifra__valor">{resultado.importados}</span>
              <span className="mini-cifra__etiqueta">Importados</span>
            </div>

            <div className="mini-cifra mini-cifra--error">
              <span className="mini-cifra__valor">{resultado.rechazados}</span>
              <span className="mini-cifra__etiqueta">Rechazados</span>
            </div>
          </div>

          {/*
            Cada fila rechazada se informa con su número y su motivo, para poder
            corregir el archivo y volver a subirlo.
          */}
          {resultado.errores.length > 0 && (
            <div className="lista-errores">
              <h3 className="lista-errores__titulo">Filas que no se importaron</h3>

              <ul className="lista-errores__lista">
                {resultado.errores.map((mensaje) => (
                  <li key={mensaje} className="lista-errores__item">
                    {mensaje}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {resultado.rechazados === 0 && resultado.importados > 0 && (
            <Aviso tipo="exito">Todas las filas se importaron sin problemas.</Aviso>
          )}
        </Panel>
      )}
    </div>
  );
}
