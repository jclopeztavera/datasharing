import {
  Card,
  CardHeader,
  Text,
  Badge,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { DeadlineType, DeadlineStatus } from '../types';
import type { DashboardDeadline } from '../types';

const useStyles = makeStyles({
  card: {
    marginBottom: '8px',
    cursor: 'pointer',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  overdue: {
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
  },
  atRisk: {
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
  },
  upcoming: {
    borderLeft: `4px solid ${tokens.colorPaletteBlueBorder2}`,
  },
  cardContent: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0 12px 12px',
  },
  matterInfo: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  dueDate: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  daysLabel: {
    fontSize: tokens.fontSizeBase100,
  },
});

interface DeadlineCardProps {
  deadline: DashboardDeadline;
  onClick?: () => void;
}

export function DeadlineCard({ deadline, onClick }: DeadlineCardProps) {
  const styles = useStyles();

  const isOverdue = deadline.status === DeadlineStatus.Overdue;
  const isAtRisk = deadline.daysUntilDue <= 5 && !isOverdue;

  const borderClass = isOverdue
    ? styles.overdue
    : isAtRisk
    ? styles.atRisk
    : styles.upcoming;

  const daysText = isOverdue
    ? `${Math.abs(deadline.daysUntilDue)} days overdue`
    : deadline.daysUntilDue === 0
    ? 'Due today'
    : `${deadline.daysUntilDue} days`;

  return (
    <Card className={`${styles.card} ${borderClass}`} onClick={onClick}>
      <CardHeader
        header={<Text weight="semibold">{deadline.deadlineTitle}</Text>}
        description={
          <Text className={styles.matterInfo}>
            {deadline.matterNumber} - {deadline.matterTitle}
          </Text>
        }
        action={
          <Badge
            appearance="filled"
            color={deadline.type === DeadlineType.Court ? 'danger' : 'warning'}
            size="small"
          >
            {deadline.type === DeadlineType.Court ? 'Court' : 'Buffer'}
          </Badge>
        }
      />
      <div className={styles.cardContent}>
        <Text className={styles.dueDate}>
          {new Date(deadline.dueDate).toLocaleDateString()}
        </Text>
        <Text
          className={styles.daysLabel}
          style={{
            color: isOverdue
              ? tokens.colorPaletteRedForeground1
              : isAtRisk
              ? tokens.colorPaletteYellowForeground2
              : tokens.colorNeutralForeground3,
          }}
        >
          {daysText}
        </Text>
      </div>
    </Card>
  );
}
