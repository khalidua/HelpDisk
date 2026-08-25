import { z } from 'zod';

export const createTicketSchema = z.object({
  title: z.string().min(3, 'Title must be at least 3 characters').max(200, 'Title is too long'),
  description: z.string().min(10, 'Description must be at least 10 characters').max(5000, 'Description is too long'),
  priority: z.enum(['Low', 'Normal', 'High', 'Urgent'] as const, { message: 'Priority is required' }),
  categoryId: z.string().uuid('Please select a valid category'),
});

export type CreateTicketFormData = z.infer<typeof createTicketSchema>;

export const addCommentSchema = z.object({
  body: z.string().min(1, 'Comment cannot be empty').max(5000, 'Comment is too long'),
});

export type AddCommentFormData = z.infer<typeof addCommentSchema>;
