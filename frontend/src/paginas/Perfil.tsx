import { useEffect, useState } from 'react';
import { KeyRound, Save } from 'lucide-react';
import { servicioPerfil } from '../api/servicios';
import { useCargarDatos } from '../hooks/useCargarDatos';
import { EncabezadoPagina } from '../componentes/comunes/EncabezadoPagina';
import { Panel } from '../componentes/comunes/Panel';
import { Boton } from '../componentes/comunes/Boton';
import { Aviso } from '../componentes/comunes/Aviso';
import { Cargando, MensajeError } from '../componentes/comunes/Estados';
import { Insignia } from '../componentes/comunes/Insignia';
import { CampoTexto } from '../componentes/comunes/Campos';
import './Listados.css';

export function Perfil() {
  const { datos: perfil, cargando, error, recargar } = useCargarDatos(servicioPerfil.miPerfil);

  const [datos, setDatos] = useState({ nombre: '', apellido: '', email: '' });
  const [passwordActual, setPasswordActual] = useState('');
  const [passwordNueva, setPasswordNueva] = useState('');
  const [passwordRepetida, setPasswordRepetida] = useState('');

  const [avisoDatos, setAvisoDatos] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(
    null,
  );
  const [avisoPassword, setAvisoPassword] = useState<{
    tipo: 'exito' | 'error';
    texto: string;
  } | null>(null);

  const [guardandoDatos, setGuardandoDatos] = useState(false);
  const [guardandoPassword, setGuardandoPassword] = useState(false);

  useEffect(() => {
    if (perfil) {
      setDatos({ nombre: perfil.nombre, apellido: perfil.apellido, email: perfil.email });
    }
  }, [perfil]);

  async function guardarDatos() {
    setGuardandoDatos(true);
    setAvisoDatos(null);

    try {
      await servicioPerfil.actualizar({
        nombre: datos.nombre.trim(),
        apellido: datos.apellido.trim(),
        email: datos.email.trim(),
      });

      setAvisoDatos({ tipo: 'exito', texto: 'Datos actualizados.' });
      recargar();
    } catch (fallo) {
      setAvisoDatos({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardandoDatos(false);
    }
  }

  async function cambiarPassword() {
    // Se comprueba aquí que ambas coincidan: el backend no recibe la repetición.
    if (passwordNueva !== passwordRepetida) {
      setAvisoPassword({ tipo: 'error', texto: 'La nueva contraseña y su repetición no coinciden.' });
      return;
    }

    setGuardandoPassword(true);
    setAvisoPassword(null);

    try {
      await servicioPerfil.cambiarPassword(passwordActual, passwordNueva);

      setAvisoPassword({ tipo: 'exito', texto: 'Contraseña actualizada.' });
      setPasswordActual('');
      setPasswordNueva('');
      setPasswordRepetida('');
    } catch (fallo) {
      setAvisoPassword({ tipo: 'error', texto: (fallo as Error).message });
    } finally {
      setGuardandoPassword(false);
    }
  }

  if (cargando) {
    return <Cargando mensaje="Cargando tu perfil…" />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  if (!perfil) {
    return <MensajeError mensaje="No se pudo cargar el perfil." />;
  }

  return (
    <div className="pagina">
      <EncabezadoPagina titulo="Mi perfil" descripcion="Tus datos de acceso al sistema." />

      <Panel titulo="Resumen">
        <div className="detalle-rejilla">
          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Rol</span>
            <span className="detalle-dato__valor">
              <Insignia texto={perfil.rol} tono="naranja" />
            </span>
          </div>

          <div className="detalle-dato">
            <span className="detalle-dato__etiqueta">Estado</span>
            <span className="detalle-dato__valor">
              <Insignia
                texto={perfil.activo ? 'Activo' : 'Inactivo'}
                tono={perfil.activo ? 'exito' : 'error'}
              />
            </span>
          </div>

          {perfil.mis_Grupos.length > 0 && (
            <div className="detalle-dato">
              <span className="detalle-dato__etiqueta">Grupos a cargo</span>
              <span className="detalle-dato__valor">{perfil.mis_Grupos.join(', ')}</span>
            </div>
          )}
        </div>
      </Panel>

      <Panel titulo="Datos personales">
        {avisoDatos && (
          <Aviso tipo={avisoDatos.tipo} alCerrar={() => setAvisoDatos(null)}>
            {avisoDatos.texto}
          </Aviso>
        )}

        <form
          onSubmit={(evento) => {
            evento.preventDefault();
            guardarDatos();
          }}
        >
          <div className="formulario-rejilla">
            <CampoTexto
              etiqueta="Nombre"
              requerido
              value={datos.nombre}
              onChange={(evento) => setDatos({ ...datos, nombre: evento.target.value })}
            />

            <CampoTexto
              etiqueta="Apellido"
              requerido
              value={datos.apellido}
              onChange={(evento) => setDatos({ ...datos, apellido: evento.target.value })}
            />

            <div className="formulario-rejilla__completo">
              <CampoTexto
                etiqueta="Correo electrónico"
                type="email"
                requerido
                value={datos.email}
                onChange={(evento) => setDatos({ ...datos, email: evento.target.value })}
                ayuda="Es el correo con el que inicias sesión."
              />
            </div>
          </div>

          <div className="formulario-acciones">
            <Boton type="submit" cargando={guardandoDatos} icono={<Save size={18} />}>
              Guardar datos
            </Boton>
          </div>
        </form>
      </Panel>

      <Panel titulo="Cambiar contraseña">
        {avisoPassword && (
          <Aviso tipo={avisoPassword.tipo} alCerrar={() => setAvisoPassword(null)}>
            {avisoPassword.texto}
          </Aviso>
        )}

        <form
          onSubmit={(evento) => {
            evento.preventDefault();
            cambiarPassword();
          }}
        >
          <div className="formulario-rejilla">
            <div className="formulario-rejilla__completo">
              <CampoTexto
                etiqueta="Contraseña actual"
                type="password"
                requerido
                autoComplete="current-password"
                value={passwordActual}
                onChange={(evento) => setPasswordActual(evento.target.value)}
              />
            </div>

            <CampoTexto
              etiqueta="Nueva contraseña"
              type="password"
              requerido
              autoComplete="new-password"
              value={passwordNueva}
              onChange={(evento) => setPasswordNueva(evento.target.value)}
              ayuda="Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo."
            />

            <CampoTexto
              etiqueta="Repetir nueva contraseña"
              type="password"
              requerido
              autoComplete="new-password"
              value={passwordRepetida}
              onChange={(evento) => setPasswordRepetida(evento.target.value)}
              /* El error aparece al escribir, sin esperar a enviar el formulario. */
              error={
                passwordRepetida && passwordNueva !== passwordRepetida
                  ? 'No coincide con la nueva contraseña.'
                  : undefined
              }
            />
          </div>

          <div className="formulario-acciones">
            <Boton
              type="submit"
              cargando={guardandoPassword}
              icono={<KeyRound size={18} />}
            >
              Cambiar contraseña
            </Boton>
          </div>
        </form>
      </Panel>
    </div>
  );
}
