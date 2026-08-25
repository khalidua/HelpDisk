export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail: string;
}

export class ApiError extends Error {
  public problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail || problem.title);
    this.name = 'ApiError';
    this.problem = problem;
  }
}
