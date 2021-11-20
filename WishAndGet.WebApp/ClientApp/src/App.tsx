import { Fragment } from 'react';
import logo from './logo.svg';
import './App.css';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import theme from './theme';
import BathtubIcon from '@mui/icons-material/Bathtub';

function App(): JSX.Element {
  return (
    <Fragment>
      <CssBaseline />
      <ThemeProvider theme={theme}>
        <div className="App">
          <header className="App-header">
            <img src={logo} className="App-logo" alt="logo" />
            <p>
              Edit <code>src/App.tsx</code> and save to reload. <BathtubIcon />
            </p>
            <div />
            <a
              className="App-link"
              href="https://reactjs.org"
              target="_blank"
              rel="noopener noreferrer"
            >
              Learn React
            </a>
          </header>
        </div>
      </ThemeProvider>
    </Fragment>
  );
}

export default App;
