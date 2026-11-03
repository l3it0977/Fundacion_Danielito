import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { AlertCircle, Eye, EyeOff, LogIn } from 'lucide-react';
import { useAutenticacion } from '../contextos/ContextoAutenticacion';
import { Boton } from '../componentes/comunes/Boton';
import { ErrorApi } from '../api/cliente';
import './Login.css';

export function Login() {
  const { usuario, iniciarSesion } = useAutenticacion();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [mostrarPassword, setMostrarPassword] = useState(false);
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);

  // Quien ya tiene sesión no necesita ver esta pantalla.
  if (usuario) {
    return <Navigate to="/" replace />;
  }

  async function alEnviar(evento: FormEvent) {
    evento.preventDefault();
    setError('');
    setEnviando(true);

    try {
      await iniciarSesion(email.trim(), password);
    } catch (fallo) {
      setError(
        fallo instanceof ErrorApi
          ? fallo.message
          : 'No se pudo conectar con el servidor. Revisa tu conexión.',
      );
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="login">
      {/*
        Lado izquierdo: identidad de la fundación sobre el degradado de la marca.
        En pantallas angostas se convierte en una franja superior con el logo pequeño.
      */}
      <section className="login__marca">
        <div className="login__marca-contenido">
          <img
            src="/img/logo-danielito.png"
            alt="Logo de la Fundación Danielito"
            className="login__logo"
          />

          <h1 className="login__marca-titulo">
            Fundación <strong>Danielito</strong>
          </h1>

          <p className="login__marca-descripcion">
            Sistema de seguimiento de niños, grupos y asistencia.
          </p>
        </div>

        <p className="login__marca-pie">Warnes, Santa Cruz — Bolivia</p>
      </section>

      {/* Lado derecho: el formulario. */}
      <section className="login__acceso">
        <div className="login__formulario-envoltorio">
          <header className="login__formulario-encabezado">
            <h2 className="login__formulario-titulo">Iniciar sesión</h2>
            <p className="login__formulario-descripcion">
              Ingresa con la cuenta que te asignó la fundación.
            </p>
          </header>

          <form className="login__formulario" onSubmit={alEnviar} noValidate>
            {/*
              El error se anuncia apenas aparece y queda junto al formulario,
              no escondido arriba de la página.
            */}
            {error && (
              <p className="login__error" role="alert">
                <AlertCircle size={18} aria-hidden="true" />
                <span>{error}</span>
              </p>
            )}

            <div className="campo">
              <label htmlFor="email" className="campo__etiqueta">
                Correo electrónico
              </label>
              <input
                id="email"
                type="email"
                className="campo__control"
                value={email}
                onChange={(evento) => setEmail(evento.target.value)}
                autoComplete="username"
                required
                autoFocus
              />
            </div>

            <div className="campo">
              <label htmlFor="password" className="campo__etiqueta">
                Contraseña
              </label>

              <div className="campo__con-boton">
                <input
                  id="password"
                  type={mostrarPassword ? 'text' : 'password'}
                  className="campo__control"
                  value={password}
                  onChange={(evento) => setPassword(evento.target.value)}
                  autoComplete="current-password"
                  required
                />

                <button
                  type="button"
                  className="campo__boton-ojo"
                  onClick={() => setMostrarPassword((visible) => !visible)}
                  aria-label={mostrarPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {mostrarPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </div>

            <Boton
              type="submit"
              cargando={enviando}
              anchoCompleto
              icono={<LogIn size={18} aria-hidden="true" />}
            >
              Entrar
            </Boton>
          </form>

          <p className="login__pie">
            ¿Problemas para entrar? Comunícate con la administración.
          </p>
        </div>
      </section>
    </div>
  );
}
