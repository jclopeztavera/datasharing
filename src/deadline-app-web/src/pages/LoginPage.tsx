import { useMsal } from '@azure/msal-react';
import {
  makeStyles,
  tokens,
  Button,
  Text,
  Card,
} from '@fluentui/react-components';
import { loginRequest } from '../services/authConfig';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  card: {
    padding: '48px',
    textAlign: 'center' as const,
    maxWidth: '400px',
    width: '100%',
  },
  title: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightBold,
    marginBottom: '8px',
    display: 'block',
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
    marginBottom: '32px',
    display: 'block',
  },
});

export function LoginPage() {
  const styles = useStyles();
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginRedirect(loginRequest);
  };

  return (
    <div className={styles.root}>
      <Card className={styles.card}>
        <Text className={styles.title}>Deadline Reliability</Text>
        <Text className={styles.subtitle}>
          Family Law Court Deadline Management
        </Text>
        <Button appearance="primary" size="large" onClick={handleLogin}>
          Sign in with Microsoft
        </Button>
      </Card>
    </div>
  );
}
