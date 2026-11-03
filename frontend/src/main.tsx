import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { LimiteDeErrores } from './componentes/LimiteDeErrores';
import './estilos/global.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <LimiteDeErrores>
      <App />
    </LimiteDeErrores>
  </StrictMode>,
);
