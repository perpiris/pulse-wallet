import { pattern, required, SchemaPath, validate } from '@angular/forms/signals';

// Same rules as the domain's Money: positive, at most 2 decimals. Amounts are never rounded, only rejected.
export const AMOUNT = /^\d+(\.\d{1,2})?$/;

// A reusable piece of a Signal Forms schema: call it inside form(model, (path) => { ... }) for any string
// field that holds an amount. The top-up/withdraw and send dialogs share it, so their rules can't drift apart.
//
// The amount is a string, not a number: <input type="number"> has quirks (scrolling changes it, "1e3" is valid,
// the decimal separator follows the browser's locale). Text + a pattern gives exact control,
// and the string is converted only when it's sent.
export function amountRules(path: SchemaPath<string>): void {
  required(path, { message: 'Enter an amount.' });
  pattern(path, AMOUNT, { message: 'Use a number with at most 2 decimals, e.g. 25.50.' });
  validate(path, ({ value }) =>
    AMOUNT.test(value()) && Number(value()) === 0
      ? { kind: 'positive', message: 'The amount must be more than 0.' }
      : undefined,
  );
}
