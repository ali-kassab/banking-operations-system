import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from './error-message';

describe('errorMessage', () => {
  it('explains when the API cannot be reached', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 0 }))).toContain('Cannot reach the API');
  });

  it('explains a 403', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 403 }))).toBe('You do not have permission to perform this action.');
  });

  it('joins validation messages', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { Amount: ['Too big.'], Email: ['Invalid.', 'Required.'] } },
    });
    expect(errorMessage(error)).toBe('Too big. Invalid. Required.');
  });

  it('prefers the ProblemDetails detail, then the title', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Email taken.' } }))).toBe(
      'Email taken.',
    );
    expect(errorMessage(new HttpErrorResponse({ status: 400, error: { title: 'Business rule violation' } }))).toBe(
      'Business rule violation',
    );
  });

  it('falls back to the status code', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 500 }))).toBe('Request failed (500).');
  });

  it('handles non-HTTP errors', () => {
    expect(errorMessage(new Error('boom'))).toBe('Something went wrong.');
  });
});
