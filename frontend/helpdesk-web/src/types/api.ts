// Authentication
export interface RegisterRequest {
  email?: string;
  password?: string;
  firstName?: string;
  lastName?: string;
  companyId?: string;
}

export interface LoginRequest {
  email?: string;
  password?: string;
}

export interface TokenResponse {
  token: string;
  expiresAt: string;
  role: 'Customer' | 'Agent' | 'Admin';
}

export interface Company {
  id: string;
  name: string;
}

// Tickets
export type TicketStatus = 'New' | 'InProgress' | 'Closed';
export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent';
export type TicketSlaStatus = 'Pending' | 'Met' | 'Breached';

export interface TicketListItem {
  id: string;
  ticketNumber: string;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  categoryId: string;
  assigneeId: string | null;
  responseDeadlineUtc: string;
  slaStatus: TicketSlaStatus;
  createdOnUtc: string;
}

export interface PaginatedList<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Comment {
  id: string;
  body: string;
  authorId: string;
  createdOnUtc: string;
  isInternal: boolean;
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedById: string;
  createdOnUtc: string;
}

export interface TicketDetail {
  id: string;
  ticketNumber: string;
  title: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  categoryId: string;
  reporterId: string;
  assigneeId: string | null;
  createdOnUtc: string;
  modifiedOnUtc: string | null;
  closedOnUtc: string | null;
  responseDeadlineUtc: string;
  slaStatus: TicketSlaStatus;
  comments: Comment[];
  attachments: Attachment[];
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
  categoryId: string;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
}

export interface AssignTicketRequest {
  assigneeId: string;
}

// Comments
export interface AddCommentRequest {
  body: string;
  isInternal: boolean;
}

// Categories
export interface Category {
  id: string;
  name: string;
  responseTimeTargetHours: number;
  createdOnUtc: string;
}

export interface CreateCategoryRequest {
  name: string;
  responseTimeTargetHours: number;
}

export interface UpdateCategoryRequest {
  name: string;
  responseTimeTargetHours: number;
}

// Agents
export interface Agent {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export interface CreateAgentRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface UpdateAgentRequest {
  email: string;
  firstName: string;
  lastName: string;
}

// Reports
export interface OpenTicketsPerAgentReport {
  agentId: string | null;
  openTicketsCount: number;
}

export interface AverageResolutionTimeReport {
  categoryId: string;
  averageResolutionTimeInHours: number;
}

export interface SlaBreachesReport {
  breachCount: number;
}
