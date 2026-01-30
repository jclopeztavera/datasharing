import { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  MessageBar,
  MessageBarBody,
  Card,
  CardHeader,
  Badge,
  Table,
  TableHeader,
  TableHeaderCell,
  TableBody,
  TableRow,
  TableCell,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  DialogTrigger,
  Input,
  Select,
  Field,
  Textarea,
} from '@fluentui/react-components';
import {
  ArrowLeft24Regular,
  Add24Regular,
  Checkmark24Regular,
  Edit24Regular,
} from '@fluentui/react-icons';
import {
  getMatter,
  getDeadlinesByMatter,
  getTriggerEvents,
  createTriggerEvent,
  completeDeadline,
  overrideDeadline,
} from '../services/api';
import {
  MatterStatus,
  DeadlineType,
  DeadlineStatus,
  TriggerEventType,
} from '../types';
import type {
  Matter,
  Deadline,
  TriggerEvent,
  CreateTriggerEventRequest,
} from '../types';

const useStyles = makeStyles({
  page: {
    display: 'flex',
    flexDirection: 'column',
    gap: '24px',
  },
  backRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  matterInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  title: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: '8px',
  },
  detailGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
  },
  detailItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  detailLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontWeight: tokens.fontWeightSemibold,
  },
  detailValue: {
    fontSize: tokens.fontSizeBase300,
  },
  tableActions: {
    display: 'flex',
    gap: '4px',
  },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '16px',
  },
  fullWidth: {
    gridColumn: '1 / -1',
  },
});

export function MatterDetailPage() {
  const styles = useStyles();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [matter, setMatter] = useState<Matter | null>(null);
  const [deadlines, setDeadlines] = useState<Deadline[]>([]);
  const [triggerEvents, setTriggerEvents] = useState<TriggerEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [triggerDialogOpen, setTriggerDialogOpen] = useState(false);
  const [overrideDialogOpen, setOverrideDialogOpen] = useState(false);
  const [selectedDeadlineId, setSelectedDeadlineId] = useState<string | null>(null);
  const [overrideDate, setOverrideDate] = useState('');
  const [overrideReason, setOverrideReason] = useState('');
  const [triggerForm, setTriggerForm] = useState({
    eventType: TriggerEventType.FilingDate,
    eventDate: '',
    description: '',
  });

  const loadData = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const [matterData, deadlineData, triggerData] = await Promise.all([
        getMatter(id),
        getDeadlinesByMatter(id),
        getTriggerEvents(id),
      ]);
      setMatter(matterData);
      setDeadlines(deadlineData);
      setTriggerEvents(triggerData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load matter');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleAddTriggerEvent = async () => {
    if (!id) return;
    try {
      const request: CreateTriggerEventRequest = {
        matterId: id,
        eventType: triggerForm.eventType,
        eventDate: triggerForm.eventDate,
        description: triggerForm.description || undefined,
      };
      await createTriggerEvent(request);
      setTriggerDialogOpen(false);
      setTriggerForm({ eventType: TriggerEventType.FilingDate, eventDate: '', description: '' });
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add trigger event');
    }
  };

  const handleCompleteDeadline = async (deadlineId: string) => {
    try {
      await completeDeadline(deadlineId);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete deadline');
    }
  };

  const handleOverrideDeadline = async () => {
    if (!selectedDeadlineId) return;
    try {
      await overrideDeadline(selectedDeadlineId, {
        newDueDate: overrideDate,
        reason: overrideReason,
      });
      setOverrideDialogOpen(false);
      setSelectedDeadlineId(null);
      setOverrideDate('');
      setOverrideReason('');
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to override deadline');
    }
  };

  const triggerEventLabel = (type: TriggerEventType) => {
    switch (type) {
      case TriggerEventType.FilingDate: return 'Filing Date';
      case TriggerEventType.ServiceDate: return 'Service Date';
      case TriggerEventType.HearingDate: return 'Hearing Date';
      default: return 'Unknown';
    }
  };

  const deadlineStatusColor = (status: DeadlineStatus) => {
    switch (status) {
      case DeadlineStatus.Overdue: return 'danger' as const;
      case DeadlineStatus.Pending: return 'warning' as const;
      case DeadlineStatus.Completed: return 'success' as const;
      case DeadlineStatus.Waived: return 'informative' as const;
      default: return 'informative' as const;
    }
  };

  const deadlineStatusLabel = (status: DeadlineStatus) => {
    switch (status) {
      case DeadlineStatus.Overdue: return 'Overdue';
      case DeadlineStatus.Pending: return 'Pending';
      case DeadlineStatus.Completed: return 'Completed';
      case DeadlineStatus.Waived: return 'Waived';
      default: return 'Unknown';
    }
  };

  if (loading) {
    return <Spinner label="Loading matter..." />;
  }

  if (!matter) {
    return (
      <MessageBar intent="error">
        <MessageBarBody>Matter not found</MessageBarBody>
      </MessageBar>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.backRow}>
        <Button
          appearance="subtle"
          icon={<ArrowLeft24Regular />}
          onClick={() => navigate('/matters')}
        >
          Back to Matters
        </Button>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.header}>
        <div className={styles.matterInfo}>
          <Text className={styles.title}>{matter.title}</Text>
          <Text className={styles.subtitle}>
            {matter.matterNumber} &bull; {matter.responsibleAttorneyName}
          </Text>
        </div>
        <Badge
          appearance="filled"
          color={matter.status === MatterStatus.Active ? 'success' : 'informative'}
          size="large"
        >
          {matter.status === MatterStatus.Active ? 'Active' : 'Closed'}
        </Badge>
      </div>

      <Card>
        <CardHeader header={<Text weight="semibold">Matter Details</Text>} />
        <div className={styles.detailGrid} style={{ padding: '0 16px 16px' }}>
          <div className={styles.detailItem}>
            <Text className={styles.detailLabel}>State</Text>
            <Text className={styles.detailValue}>{matter.state}</Text>
          </div>
          <div className={styles.detailItem}>
            <Text className={styles.detailLabel}>County</Text>
            <Text className={styles.detailValue}>{matter.county}</Text>
          </div>
          <div className={styles.detailItem}>
            <Text className={styles.detailLabel}>Case Type</Text>
            <Text className={styles.detailValue}>{matter.caseType}</Text>
          </div>
          <div className={styles.detailItem}>
            <Text className={styles.detailLabel}>Filing Date</Text>
            <Text className={styles.detailValue}>
              {new Date(matter.filingDate).toLocaleDateString()}
            </Text>
          </div>
        </div>
      </Card>

      {/* Trigger Events */}
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
          <Text className={styles.sectionTitle}>Trigger Events</Text>
          <Dialog open={triggerDialogOpen} onOpenChange={(_, data) => setTriggerDialogOpen(data.open)}>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="primary" icon={<Add24Regular />} size="small">
                Add Trigger Event
              </Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Add Trigger Event</DialogTitle>
                <DialogContent>
                  <div className={styles.formGrid}>
                    <Field label="Event Type" required>
                      <Select
                        value={String(triggerForm.eventType)}
                        onChange={(_, data) =>
                          setTriggerForm((prev) => ({
                            ...prev,
                            eventType: Number(data.value) as TriggerEventType,
                          }))
                        }
                      >
                        <option value={String(TriggerEventType.FilingDate)}>Filing Date</option>
                        <option value={String(TriggerEventType.ServiceDate)}>Service Date</option>
                        <option value={String(TriggerEventType.HearingDate)}>Hearing Date</option>
                      </Select>
                    </Field>
                    <Field label="Event Date" required>
                      <Input
                        type="date"
                        value={triggerForm.eventDate}
                        onChange={(_, data) =>
                          setTriggerForm((prev) => ({ ...prev, eventDate: data.value }))
                        }
                      />
                    </Field>
                    <div className={styles.fullWidth}>
                      <Field label="Description">
                        <Textarea
                          value={triggerForm.description}
                          onChange={(_, data) =>
                            setTriggerForm((prev) => ({ ...prev, description: data.value }))
                          }
                        />
                      </Field>
                    </div>
                  </div>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Cancel</Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleAddTriggerEvent}
                    disabled={!triggerForm.eventDate}
                  >
                    Add
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </div>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Type</TableHeaderCell>
              <TableHeaderCell>Date</TableHeaderCell>
              <TableHeaderCell>Original Date</TableHeaderCell>
              <TableHeaderCell>Description</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {triggerEvents.map((te) => (
              <TableRow key={te.id}>
                <TableCell>
                  <Badge appearance="outline">{triggerEventLabel(te.eventType)}</Badge>
                </TableCell>
                <TableCell>{new Date(te.eventDate).toLocaleDateString()}</TableCell>
                <TableCell>{new Date(te.originalEventDate).toLocaleDateString()}</TableCell>
                <TableCell>{te.description || '-'}</TableCell>
              </TableRow>
            ))}
            {triggerEvents.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Text style={{ padding: '16px', display: 'block', textAlign: 'center' }}>
                    No trigger events. Add one to generate deadlines.
                  </Text>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Deadlines */}
      <div>
        <Text className={styles.sectionTitle}>Deadlines</Text>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Deadline</TableHeaderCell>
              <TableHeaderCell>Type</TableHeaderCell>
              <TableHeaderCell>Due Date</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Rule</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {deadlines.map((d) => (
              <TableRow key={d.id}>
                <TableCell>
                  <div>
                    <Text weight="semibold">{d.title}</Text>
                    {d.isManualOverride && (
                      <Badge appearance="outline" size="small" style={{ marginLeft: 4 }}>
                        Override
                      </Badge>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <Badge
                    appearance="filled"
                    color={d.type === DeadlineType.Court ? 'danger' : 'warning'}
                    size="small"
                  >
                    {d.type === DeadlineType.Court ? 'Court' : 'Buffer'}
                  </Badge>
                </TableCell>
                <TableCell>{new Date(d.dueDate).toLocaleDateString()}</TableCell>
                <TableCell>
                  <Badge appearance="filled" color={deadlineStatusColor(d.status)} size="small">
                    {deadlineStatusLabel(d.status)}
                  </Badge>
                </TableCell>
                <TableCell>{d.courtRuleName || '-'}</TableCell>
                <TableCell>
                  <div className={styles.tableActions}>
                    {d.status === DeadlineStatus.Pending && (
                      <>
                        <Button
                          appearance="subtle"
                          icon={<Checkmark24Regular />}
                          size="small"
                          onClick={() => handleCompleteDeadline(d.id)}
                          title="Mark complete"
                        />
                        <Button
                          appearance="subtle"
                          icon={<Edit24Regular />}
                          size="small"
                          onClick={() => {
                            setSelectedDeadlineId(d.id);
                            setOverrideDate(d.dueDate.split('T')[0]);
                            setOverrideDialogOpen(true);
                          }}
                          title="Override"
                        />
                      </>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {deadlines.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Text style={{ padding: '16px', display: 'block', textAlign: 'center' }}>
                    No deadlines. Add a trigger event to generate deadlines.
                  </Text>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Override Dialog */}
      <Dialog open={overrideDialogOpen} onOpenChange={(_, data) => setOverrideDialogOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Override Deadline</DialogTitle>
            <DialogContent>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <Field label="New Due Date" required>
                  <Input
                    type="date"
                    value={overrideDate}
                    onChange={(_, data) => setOverrideDate(data.value)}
                  />
                </Field>
                <Field label="Reason for Override" required>
                  <Textarea
                    value={overrideReason}
                    onChange={(_, data) => setOverrideReason(data.value)}
                    placeholder="Explain why this deadline is being overridden"
                  />
                </Field>
              </div>
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">Cancel</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={handleOverrideDeadline}
                disabled={!overrideDate || !overrideReason}
              >
                Confirm Override
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
