import { createTheme } from '@mui/material/styles';

const theme = createTheme({
  components: {
    MuiTooltip: {
      defaultProps: {
        arrow: true,
      },
    },
  },
});

export default theme;
