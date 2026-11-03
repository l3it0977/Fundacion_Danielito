import { useMemo, useState } from 'react';
import { ChevronDown, Search, Users } from 'lucide-react';
import { CampoTexto } from './Campos';
import { Boton } from './Boton';
import { Insignia } from './Insignia';
import { SinDatos } from './Estados';
import type { NinoSeleccion } from '../../tipos';
import './SelectorNinos.css';

interface Props {
  ninosDisponibles: NinoSeleccion[];
  /** Ids de los niños actualmente elegidos. */
  seleccionados: number[];
  alCambiarSeleccion: (seleccionados: number[]) => void;
}

/**
 * Permite elegir participantes de cualquier grupo, a voluntad.
 * Los niños se muestran agrupados por grupo de edad y cada grupo se puede marcar
 * entero de una vez: así se arma rápido una actividad que cruza varias edades,
 * por ejemplo cinco niños de 6 a 8 años y diez de 9 a 11.
 */
export function SelectorNinos({ ninosDisponibles, seleccionados, alCambiarSeleccion }: Props) {
  const [busqueda, setBusqueda] = useState('');

  // Se agrupa y filtra sobre la lista ya cargada, sin volver a consultar la API.
  const gruposConSusNinos = useMemo(() => {
    const textoBuscado = busqueda.trim().toLowerCase();

    const coincidentes = textoBuscado
      ? ninosDisponibles.filter((nino) =>
          `${nino.nombre_Completo} ${nino.codigo}`.toLowerCase().includes(textoBuscado),
        )
      : ninosDisponibles;

    const porGrupo = new Map<string, NinoSeleccion[]>();

    for (const nino of coincidentes) {
      const nombreGrupo = nino.nombre_Grupo || 'Sin grupo';
      porGrupo.set(nombreGrupo, [...(porGrupo.get(nombreGrupo) ?? []), nino]);
    }

    return [...porGrupo.entries()].sort((unGrupo, otroGrupo) =>
      unGrupo[0].localeCompare(otroGrupo[0]),
    );
  }, [ninosDisponibles, busqueda]);

  function alternarNino(ninoId: number) {
    alCambiarSeleccion(
      seleccionados.includes(ninoId)
        ? seleccionados.filter((id) => id !== ninoId)
        : [...seleccionados, ninoId],
    );
  }

  /** Marca o desmarca de una vez a todos los niños de un grupo. */
  function alternarGrupoCompleto(ninosDelGrupo: NinoSeleccion[]) {
    const idsDelGrupo = ninosDelGrupo.map((nino) => nino.id);
    const estanTodos = idsDelGrupo.every((id) => seleccionados.includes(id));

    alCambiarSeleccion(
      estanTodos
        ? seleccionados.filter((id) => !idsDelGrupo.includes(id))
        : [...new Set([...seleccionados, ...idsDelGrupo])],
    );
  }

  if (ninosDisponibles.length === 0) {
    return <SinDatos mensaje="No hay niños registrados para elegir." />;
  }

  return (
    <div className="selector-ninos">
      <div className="selector-ninos__barra">
        <div className="selector-ninos__buscador">
          <CampoTexto
            etiqueta="Buscar niño"
            type="search"
            placeholder="Nombre o código"
            value={busqueda}
            onChange={(evento) => setBusqueda(evento.target.value)}
          />
        </div>

        <p className="selector-ninos__conteo">
          <Users size={16} aria-hidden="true" />
          {seleccionados.length} elegido{seleccionados.length === 1 ? '' : 's'}
        </p>

        {seleccionados.length > 0 && (
          <Boton
            type="button"
            tamano="chico"
            variante="contorno"
            onClick={() => alCambiarSeleccion([])}
          >
            Limpiar todo
          </Boton>
        )}
      </div>

      {gruposConSusNinos.length === 0 ? (
        <SinDatos mensaje={`Ningún niño coincide con "${busqueda}".`} />
      ) : (
        gruposConSusNinos.map(([nombreGrupo, ninosDelGrupo]) => {
          const elegidosDelGrupo = ninosDelGrupo.filter((nino) =>
            seleccionados.includes(nino.id),
          ).length;

          return (
            <details key={nombreGrupo} className="grupo-seleccion" open>
              <summary className="grupo-seleccion__resumen">
                <span className="grupo-seleccion__titulo">
                  {nombreGrupo}
                  <Insignia
                    texto={`${elegidosDelGrupo} de ${ninosDelGrupo.length}`}
                    tono={elegidosDelGrupo > 0 ? 'teal' : 'neutro'}
                  />
                </span>

                <span className="grupo-seleccion__acciones">
                  {/*
                    Es un span y no un botón porque está dentro de <summary>:
                    un botón anidado haría que el teclado abra y cierre el grupo
                    al mismo tiempo que marca los niños.
                  */}
                  <span
                    role="button"
                    tabIndex={0}
                    className="grupo-seleccion__marcar"
                    onClick={(evento) => {
                      evento.preventDefault();
                      alternarGrupoCompleto(ninosDelGrupo);
                    }}
                    onKeyDown={(evento) => {
                      if (evento.key === 'Enter' || evento.key === ' ') {
                        evento.preventDefault();
                        alternarGrupoCompleto(ninosDelGrupo);
                      }
                    }}
                  >
                    {elegidosDelGrupo === ninosDelGrupo.length ? 'Quitar todos' : 'Marcar todos'}
                  </span>

                  <ChevronDown
                    className="grupo-seleccion__flecha"
                    size={18}
                    aria-hidden="true"
                  />
                </span>
              </summary>

              <div className="rejilla-seleccion">
                {ninosDelGrupo.map((nino) => {
                  const marcado = seleccionados.includes(nino.id);

                  return (
                    <label
                      key={nino.id}
                      className={`ficha-seleccion ${marcado ? 'ficha-seleccion--marcada' : ''}`}
                    >
                      <input
                        type="checkbox"
                        className="casilla__control"
                        checked={marcado}
                        onChange={() => alternarNino(nino.id)}
                      />

                      <span className="ficha-seleccion__texto">
                        <span className="ficha-seleccion__nombre">{nino.nombre_Completo}</span>
                        <span className="ficha-seleccion__detalle">{nino.codigo}</span>
                      </span>
                    </label>
                  );
                })}
              </div>
            </details>
          );
        })
      )}

      {/* Aviso discreto: ayuda a notar un envío vacío antes de que falle. */}
      {seleccionados.length === 0 && (
        <p className="selector-ninos__aviso">
          <Search size={16} aria-hidden="true" />
          Elige al menos un niño para poder guardar la actividad.
        </p>
      )}
    </div>
  );
}
