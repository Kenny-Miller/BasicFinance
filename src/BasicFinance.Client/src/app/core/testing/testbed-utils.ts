import { TestBed } from '@angular/core/testing';

/**
 * Flushes pending effects so a resource's load effect can start (or a `reload()`
 * takes effect), then settles the macrotask queue once so the SDK client's
 * promise bridge and the resource's async continuations complete before state
 * is asserted.
 */
export function settleSdkBridge(): Promise<void> {
  TestBed.flushEffects();
  return new Promise(resolve => setTimeout(resolve, 0));
}
