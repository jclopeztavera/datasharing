export enum MatterStatus {
  Active = 1,
  Closed = 2,
}

export enum UserRole {
  Attorney = 1,
  Paralegal = 2,
  Admin = 3,
}

export enum TriggerEventType {
  FilingDate = 1,
  ServiceDate = 2,
  HearingDate = 3,
}

export enum DeadlineType {
  Court = 1,
  Buffer = 2,
}

export enum DeadlineStatus {
  Pending = 1,
  Completed = 2,
  Overdue = 3,
  Waived = 4,
}

export enum SyncStatus {
  Pending = 1,
  Synced = 2,
  Failed = 3,
  Conflict = 4,
}

export interface Matter {
  id: string;
  matterNumber: string;
  title: string;
  state: string;
  county: string;
  caseType: string;
  filingDate: string;
  responsibleAttorneyId: string;
  responsibleAttorneyName: string;
  status: MatterStatus;
  createdAt: string;
}

export interface CreateMatterRequest {
  matterNumber: string;
  title: string;
  state: string;
  county: string;
  caseType: string;
  filingDate: string;
  responsibleAttorneyId: string;
}

export interface Deadline {
  id: string;
  matterId: string;
  matterNumber?: string;
  matterTitle?: string;
  title: string;
  description?: string;
  dueDate: string;
  type: DeadlineType;
  status: DeadlineStatus;
  isManualOverride: boolean;
  courtRuleName?: string;
  triggerEventType?: TriggerEventType;
  triggerEventDate?: string;
  bufferDueDate?: string;
}

export interface OverrideDeadlineRequest {
  newDueDate: string;
  reason: string;
}

export interface TriggerEvent {
  id: string;
  matterId: string;
  eventType: TriggerEventType;
  eventDate: string;
  originalEventDate: string;
  description?: string;
  createdAt: string;
}

export interface CreateTriggerEventRequest {
  matterId: string;
  eventType: TriggerEventType;
  eventDate: string;
  description?: string;
}

export interface CourtRule {
  id: string;
  state: string;
  county?: string;
  caseType: string;
  ruleName: string;
  ruleDescription: string;
  ruleCitation?: string;
  triggerEventType: TriggerEventType;
  daysFromTrigger: number;
  useBusinessDays: boolean;
  bufferDays?: number;
  isActive: boolean;
  effectiveDate: string;
}

export interface DashboardData {
  upcomingDeadlines: DashboardDeadline[];
  overdueDeadlines: DashboardDeadline[];
  atRiskBufferDeadlines: DashboardDeadline[];
  totalActiveMatters: number;
  totalUpcoming: number;
  totalOverdue: number;
  totalAtRisk: number;
}

export interface DashboardDeadline {
  deadlineId: string;
  matterId: string;
  matterNumber: string;
  matterTitle: string;
  deadlineTitle: string;
  dueDate: string;
  type: DeadlineType;
  status: DeadlineStatus;
  daysUntilDue: number;
  responsibleAttorney: string;
  bufferDueDate?: string;
}

export interface UserInfo {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
}
