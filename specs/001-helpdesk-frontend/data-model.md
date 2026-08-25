# Frontend Data Model: HelpDisk

Extracted from the backend API contracts and spec.md.

## Entities

### User Session
```typescript
interface UserSession {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Customer' | 'Agent' | 'Admin';
  companyId?: string; // Only for Customers
  token: string;
  expiresAt: string; // ISO 8601
}
```

### Ticket
```typescript
interface Ticket {
  ticketNumber: string;
  title: string;
  description: string;
  status: 'New' | 'InProgress' | 'Closed';
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  categoryId: string;
  categoryName: string;
  reporterId: string;
  reporterName: string;
  assigneeId?: string;
  assigneeName?: string;
  createdAt: string; // ISO 8601 UTC
  slaDeadline: string; // ISO 8601 UTC
  slaStatus: 'Pending' | 'Met' | 'Breached';
}
```

### Comment
```typescript
interface Comment {
  id: string;
  ticketId: string;
  content: string;
  authorId: string;
  authorName: string;
  isInternal: boolean;
  createdAt: string; // ISO 8601 UTC
}
```

### Attachment
```typescript
interface Attachment {
  id: string;
  ticketId: string;
  fileName: string;
  fileType: string; // MIME type
  fileSize: number; // bytes
  uploaderId: string;
  uploaderName: string;
  uploadTime: string; // ISO 8601 UTC
}
```

### Category
```typescript
interface Category {
  id: string;
  name: string;
  slaHours: number;
}
```

### Agent
```typescript
interface Agent {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
}
```

### Company
```typescript
interface Company {
  id: string;
  name: string;
}
```
