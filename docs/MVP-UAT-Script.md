# MVP — Family UAT Script (Sprint 7 / TASK 7.4)

A short, hands-on script to confirm the MVP replaces the old budgeting Excel. Run it with both
family members on real(ish) data. Tick each step; note anything confusing or broken in **Feedback**.

> Acceptance reference: `MVP-Plan.md` §5. The MVP is "done" when every box below is checked and no
> blocker remains.

## 0. Setup (each person)
- [ ] Register an account, confirm email (if required), sign in.
- [ ] Complete the profile prompt (name, preferred currency = SEK).

## 1. Household (one owner + one spouse)
- [ ] Owner: **Household → Create** ("Andersson").
- [ ] Owner: invite the spouse by email; copy the invite link.
- [ ] Spouse: open the invite link and accept; both now appear as members.

## 2. Accounts
- [ ] Create a **Checking** account with a realistic opening balance.
- [ ] Create a **Savings** account.
- [ ] (Optional) Mark an account as **shared** with the household.
- [ ] Empty-state check: a brand-new user with no accounts sees a helpful message, not a blank page.

## 3. Categories (mirror the old Excel sheet)
- [ ] Seed the default categories, **or** create your own (e.g. Housing → Rent, Groceries, Dining,
      Transport, Subscriptions).
- [ ] Rename one; create a sub-category; confirm the tree shows nesting.
- [ ] Try to delete a category that's in use later (see step 6) — it should be blocked with a clear
      message, not an error.

## 4. Transactions (the daily loop)
- [ ] Record **income** (salary) into Checking.
- [ ] Record an **expense** with a single category.
- [ ] Record an expense **split across two categories**; confirm the form blocks saving until the
      split total equals the amount.
- [ ] Record an **account-less ("cash")** expense (No account / Cash); confirm it appears in the list
      and does **not** change any account balance.
- [ ] Record a **transfer** Checking → Savings; confirm both balances move and it is neither income
      nor expense.
- [ ] Add **tags** and a **note** to a transaction; filter the list by month, category, type, tag,
      and search; confirm sorting and paging.

## 5. Balances
- [ ] Open **Accounts**; confirm each balance = opening + income − expenses ± transfers.

## 6. Budgets + alerts
- [ ] Create a **monthly budget** for Groceries (e.g. 5 000).
- [ ] Spend against it until you pass the **alert threshold** (default 80%); the budget card turns
      to **Warning** and shows the alert.
- [ ] Spend past 100%; status becomes **Exceeded**. (Email is logged in the API in the MVP — confirm
      with whoever runs the server that a single alert is logged per crossing, not repeatedly.)
- [ ] Confirm the **shared** vs **individual** budget scope behaves as expected.

## 7. Dashboard (the at-a-glance picture)
- [ ] Open the **Dashboard** (it's the landing page after login).
- [ ] Confirm **Income / Expenses / Net** for the current month match what you entered.
- [ ] Confirm **Spending by category** lists your largest categories with bars.
- [ ] Confirm the **Budgets** summary and **Accounts** snapshot look right.
- [ ] Switch **month** back/forward; switch **scope** Household ↔ Individual; numbers update.
- [ ] Cross-check: the dashboard month totals equal the sum of the **Transactions** list for that
      month/scope.

## 8. Polish / cross-cutting
- [ ] Every list with no data shows an empty state (no dead-ends, no raw errors).
- [ ] Validation errors are shown clearly (e.g. negative budget, bad date range).
- [ ] Loading spinners appear while data loads.
- [ ] Works on a **phone browser** — especially adding a transaction.

## 9. Scope guard (no Phase 2+ leakage)
- [ ] Confirm nothing beyond the MVP appears as usable (no multi-currency FX, loans/splitting,
      receipts/AI, net worth, price comparison, etc.). The Recurring section is a Sprint-5 placeholder.

---

### Feedback log
| # | Step | Issue / confusion | Severity (blocker/minor) | Status |
|---|------|-------------------|--------------------------|--------|
|   |      |                   |                          |        |

### Sign-off
- [ ] Both users track individual **and** shared income/expenses across accounts.
- [ ] Categories mirror the old Excel sheet and are manageable.
- [ ] Monthly budget + alert + dashboard give the at-a-glance picture.
- [ ] No blocker remains → **MVP accepted**.
