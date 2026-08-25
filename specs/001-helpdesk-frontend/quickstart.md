# Quickstart Validation Guide

Follow these steps to validate the HelpDisk Frontend implementation end-to-end.

## Prerequisites
1. Node.js (v18+)
2. The HelpDisk ASP.NET Core API must be running locally.
3. The backend database must be seeded with initial roles, companies, and categories.

## Setup
```bash
# 1. Install dependencies
cd frontend/helpdesk-web
npm install

# 2. Configure environment
# Copy .env.example to .env.local and set NEXT_PUBLIC_API_URL to the backend URL
cp .env.example .env.local

# 3. Start development server
npm run dev
```

## Validation Scenarios

### Scenario 1: Authentication & Routing
1. Open `http://localhost:3000` in an incognito window. You should be redirected to `/login`.
2. Click "Register". Fill in details and select a company. Submit.
3. Verify you are redirected to `/dashboard` and the UI shows Customer-specific widgets.
4. Log out.
5. Log in with seeded admin credentials (`admin@helpdisk.com`). Verify you see Admin-specific widgets and the `/admin/*` links in the navigation.

### Scenario 2: Ticket Lifecycle
1. Log in as a Customer.
2. Navigate to `/tickets/new`. Create a ticket with priority "High" and a category.
3. Verify the ticket appears in your `/tickets` list with "New" status.
4. Log out. Log in as an Agent (`agent1@helpdisk.com`).
5. Open the ticket just created. Assign it to yourself. Verify status changes to "InProgress".
6. Add an internal comment.
7. Close the ticket. Verify you can no longer edit, assign, or comment.
8. Log out. Log in as the Customer.
9. Open the closed ticket. Verify the internal comment is NOT visible.
10. Click the "Reopen" button. Verify the ticket status changes to "New" or "InProgress".

### Scenario 3: Admin Management
1. Log in as an Admin.
2. Navigate to `/admin/categories`.
3. Create a new category with a 24-hour SLA. Verify it appears in the list.
4. Navigate to `/admin/agents`.
5. Create a new agent account. Verify they can now log in.
