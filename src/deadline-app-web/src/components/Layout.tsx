import { ReactNode } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import {
  makeStyles,
  tokens,
  Button,
  Text,
  TabList,
  Tab,
} from '@fluentui/react-components';
import {
  CalendarLtr24Regular,
  Briefcase24Regular,
  SignOut24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 24px',
    height: '48px',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  headerTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  userName: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  nav: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    paddingLeft: '24px',
  },
  content: {
    flex: 1,
    padding: '24px',
    maxWidth: '1400px',
    width: '100%',
    margin: '0 auto',
    boxSizing: 'border-box',
  },
});

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();
  const { instance, accounts } = useMsal();

  const currentTab = location.pathname.startsWith('/matters') ? 'matters' : 'dashboard';
  const userName = accounts[0]?.name || accounts[0]?.username || '';

  const handleSignOut = () => {
    instance.logoutRedirect();
  };

  return (
    <div className={styles.root}>
      <header className={styles.header}>
        <Text className={styles.headerTitle}>Deadline Reliability</Text>
        <div className={styles.headerRight}>
          <Text className={styles.userName}>{userName}</Text>
          <Button
            appearance="transparent"
            icon={<SignOut24Regular />}
            onClick={handleSignOut}
            style={{ color: 'white' }}
            size="small"
          />
        </div>
      </header>
      <nav className={styles.nav}>
        <TabList
          selectedValue={currentTab}
          onTabSelect={(_, data) => {
            if (data.value === 'dashboard') navigate('/');
            if (data.value === 'matters') navigate('/matters');
          }}
        >
          <Tab value="dashboard" icon={<CalendarLtr24Regular />}>
            Dashboard
          </Tab>
          <Tab value="matters" icon={<Briefcase24Regular />}>
            Matters
          </Tab>
        </TabList>
      </nav>
      <main className={styles.content}>{children}</main>
    </div>
  );
}
