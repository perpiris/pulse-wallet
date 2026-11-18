import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    // The header uses routerLink, which needs the router; an empty route table is enough for the shell.
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should show the app name and the sign-in links when signed out', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('header')?.textContent).toContain('PulseWallet');
    expect(compiled.querySelector('nav')?.textContent).toContain('Sign in');
  });
});
