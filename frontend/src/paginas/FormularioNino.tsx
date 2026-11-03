import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { servicioGrupos, servicioNinos } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError } from '../componentes/comunes/Estados';
import { MapaUbicacion } from '../componentes/comunes/MapaUbicacion';
import { CampoCoordenadas } from '../componentes/comunes/CampoCoordenadas';
import {
  CampoCasilla,
  CampoSeleccion,
  CampoTexto,
  CampoTextoLargo,
} from '../componentes/comunes/Campos';
import type { Nino } from '../tipos';
import './Listados.css';

/** Todos los campos son texto mientras se edita; se convierten al enviar. */
interface FormularioNino {
  codigo: string;
  nombres: string;
  apellidos: string;
  fecha_Nacimiento: string;
  contacto: string;
  curso: string;
  colegio: string;
  grupoId: string;
  vulnerable: boolean;
  motivo_Vulnerabilidad: string;
  observaciones: string;
  fue_Al_Encuentro: boolean;
  bautizado: boolean;
  asiste_Iglesia: boolean;
  nombre_Iglesia: string;
  nombre_Celula: string;
  activo: boolean;
  latitud: number | null;
  longitud: number | null;
}

const FORMULARIO_VACIO: FormularioNino = {
  codigo: '',
  nombres: '',
  apellidos: '',
  fecha_Nacimiento: '',
  contacto: '',
  curso: '',
  colegio: '',
  grupoId: '',
  vulnerable: false,
  motivo_Vulnerabilidad: '',
  observaciones: '',
  fue_Al_Encuentro: false,
  bautizado: false,
  asiste_Iglesia: false,
  nombre_Iglesia: '',
  nombre_Celula: '',
  activo: true,
  latitud: null,
  longitud: null,
};

/** Convierte la fecha que devuelve la API al formato que espera un input de tipo date. */
function aFechaDeCampo(fechaIso: string | null): string {
  return fechaIso ? fechaIso.split('T')[0] : '';
}

export function FormularioNino() {
  const { id } = useParams();
  const navegar = useNavigate();

  const esEdicion = Boolean(id);

  const { datos: grupos } = useCargarDatos(servicioGrupos.listar);

  const [formulario, setFormulario] = useState<FormularioNino>(FORMULARIO_VACIO);
  const [cargando, setCargando] = useState(esEdicion);
  const [error, setError] = useState('');
  const [guardando, setGuardando] = useState(false);

  // En edición se traen los datos actuales del niño.
  useEffect(() => {
    if (!id) {
      return;
    }

    servicioNinos
      .obtener(Number(id))
      .then((nino: Nino) =>
        setFormulario({
          codigo: nino.codigo,
          nombres: nino.nombres,
          apellidos: nino.apellidos,
          fecha_Nacimiento: aFechaDeCampo(nino.fecha_Nacimiento),
          contacto: nino.contacto ?? '',
          curso: nino.curso ?? '',
          colegio: nino.colegio ?? '',
          grupoId: String(nino.grupoId),
          vulnerable: nino.vulnerable,
          motivo_Vulnerabilidad: nino.motivo_Vulnerabilidad ?? '',
          observaciones: nino.observaciones ?? '',
          fue_Al_Encuentro: nino.fue_Al_Encuentro,
          bautizado: nino.bautizado,
          asiste_Iglesia: nino.asiste_Iglesia,
          nombre_Iglesia: nino.nombre_Iglesia ?? '',
          nombre_Celula: nino.nombre_Celula ?? '',
          activo: nino.activo,
          latitud: nino.latitud,
          longitud: nino.longitud,
        }),
      )
      .catch((fallo: Error) => setError(fallo.message))
      .finally(() => setCargando(false));
  }, [id]);

  function actualizar<Campo extends keyof FormularioNino>(
    campo: Campo,
    valor: FormularioNino[Campo],
  ) {
    setFormulario((anterior) => ({ ...anterior, [campo]: valor }));
  }

  async function guardar() {
    setGuardando(true);
    setError('');

    // Los campos vacíos se envían como null, no como cadena vacía.
    const datosAEnviar = {
      codigo: formulario.codigo.trim(),
      nombres: formulario.nombres.trim(),
      apellidos: formulario.apellidos.trim(),
      fecha_Nacimiento: formulario.fecha_Nacimiento || null,
      contacto: formulario.contacto.trim() || null,
      curso: formulario.curso.trim() || null,
      colegio: formulario.colegio.trim() || null,
      grupoId: Number(formulario.grupoId),
      vulnerable: formulario.vulnerable,
      motivo_Vulnerabilidad: formulario.motivo_Vulnerabilidad.trim() || null,
      observaciones: formulario.observaciones.trim() || null,
      fue_Al_Encuentro: formulario.fue_Al_Encuentro,
      bautizado: formulario.bautizado,
      asiste_Iglesia: formulario.asiste_Iglesia,
      nombre_Iglesia: formulario.nombre_Iglesia.trim() || null,
      nombre_Celula: formulario.nombre_Celula.trim() || null,
      activo: formulario.activo,
      latitud: formulario.latitud,
      longitud: formulario.longitud,
    };

    try {
      if (esEdicion) {
        await servicioNinos.actualizar({ ...datosAEnviar, id: Number(id) });
      } else {
        await servicioNinos.crear(datosAEnviar);
      }

      navegar('/ninos');
    } catch (fallo) {
      setError((fallo as Error).message);
      setGuardando(false);
      // Lleva el aviso a la vista: en un formulario largo el error queda fuera de pantalla.
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  if (cargando) {
    return <Cargando mensaje="Cargando datos del niño…" />;
  }

  if (error && esEdicion && !formulario.codigo) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo={esEdicion ? 'Editar niño' : 'Registrar niño'}
        descripcion="Los maestros a cargo se toman del grupo que elijas."
        acciones={
          <Boton
            variante="contorno"
            onClick={() => navegar('/ninos')}
            icono={<ArrowLeft size={18} />}
          >
            Volver
          </Boton>
        }
      />

      {error && <Aviso tipo="error">{error}</Aviso>}

      <form
        onSubmit={(evento) => {
          evento.preventDefault();
          guardar();
        }}
      >
        <Panel titulo="Datos personales">
          <div className="formulario-rejilla">
            <CampoTexto
              etiqueta="Código"
              requerido
              value={formulario.codigo}
              onChange={(evento) => actualizar('codigo', evento.target.value)}
              ayuda="Identificador único dentro del sistema."
            />

            <CampoSeleccion
              etiqueta="Grupo"
              requerido
              textoVacio="Selecciona un grupo"
              value={formulario.grupoId}
              onChange={(evento) => actualizar('grupoId', evento.target.value)}
              opciones={(grupos ?? []).map((grupo) => ({
                valor: grupo.id,
                etiqueta: grupo.nombres_Maestros.length > 0
                  ? `${grupo.nombre} — ${grupo.nombres_Maestros.join(', ')}`
                  : grupo.nombre,
              }))}
            />

            <CampoTexto
              etiqueta="Nombres"
              requerido
              value={formulario.nombres}
              onChange={(evento) => actualizar('nombres', evento.target.value)}
            />

            <CampoTexto
              etiqueta="Apellidos"
              requerido
              value={formulario.apellidos}
              onChange={(evento) => actualizar('apellidos', evento.target.value)}
            />

            <CampoTexto
              etiqueta="Fecha de nacimiento"
              type="date"
              value={formulario.fecha_Nacimiento}
              onChange={(evento) => actualizar('fecha_Nacimiento', evento.target.value)}
              ayuda="La edad se calcula sola a partir de esta fecha."
            />

            <CampoTexto
              etiqueta="Contacto"
              value={formulario.contacto}
              onChange={(evento) => actualizar('contacto', evento.target.value)}
              ayuda="Teléfono de un familiar o responsable."
            />

            <CampoTexto
              etiqueta="Curso"
              value={formulario.curso}
              onChange={(evento) => actualizar('curso', evento.target.value)}
            />

            <CampoTexto
              etiqueta="Colegio"
              value={formulario.colegio}
              onChange={(evento) => actualizar('colegio', evento.target.value)}
            />
          </div>
        </Panel>

        <div className="separador" />

        <Panel titulo="Situación y seguimiento">
          <div className="formulario-rejilla">
            <CampoCasilla
              etiqueta="Está en situación vulnerable"
              checked={formulario.vulnerable}
              onChange={(evento) => actualizar('vulnerable', evento.target.checked)}
            />

            <CampoCasilla
              etiqueta="Niño activo"
              checked={formulario.activo}
              onChange={(evento) => actualizar('activo', evento.target.checked)}
            />

            {/* El motivo solo tiene sentido si está marcado como vulnerable. */}
            {formulario.vulnerable && (
              <div className="formulario-rejilla__completo">
                <CampoTextoLargo
                  etiqueta="Motivo de vulnerabilidad"
                  value={formulario.motivo_Vulnerabilidad}
                  onChange={(evento) =>
                    actualizar('motivo_Vulnerabilidad', evento.target.value)
                  }
                />
              </div>
            )}

            <div className="formulario-rejilla__completo">
              <CampoTextoLargo
                etiqueta="Observaciones"
                value={formulario.observaciones}
                onChange={(evento) => actualizar('observaciones', evento.target.value)}
              />
            </div>
          </div>
        </Panel>

        <div className="separador" />

        <Panel titulo="Vida en la iglesia">
          <div className="formulario-rejilla">
            <CampoCasilla
              etiqueta="Fue al encuentro"
              checked={formulario.fue_Al_Encuentro}
              onChange={(evento) => actualizar('fue_Al_Encuentro', evento.target.checked)}
            />

            <CampoCasilla
              etiqueta="Está bautizado"
              checked={formulario.bautizado}
              onChange={(evento) => actualizar('bautizado', evento.target.checked)}
            />

            <CampoCasilla
              etiqueta="Asiste a una iglesia"
              checked={formulario.asiste_Iglesia}
              onChange={(evento) => actualizar('asiste_Iglesia', evento.target.checked)}
            />

            {/* Estos datos solo aplican si asiste a una iglesia. */}
            {formulario.asiste_Iglesia && (
              <>
                <CampoTexto
                  etiqueta="Nombre de la iglesia"
                  value={formulario.nombre_Iglesia}
                  onChange={(evento) => actualizar('nombre_Iglesia', evento.target.value)}
                />

                <CampoTexto
                  etiqueta="Nombre de la célula"
                  value={formulario.nombre_Celula}
                  onChange={(evento) => actualizar('nombre_Celula', evento.target.value)}
                />
              </>
            )}
          </div>
        </Panel>

        <div className="separador" />

        <Panel titulo="Ubicación de la vivienda">
          {/*
            Dos formas de indicar la ubicación: pegar las coordenadas o mover el
            marcador. Ambas escriben los mismos campos, así que se mantienen
            sincronizadas en los dos sentidos.
          */}
          <CampoCoordenadas
            latitud={formulario.latitud}
            longitud={formulario.longitud}
            alCambiar={(latitud, longitud) => {
              actualizar('latitud', latitud);
              actualizar('longitud', longitud);
            }}
          />

          <p className="texto-ayuda">
            O toca el mapa y arrastra el marcador para señalar dónde vive.
          </p>

          <MapaUbicacion
            latitud={formulario.latitud}
            longitud={formulario.longitud}
            alCambiarUbicacion={(latitud, longitud) => {
              actualizar('latitud', latitud);
              actualizar('longitud', longitud);
            }}
          />

        </Panel>

        <div className="formulario-acciones">
          <Boton type="submit" cargando={guardando} icono={<Save size={18} />}>
            {esEdicion ? 'Guardar cambios' : 'Registrar niño'}
          </Boton>

          <Boton variante="contorno" type="button" onClick={() => navegar('/ninos')}>
            Cancelar
          </Boton>
        </div>
      </form>
    </div>
  );
}
