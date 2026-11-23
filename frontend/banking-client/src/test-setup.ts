// Global setup for the unit tests (angular.json → test → setupFiles).
//
// Node 25+ defines its own global `localStorage`, which is `undefined` unless Node is started with
// --localstorage-file. In Vitest's jsdom environment that property hides jsdom's working Storage,
// so point it back at the jsdom window (Vitest exposes the instance as the global `jsdom`).
// Browsers are not affected; this file only runs in tests.
declare const jsdom: { window: Window } | undefined;

if (typeof localStorage === 'undefined' && typeof jsdom !== 'undefined') {
  for (const name of ['localStorage', 'sessionStorage'] as const) {
    Object.defineProperty(globalThis, name, { value: jsdom.window[name], configurable: true, writable: true });
  }
}
