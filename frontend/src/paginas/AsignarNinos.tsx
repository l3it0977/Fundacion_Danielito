import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { servicioActividades, servicioNinos } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { usePermisosActividad } from '../hooks/usePermisosActividad';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError } from '../componentes/comunes/Estados';
import { SelectorNinos } from '../componentes/comunes/SelectorNinos';
import './Listados.css';

/**
 * Cambia los participantes de una actividad ya creada.
 * Se puede elegir a cualquier niño, de cualquier grupo: la actividad no está
 * limitada a un conjunto de grupos.
 */
export function AsignarNinos() {
  const { id } = useParams();
  const navegar = useNavigate();
  const actividadId = Number(id);
  const { puedeGestionar } = usePermisosActividad();

  const {
    datos: actividad,
    cargando: cargandoActividad,
    error: errorActividad,
    recargar,
  } = useCargarDatos(() => servicioActividades.obtener(actividadId), [actividadId]);

  const { datos: ninosDisponibles, cargando: cargandoNinos } = useCargarDatos(
    servicioNinos.paraSeleccion,
  );

  const [seleccionados, setSeleccionados] = useState<number[]>([]);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const [guardando, setGuardando] = useState(false);

  // Al cargar la actividad se marcan los niños que ya participaban.
  useEffect(() => {
    if (actividad) {
      setSeleccionados(actividad.ninos);
    }
  }, [actividad]);

  async function guardar() {
    setGuardando(true);
    setAviso(null);

    try {
      await servicioActividades.asignarNinos(actividadId, seleccionados);

      setAviso({
        tipo: 'exito',
        texto: 'Participantes actualizados. La lista de asistencia se ajustó sola.',
      });

      // Se recarga para reflejar los grupos, que se deducen de los participantes.
      recargar();
    } catch (fallo) {
      setAviso({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardando(false);
    }
  }

  if (cargandoActividad || cargandoNinos) {
    return <Cargando mensaje="Cargando actividad…" />;
  }

  if (errorActividad) {
    return <MensajeError mensaje={errorActividad} />;
  }

  if (!actividad) {
    return <MensajeError mensaje="No se encontró la actividad." />;
  }

  // Si un maestro llega aquí escribiendo la dirección de una actividad ajena, se le
  // explica de entrada, en vez de dejarlo elegir niños y fallar recién al guardar.
  if (!puedeGestionar(actividad)) {
    return (
      <MensajeError mensaje="Solo puedes cambiar los participantes de las actividades que tú creaste." />
    );
  }

  return (
    <div className="pagina">
      <EncabezadoPagina
        titulo="Participantes"
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

      <Panel titulo="Elegir niños">
        <p className="texto-ayuda">
          Puedes elegir niños de cualquier grupo. Al guardar, quienes se agreguen
          entran a la lista de asistencia y quienes salgan dejan de aparecer en ella,
          salvo que ya se les haya pasado lista.
        </p>

        <SelectorNinos
          ninosDisponibles={ninosDisponibles ?? []}
          seleccionados={seleccionados}
          alCambiarSeleccion={setSeleccionados}
        />

        <div className="formulario-acciones">
          <Boton
            onClick={guardar}
            cargando={guardando}
            disabled={seleccionados.length === 0}
            icono={<Save size={18} />}
          >
            Guardar participantes
          </Boton>
        </div>
      </Panel>
    </div>
  );
}
