import { Routes } from '@angular/router';

/** Empty stand-ins for the routes AuthService and the guards navigate to, so tests can navigate. Test-only. */
export const TEST_ROUTES: Routes = [
  { path: 'login', children: [] },
  { path: 'dashboard', children: [] },
];
