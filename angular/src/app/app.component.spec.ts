import { describe, expect, it } from 'vitest';
import { environment } from '../environments/environment';

describe('SmartPantry frontend configuration', () => {
  it('identifies the application', () => {
    expect(environment.application.name).toBe('SmartPantry');
  });
});