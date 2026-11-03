import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil } from 'lucide-react';
import { servicioNinos } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { useAutenticacion } from '../contextos/ContextoAutenticacion';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Cargando, MensajeError } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { MapaUbicacion } from '../componentes/comunes/MapaUbicacion';
import './Listados.css';

/** Muestra un dato con su etiqueta; los vacíos se marcan con un guion. */
function Dato({ etiqueta, valor }: { etiqueta: string; valor: string | number | null }) {
  return (
    <div className="detalle-dato">
      <span className="detalle-dato__etiqueta">{etiqueta}</span>
      <span className="detalle-dato__valor">{valor === null || valor === '' ? '—' : valor}</span>
    </div>
  );
}

function Si({ valor }: { valor: boolean }) {
  return <Insignia texto={valor ? 'Sí' : 'No'} tono={valor ? 'exito' : 'neutro'} />;
}

export function DetalleNino() {
  const { id } = useParams();
  const navegar = useNavigate();
  const { tieneRol } = useAutenticacion();

  const {
    datos: nino,
    cargando,
    error,
  } = useCargarDatos(() => servicioNinos.obtener(Number(id)), [id]);

  if (cargando) {
    return <Cargando mensaje="Cargando ficha…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  if (!nino) {
    return <MensajeError mensaje="No se encontró el niño." />;
  }

  const fechaNacimiento = nino.fecha_Nacimiento
    ? new Date(nino.fecha_Nacimiento).toLocaleDateString('es-BO')
    : null;

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo={`${nino.nombres} ${nino.apellidos}`}
        descripcion={`Código ${nino.codigo} · ${nino.nombre_Grupo}`}
        acciones={
          <>
            <Boton
              variante="contorno"
              onClick={() => navegar('/ninos')}
              icono={<ArrowLeft size={18} />}
            >
              Volver
            </Boton>

            {tieneRol('Directora', 'Administrador') && (
              <Link
                to={`/ninos/${nino.id}/editar`}
                className="enlace-boton enlace-boton--naranja enlace-boton--grande"
              >
                <Pencil size={18} aria-hidden="true" />
                Editar
              </Link>
            )}
          </>
        }
      />

      <Panel titulo="Datos personales">
        <div className="detalle-rejilla">
          <Dato etiqueta="Código" valor={nino.codigo} />
          <Dato etiqueta="Nombres" valor={nino.nombres} />
          <Dato etiqueta="Apellidos" valor={nino.apellidos} />
          <Dato etiqueta="Fecha de nacimiento" valor={fechaNacimiento} />
          <Dato etiqueta="Edad" valor={nino.edad != null ? `${nino.edad} años` : null} />
          <Dato etiqueta="Contacto" valor={nino.contacto} />
          <Dato etiqueta="Curso" valor={nino.curso} />
          <Dato etiqueta="Colegio" valor={nino.colegio} />
          <Dato etiqueta="Grupo" valor={nino.nombre_Grupo} />
          <Dato
            etiqueta={nino.nombres_Maestros.length === 1 ? 'Maestro a cargo' : 'Maestros a cargo'}
            valor={nino.nombres_Maestros.join(', ')}
          />

          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Estado</span>
            <span className="detalle-dato__valor">
              <Insignia
                texto={nino.activo ? 'Activo' : 'Inactivo'}
                tono={nino.activo ? 'exito' : 'neutro'}
              />
            </span>
          </div>
        </div>
      </Panel>

      <Panel titulo="Situación y seguimiento">
        <div className="detalle-rejilla">
          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">En situación vulnerable</span>
            <span className="detalle-dato__valor">
              <Insignia
                texto={nino.vulnerable ? 'Sí' : 'No'}
                tono={nino.vulnerable ? 'advertencia' : 'neutro'}
              />
            </span>
          </div>

          {nino.vulnerable && (
            <Dato etiqueta="Motivo" valor={nino.motivo_Vulnerabilidad} />
          )}

          <Dato etiqueta="Observaciones" valor={nino.observaciones} />
        </div>
      </Panel>

      <Panel titulo="Vida en la iglesia">
        <div className="detalle-rejilla">
          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Fue al encuentro</span>
            <span className="detalle-dato__valor">
              <Si valor={nino.fue_Al_Encuentro} />
            </span>
          </div>

          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Bautizado</span>
            <span className="detalle-dato__valor">
              <Si valor={nino.bautizado} />
            </span>
          </div>

          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Asiste a una iglesia</span>
            <span className="detalle-dato__valor">
              <Si valor={nino.asiste_Iglesia} />
            </span>
          </div>

          {nino.asiste_Iglesia && (
            <>
              <Dato etiqueta="Iglesia" valor={nino.nombre_Iglesia} />
              <Dato etiqueta="Célula" valor={nino.nombre_Celula} />
            </>
          )}
        </div>
      </Panel>

      {nino.latitud != null && nino.longitud != null && (
        <Panel titulo="Ubicación de la vivienda">
          <MapaUbicacion latitud={nino.latitud} longitud={nino.longitud} />
        </Panel>
      )}
    </div>
  );
}
