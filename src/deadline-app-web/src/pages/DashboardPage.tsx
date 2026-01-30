import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  makeStyles,
  tokens,
  Text,
  Card,
  CardHeader,
  Spinner,
  MessageBar,
  MessageBarBody,
  CounterBadge,
} from '@fluentui/react-components';
import {
  Warning24Filled,
  Clock24Filled,
  CalendarLtr24Filled,
} from '@fluentui/react-icons';
import { getDashboard } from '../services/api';
import { DeadlineCard } from '../components/DeadlineCard';
import type { DashboardData } from '../types';

const useStyles = makeStyles({
  page: {
    display: 'flex',
    flexDirection: 'column',
    gap: '24px',
  },
  title: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
  },
  summaryRow: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
  },
  summaryCard: {
    padding: '16px',
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  summaryIcon: {
    padding: '8px',
    borderRadius: '8px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  summaryText: {
    display: 'flex',
    flexDirection: 'column',
  },
  summaryValue: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightBold,
  },
  summaryLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  columns: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
    gap: '24px',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: '8px',
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  emptyState: {
    padding: '24px',
    textAlign: 'center' as const,
    color: tokens.colorNeutralForeground3,
  },
});

export function DashboardPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await getDashboard();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  if (loading) {
    return <Spinner label="Loading dashboard..." />;
  }

  if (error) {
    return (
      <MessageBar intent="error">
        <MessageBarBody>{error}</MessageBarBody>
      </MessageBar>
    );
  }

  if (!data) return null;

  return (
    <div className={styles.page}>
      <Text className={styles.title}>Dashboard</Text>

      <div className={styles.summaryRow}>
        <Card className={styles.summaryCard}>
          <div
            className={styles.summaryIcon}
            style={{ backgroundColor: tokens.colorPaletteRedBackground2 }}
          >
            <Warning24Filled primaryFill={tokens.colorPaletteRedForeground1} />
          </div>
          <div className={styles.summaryText}>
            <Text className={styles.summaryValue}>{data.totalOverdue}</Text>
            <Text className={styles.summaryLabel}>Overdue</Text>
          </div>
        </Card>

        <Card className={styles.summaryCard}>
          <div
            className={styles.summaryIcon}
            style={{ backgroundColor: tokens.colorPaletteYellowBackground2 }}
          >
            <Clock24Filled primaryFill={tokens.colorPaletteYellowForeground2} />
          </div>
          <div className={styles.summaryText}>
            <Text className={styles.summaryValue}>{data.totalAtRisk}</Text>
            <Text className={styles.summaryLabel}>At Risk</Text>
          </div>
        </Card>

        <Card className={styles.summaryCard}>
          <div
            className={styles.summaryIcon}
            style={{ backgroundColor: tokens.colorPaletteBlueBackground2 }}
          >
            <CalendarLtr24Filled primaryFill={tokens.colorPaletteBlueForeground2} />
          </div>
          <div className={styles.summaryText}>
            <Text className={styles.summaryValue}>{data.totalUpcoming}</Text>
            <Text className={styles.summaryLabel}>Next 30 Days</Text>
          </div>
        </Card>

        <Card className={styles.summaryCard}>
          <div
            className={styles.summaryIcon}
            style={{ backgroundColor: tokens.colorNeutralBackground3 }}
          >
            <CalendarLtr24Filled primaryFill={tokens.colorNeutralForeground3} />
          </div>
          <div className={styles.summaryText}>
            <Text className={styles.summaryValue}>{data.totalActiveMatters}</Text>
            <Text className={styles.summaryLabel}>Active Matters</Text>
          </div>
        </Card>
      </div>

      <div className={styles.columns}>
        {/* Overdue Deadlines */}
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <Text className={styles.sectionTitle}>Overdue</Text>
            <CounterBadge count={data.overdueDeadlines.length} color="danger" />
          </div>
          {data.overdueDeadlines.length === 0 ? (
            <Card>
              <Text className={styles.emptyState}>No overdue deadlines</Text>
            </Card>
          ) : (
            data.overdueDeadlines.map((d) => (
              <DeadlineCard
                key={d.deadlineId}
                deadline={d}
                onClick={() => navigate(`/matters/${d.matterId}`)}
              />
            ))
          )}
        </div>

        {/* At-Risk Buffer Deadlines */}
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <Text className={styles.sectionTitle}>Buffer Deadlines at Risk</Text>
            <CounterBadge count={data.atRiskBufferDeadlines.length} color="warning" />
          </div>
          {data.atRiskBufferDeadlines.length === 0 ? (
            <Card>
              <Text className={styles.emptyState}>No at-risk buffer deadlines</Text>
            </Card>
          ) : (
            data.atRiskBufferDeadlines.map((d) => (
              <DeadlineCard
                key={d.deadlineId}
                deadline={d}
                onClick={() => navigate(`/matters/${d.matterId}`)}
              />
            ))
          )}
        </div>

        {/* Upcoming Deadlines */}
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <Text className={styles.sectionTitle}>Upcoming (30 days)</Text>
            <CounterBadge count={data.upcomingDeadlines.length} color="informative" />
          </div>
          {data.upcomingDeadlines.length === 0 ? (
            <Card>
              <Text className={styles.emptyState}>No upcoming deadlines</Text>
            </Card>
          ) : (
            data.upcomingDeadlines.map((d) => (
              <DeadlineCard
                key={d.deadlineId}
                deadline={d}
                onClick={() => navigate(`/matters/${d.matterId}`)}
              />
            ))
          )}
        </div>
      </div>
    </div>
  );
}
