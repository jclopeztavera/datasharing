import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  MessageBar,
  MessageBarBody,
  Table,
  TableHeader,
  TableHeaderCell,
  TableBody,
  TableRow,
  TableCell,
  Badge,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  DialogTrigger,
  Input,
  Label,
  Select,
  Field,
} from '@fluentui/react-components';
import { Add24Regular } from '@fluentui/react-icons';
import { getMatters, createMatter } from '../services/api';
import { MatterStatus } from '../types';
import type { Matter, CreateMatterRequest } from '../types';

const useStyles = makeStyles({
  page: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
  },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '16px',
  },
  fullWidth: {
    gridColumn: '1 / -1',
  },
  clickableRow: {
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
});

const initialForm: CreateMatterRequest = {
  matterNumber: '',
  title: '',
  state: 'FL',
  county: '',
  caseType: 'Family Law',
  filingDate: new Date().toISOString().split('T')[0],
  responsibleAttorneyId: '',
};

export function MattersPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [matters, setMatters] = useState<Matter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<CreateMatterRequest>(initialForm);
  const [submitting, setSubmitting] = useState(false);

  const loadMatters = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await getMatters();
      setMatters(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load matters');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadMatters();
  }, [loadMatters]);

  const handleCreate = async () => {
    try {
      setSubmitting(true);
      await createMatter(form);
      setDialogOpen(false);
      setForm(initialForm);
      await loadMatters();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create matter');
    } finally {
      setSubmitting(false);
    }
  };

  const updateField = (field: keyof CreateMatterRequest, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  if (loading) {
    return <Spinner label="Loading matters..." />;
  }

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <Text className={styles.title}>Matters</Text>
        <Dialog open={dialogOpen} onOpenChange={(_, data) => setDialogOpen(data.open)}>
          <DialogTrigger disableButtonEnhancement>
            <Button appearance="primary" icon={<Add24Regular />}>
              New Matter
            </Button>
          </DialogTrigger>
          <DialogSurface>
            <DialogBody>
              <DialogTitle>Create New Matter</DialogTitle>
              <DialogContent>
                <div className={styles.formGrid}>
                  <Field label="Matter Number" required>
                    <Input
                      value={form.matterNumber}
                      onChange={(_, data) => updateField('matterNumber', data.value)}
                    />
                  </Field>
                  <Field label="State" required>
                    <Select
                      value={form.state}
                      onChange={(_, data) => updateField('state', data.value)}
                    >
                      <option value="FL">Florida</option>
                      <option value="NY">New York</option>
                    </Select>
                  </Field>
                  <div className={styles.fullWidth}>
                    <Field label="Title" required>
                      <Input
                        value={form.title}
                        onChange={(_, data) => updateField('title', data.value)}
                      />
                    </Field>
                  </div>
                  <Field label="County" required>
                    <Input
                      value={form.county}
                      onChange={(_, data) => updateField('county', data.value)}
                    />
                  </Field>
                  <Field label="Case Type" required>
                    <Input
                      value={form.caseType}
                      onChange={(_, data) => updateField('caseType', data.value)}
                    />
                  </Field>
                  <Field label="Filing Date" required>
                    <Input
                      type="date"
                      value={form.filingDate}
                      onChange={(_, data) => updateField('filingDate', data.value)}
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
                  onClick={handleCreate}
                  disabled={submitting || !form.matterNumber || !form.title || !form.county}
                >
                  {submitting ? 'Creating...' : 'Create'}
                </Button>
              </DialogActions>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Matter #</TableHeaderCell>
            <TableHeaderCell>Title</TableHeaderCell>
            <TableHeaderCell>State</TableHeaderCell>
            <TableHeaderCell>County</TableHeaderCell>
            <TableHeaderCell>Filing Date</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {matters.map((matter) => (
            <TableRow
              key={matter.id}
              className={styles.clickableRow}
              onClick={() => navigate(`/matters/${matter.id}`)}
            >
              <TableCell>{matter.matterNumber}</TableCell>
              <TableCell>{matter.title}</TableCell>
              <TableCell>{matter.state}</TableCell>
              <TableCell>{matter.county}</TableCell>
              <TableCell>{new Date(matter.filingDate).toLocaleDateString()}</TableCell>
              <TableCell>
                <Badge
                  appearance="filled"
                  color={matter.status === MatterStatus.Active ? 'success' : 'informative'}
                >
                  {matter.status === MatterStatus.Active ? 'Active' : 'Closed'}
                </Badge>
              </TableCell>
            </TableRow>
          ))}
          {matters.length === 0 && (
            <TableRow>
              <TableCell colSpan={6}>
                <Text style={{ padding: '24px', display: 'block', textAlign: 'center' }}>
                  No matters found. Create your first matter to get started.
                </Text>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
