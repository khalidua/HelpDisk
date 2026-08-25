# UI Routes Contract

This defines the Next.js App Router structure and the roles permitted to access each route.

| Route Path | Description | Allowed Roles |
|------------|-------------|---------------|
| `/` | Landing/redirect to dashboard | All |
| `/login` | Authentication | Public |
| `/register` | Customer self-registration | Public |
| `/dashboard` | Main dashboard (content varies by role) | Customer, Agent, Admin |
| `/tickets` | Ticket list (filtered based on role) | Customer, Agent, Admin |
| `/tickets/new` | Create new ticket | Customer, Agent, Admin |
| `/tickets/[id]` | Ticket details, comments, attachments | Customer, Agent, Admin (scoped by company for Customers) |
| `/admin/agents` | Manage agents | Admin |
| `/admin/categories`| Manage categories | Admin |
| `/admin/reports` | View KPI reports | Admin |

**Notes:**
- A `<RoleGuard>` or middleware must intercept requests to paths starting with `/admin` and redirect non-Admin users to `/dashboard`.
- Unauthenticated users attempting to access any route other than `/login` and `/register` must be redirected to `/login`.
